using System.Text.Json;

namespace order_api.Messaging;

public record OrderEvent(
    Guid EventId,
    string EventType,
    Guid OrderId,
    DateTimeOffset OccurredAt,
    JsonElement Payload);

public record OrderPlacedPayload(
    string CustomerId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
