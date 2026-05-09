using inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory.Data;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Sku).HasMaxLength(50);
            e.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProcessedEvent>(e => e.HasKey(p => p.EventId));
    }
}
