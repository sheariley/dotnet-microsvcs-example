using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_api.Data;
using order_api.Messaging;
using order_api.Models;
using order_api.WebSockets;

namespace order_api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController(OrderDbContext db, IProducer<string, string> producer, WebSocketHub hub) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("order-api");

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = req.CustomerId,
            ProductId = req.ProductId,
            ProductName = req.ProductName,
            Quantity = req.Quantity,
            UnitPrice = req.UnitPrice,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var envelope = new
        {
            EventId = Guid.NewGuid(),
            EventType = "OrderPlaced",
            OrderId = order.Id,
            OccurredAt = DateTimeOffset.UtcNow,
            Payload = new OrderPlacedPayload(
                order.CustomerId,
                order.ProductId,
                order.ProductName,
                order.Quantity,
                order.UnitPrice),
        };

        await producer.ProduceAsync("orders", new Message<string, string>
        {
            Key = order.Id.ToString(),
            Value = JsonSerializer.Serialize(envelope),
        });

        using (var activity = ActivitySource.StartActivity("order.ws-push", ActivityKind.Internal))
        {
            activity?.SetTag("ws.message.type", "OrderCreated");
            activity?.SetTag("order.id", order.Id.ToString());
            activity?.SetTag("customer.id", order.CustomerId);

            await hub.PushAsync(order.CustomerId, JsonSerializer.Serialize(new
            {
                type = "OrderCreated",
                order,
            }, ApiJsonOptions.Shared));
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await db.Orders.FindAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? customerId)
    {
        var query = db.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders);
    }
}

public record CreateOrderRequest(
    string CustomerId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
