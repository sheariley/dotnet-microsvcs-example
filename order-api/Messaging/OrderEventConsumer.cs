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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result.IsPartitionEOF || result.Message?.Value is null)
                    continue;

                // Capture the consumer span's context before any await — the instrumentation
                // disposes the activity when Consume() returns, so it won't be in
                // Activity.Current by the time HandleOutcome resumes after an await.
                var consumeContext = Activity.Current?.Context ?? default;

                var envelope = JsonSerializer.Deserialize<OrderEvent>(result.Message.Value, JsonOpts);

                if (envelope?.EventType is "OrderReserved" or "OrderRejected")
                    await HandleOutcome(envelope, consumeContext, stoppingToken);

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

    private async Task HandleOutcome(OrderEvent envelope, ActivityContext consumeContext, CancellationToken ct)
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

        using var activity = ActivitySource.StartActivity("order.ws-push", ActivityKind.Internal, consumeContext);
        activity?.SetTag("ws.message.type", "OrderStatusChanged");
        activity?.SetTag("order.id", order.Id.ToString());
        activity?.SetTag("customer.id", order.CustomerId);

        var message = JsonSerializer.Serialize(new
        {
            type = "OrderStatusChanged",
            orderId = order.Id,
            status = order.Status.ToString(),
            updatedAt = order.UpdatedAt,
        });

        await hub.PushAsync(order.CustomerId, message, ct);
    }
}
