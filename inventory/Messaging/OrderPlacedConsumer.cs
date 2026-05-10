using System.Text.Json;
using Confluent.Kafka;
using inventory.Data;
using inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory.Messaging;

public class OrderPlacedConsumer(
    IServiceScopeFactory scopeFactory,
    IProducer<string, string> producer,
    InstrumentedConsumerBuilder<string, string> consumerBuilder,
    ILogger<OrderPlacedConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private const string Topic = "orders";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = consumerBuilder.Build();
        consumer.Subscribe(Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result.IsPartitionEOF || result.Message?.Value is null)
                    continue;

                var envelope = JsonSerializer.Deserialize<OrderEvent>(result.Message.Value, JsonOpts);

                if (envelope?.EventType == "OrderPlaced")
                    await HandleOrderPlaced(envelope, stoppingToken);

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

    private async Task HandleOrderPlaced(OrderEvent envelope, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        // Idempotency: skip if already processed
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == envelope.EventId, ct))
        {
            logger.LogWarning("Duplicate event {EventId} skipped", envelope.EventId);
            return;
        }

        var payload = envelope.Payload.Deserialize<OrderPlacedPayload>(JsonOpts);
        if (payload is null) return;

        // Atomic reservation: decrement only if sufficient stock
        var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Products" SET "StockQuantity" = "StockQuantity" - {payload.Quantity} WHERE "Id" = {payload.ProductId} AND "StockQuantity" >= {payload.Quantity}""",
            ct);

        string outEventType;
        object outPayload;

        if (rowsAffected > 0)
        {
            outEventType = "OrderReserved";
            outPayload = new { };
            logger.LogInformation("Reserved {Qty} units for order {OrderId}", payload.Quantity, envelope.OrderId);
        }
        else
        {
            outEventType = "OrderRejected";
            outPayload = new OrderRejectedPayload("Insufficient stock");
            logger.LogInformation("Rejected order {OrderId} — insufficient stock", envelope.OrderId);
        }

        var outEnvelope = new
        {
            EventId = Guid.NewGuid(),
            EventType = outEventType,
            OrderId = envelope.OrderId,
            OccurredAt = DateTimeOffset.UtcNow,
            Payload = outPayload,
        };

        await producer.ProduceAsync(Topic, new Message<string, string>
        {
            Key = envelope.OrderId.ToString(),
            Value = JsonSerializer.Serialize(outEnvelope),
        }, ct);

        db.ProcessedEvents.Add(new ProcessedEvent { EventId = envelope.EventId });
        await db.SaveChangesAsync(ct);
    }
}
