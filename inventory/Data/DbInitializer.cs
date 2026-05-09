using inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace inventory.Data;

public static class DbInitializer
{
    private static readonly Product[] Seeds =
    [
        new() { Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000000"), Name = "Wireless Headphones", Sku = "AUDIO-001", Price = 79.99m, StockQuantity = 50 },
        new() { Id = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000000"), Name = "USB-C Hub 7-Port",    Sku = "HUB-002",   Price = 39.99m, StockQuantity = 100 },
        new() { Id = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000000"), Name = "Mechanical Keyboard", Sku = "KB-003",    Price = 129.99m, StockQuantity = 25 },
        new() { Id = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000000"), Name = "Webcam 1080p",        Sku = "CAM-004",   Price = 59.99m, StockQuantity = 75 },
        new() { Id = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000000"), Name = "LED Desk Lamp",       Sku = "LAMP-005",  Price = 34.99m, StockQuantity = 150 },
    ];

    public static async Task SeedAsync(InventoryDbContext db)
    {
        if (await db.Products.AnyAsync())
            return;

        db.Products.AddRange(Seeds);
        await db.SaveChangesAsync();
    }
}
