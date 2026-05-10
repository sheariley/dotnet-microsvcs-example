using System.Text.Json;
using Confluent.Kafka;
using order_api.Data;
using order_api.Models;

namespace order_api.Messaging;

public class OrderEventConsumer(
    IServiceScopeFactory scopeFactory,
    InstrumentedConsumerBuilder<string, string> consumerBuilder,
    ILogger<OrderEventConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

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
    }
}
