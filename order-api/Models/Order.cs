namespace order_api.Models;

public class Order
{
    public Guid Id { get; set; }
    public required string CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum OrderStatus { Placed, Reserved, Confirmed, Rejected }
