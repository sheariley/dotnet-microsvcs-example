using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_api.Data;
using order_api.Models;

namespace order_api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController(OrderDbContext db) : ControllerBase
{
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
