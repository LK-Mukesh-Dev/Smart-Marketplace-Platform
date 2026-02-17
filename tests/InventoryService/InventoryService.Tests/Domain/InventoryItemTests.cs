using FluentAssertions;
using InventoryService.Domain.Entities;

namespace InventoryService.Tests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void InventoryItem_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var item = new InventoryItem(productId, "Test Product", "SKU123", 100);

        // Assert
        item.Should().NotBeNull();
        item.Id.Should().NotBeEmpty();
        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be("Test Product");
        item.Sku.Should().Be("SKU123");
        item.QuantityAvailable.Should().Be(100);
        item.QuantityReserved.Should().Be(0);
        item.TotalQuantity.Should().Be(100);
    }

    [Fact]
    public void CanReserve_ShouldReturnTrue_WhenStockIsAvailable()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 100);

        // Act
        var canReserve = item.CanReserve(50);

        // Assert
        canReserve.Should().BeTrue();
    }

    [Fact]
    public void CanReserve_ShouldReturnFalse_WhenInsufficientStock()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 10);

        // Act
        var canReserve = item.CanReserve(20);

        // Assert
        canReserve.Should().BeFalse();
    }

    [Fact]
    public void Reserve_ShouldDecreaseAvailableAndIncreaseReserved()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 100);

        // Act
        item.Reserve(30);

        // Assert
        item.QuantityAvailable.Should().Be(70);
        item.QuantityReserved.Should().Be(30);
        item.TotalQuantity.Should().Be(100);
    }

    [Fact]
    public void Reserve_ShouldThrowException_WhenInsufficientStock()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 10);

        // Act & Assert
        var act = () => item.Reserve(20);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReleaseReservation_ShouldIncreaseAvailableAndDecreaseReserved()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 100);
        item.Reserve(30);

        // Act
        item.ReleaseReservation(10);

        // Assert
        item.QuantityAvailable.Should().Be(80);
        item.QuantityReserved.Should().Be(20);
        item.TotalQuantity.Should().Be(100);
    }

    [Fact]
    public void ConfirmReservation_ShouldDecreaseReserved()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 100);
        item.Reserve(30);

        // Act
        item.ConfirmReservation(30);

        // Assert
        item.QuantityAvailable.Should().Be(70);
        item.QuantityReserved.Should().Be(0);
        item.TotalQuantity.Should().Be(70);
    }

    [Fact]
    public void AddStock_ShouldIncreaseAvailableQuantity()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 100);

        // Act
        item.AddStock(50);

        // Assert
        item.QuantityAvailable.Should().Be(150);
        item.TotalQuantity.Should().Be(150);
    }

    [Fact]
    public void RemoveStock_ShouldDecreaseAvailableQuantity()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 100);

        // Act
        item.RemoveStock(20);

        // Assert
        item.QuantityAvailable.Should().Be(80);
        item.TotalQuantity.Should().Be(80);
    }

    [Fact]
    public void IsLowStock_ShouldReturnTrue_WhenBelowReorderLevel()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 5, reorderLevel: 10);

        // Assert
        item.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void IsLowStock_ShouldReturnFalse_WhenAboveReorderLevel()
    {
        // Arrange
        var item = new InventoryItem(Guid.NewGuid(), "Test Product", "SKU123", 50, reorderLevel: 10);

        // Assert
        item.IsLowStock.Should().BeFalse();
    }
}
