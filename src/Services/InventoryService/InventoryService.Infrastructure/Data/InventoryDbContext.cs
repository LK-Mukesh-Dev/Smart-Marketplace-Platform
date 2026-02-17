using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;

namespace InventoryService.Infrastructure.Data;

public class InventoryDbContext : DbContext
{
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<StockReservation> StockReservations { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItems");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.HasIndex(e => e.ProductId)
                .IsUnique();

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Sku)
                .IsUnique();

            entity.Property(e => e.QuantityAvailable)
                .IsRequired();

            entity.Property(e => e.QuantityReserved)
                .IsRequired();

            entity.Property(e => e.ReorderLevel)
                .IsRequired();

            entity.Property(e => e.MaxStockLevel)
                .IsRequired();

            entity.Property(e => e.LastRestocked)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.ToTable("StockReservations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.Property(e => e.OrderId)
                .IsRequired();

            entity.HasIndex(e => e.OrderId);

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.ReservedAt)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("StockMovements");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProductId)
                .IsRequired();

            entity.HasIndex(e => e.ProductId);

            entity.Property(e => e.MovementType)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.QuantityBefore)
                .IsRequired();

            entity.Property(e => e.QuantityAfter)
                .IsRequired();

            entity.Property(e => e.Reference)
                .HasMaxLength(100);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });
    }
}
