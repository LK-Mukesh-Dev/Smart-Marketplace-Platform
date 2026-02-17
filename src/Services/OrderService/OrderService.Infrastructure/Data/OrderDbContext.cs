using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Infrastructure.Data;

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);

            entity.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(o => o.OrderNumber)
                .IsUnique();

            entity.Property(o => o.UserId)
                .IsRequired();

            entity.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(o => o.PaymentStatus)
                .IsRequired()
                .HasConversion<string>();

            entity.OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(100);
                address.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
                address.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
            });

            entity.OwnsOne(o => o.TotalAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            entity.OwnsOne(o => o.ShippingCost, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ShippingCost").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("ShippingCurrency").HasMaxLength(3);
            });

            entity.OwnsOne(o => o.Tax, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Tax").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("TaxCurrency").HasMaxLength(3);
            });

            entity.OwnsOne(o => o.GrandTotal, money =>
            {
                money.Property(m => m.Amount).HasColumnName("GrandTotal").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("GrandTotalCurrency").HasMaxLength(3);
            });

            entity.Property(o => o.Notes)
                .HasMaxLength(500);

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.HasMany<OrderItem>()
                .WithOne()
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(oi => oi.Id);

            entity.Property(oi => oi.ProductId)
                .IsRequired();

            entity.Property(oi => oi.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(oi => oi.Quantity)
                .IsRequired();

            entity.OwnsOne(oi => oi.UnitPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            entity.OwnsOne(oi => oi.TotalPrice, money =>
            {
                money.Property(m => m.Amount).HasColumnName("TotalPrice").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("TotalCurrency").HasMaxLength(3);
            });

            entity.Property(oi => oi.CreatedAt)
                .IsRequired();
        });
    }
}
