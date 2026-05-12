using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using order_api.Data;
using order_api.Models;
using order_api.WebSockets;

namespace order_api.Messaging;

public class OrderEventConsumer(
    IServiceScopeFactory scopeFactory,
    InstrumentedConsumerBuilder<string, string> consumerBuilder,
    WebSocketHub hub,
    ILogger<OrderEventConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly ActivitySource ActivitySource = new("order-api");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = consumerBuilder.Build();
        consumer.Subscribe("orders");

        // ActivityStarted fires synchronously on the consuming thread inside Consume(), before
        // the instrumentation disposes the span. We capture the context here (it's just an
        // immutable struct) so we can parent our process span under the correct consumer span
        // rather than the inventory producer span that's embedded in the message headers.
        ActivityContext consumerSpanContext = default;
        using var contextCapture = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "OpenTelemetry.Instrumentation.ConfluentKafka",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a =>
            {
                if (a.Kind == ActivityKind.Consumer) consumerSpanContext = a.Context;
            },
        };
        ActivitySource.AddActivityListener(contextCapture);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                consumerSpanContext = default;
                var result = consumer.Consume(stoppingToken);
                if (result.IsPartitionEOF || result.Message?.Value is null)
                    continue;

                using var processActivity = ActivitySource.StartActivity(
                    "orders.process", ActivityKind.Consumer, consumerSpanContext);
                processActivity?.SetTag("messaging.system", "kafka");
                processActivity?.SetTag("messaging.destination", "orders");

                var envelope = JsonSerializer.Deserialize<OrderEvent>(result.Message.Value, JsonOpts);

                if (envelope?.EventType is "OrderReserved" or "OrderRejected")
                    await HandleOutcome(envelope, stoppingToken);

                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message from orders topic");
                await Task.Delay(1000, stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task HandleOutcome(OrderEvent envelope, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders.FindAsync([envelope.OrderId], ct);
        if (order is null) return;

        order.Status = envelope.EventType == "OrderReserved"
            ? OrderStatus.Confirmed
            : OrderStatus.Rejected;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderId} advanced to {Status}", order.Id, order.Status);

        using var activity = ActivitySource.StartActivity("order.ws-push", ActivityKind.Internal);
        activity?.SetTag("ws.message.type", "OrderStatusChanged");
        activity?.SetTag("order.id", order.Id.ToString());
        activity?.SetTag("customer.id", order.CustomerId);

        var message = JsonSerializer.Serialize(new
        {
            type = "OrderStatusChanged",
            orderId = order.Id,
            status = order.Status,
            updatedAt = order.UpdatedAt,
        }, ApiJsonOptions.Shared);

        await hub.PushAsync(order.CustomerId, message, ct);
    }
}
