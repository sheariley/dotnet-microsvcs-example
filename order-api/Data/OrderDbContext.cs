using Microsoft.EntityFrameworkCore;
using order_api.Models;

namespace order_api.Data;

public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.CustomerId).HasMaxLength(100);
            e.Property(o => o.ProductName).HasMaxLength(200);
            e.Property(o => o.UnitPrice).HasPrecision(18, 2);
            e.Property(o => o.Status).HasConversion<string>();
        });
    }
}
