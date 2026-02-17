using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

public class OrderItemTests
{
    [Fact]
    public void OrderItem_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var orderItem = new OrderItem(productId, "Test Product", 5, unitPrice);

        // Assert
        orderItem.Should().NotBeNull();
        orderItem.Id.Should().NotBeEmpty();
        orderItem.ProductId.Should().Be(productId);
        orderItem.ProductName.Should().Be("Test Product");
        orderItem.Quantity.Should().Be(5);
        orderItem.UnitPrice.Should().Be(unitPrice);
        orderItem.TotalPrice.Should().Be(new Money(54.95m));
        orderItem.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OrderItem_ShouldThrowException_WithZeroQuantity()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var act = () => new OrderItem(productId, "Test Product", 0, unitPrice);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void OrderItem_ShouldThrowException_WithNegativeQuantity()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var act = () => new OrderItem(productId, "Test Product", -1, unitPrice);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void OrderItem_ShouldThrowException_WithEmptyProductName()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var act = () => new OrderItem(productId, "", 5, unitPrice);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Product name cannot be empty*");
    }

    [Fact]
    public void OrderItem_ShouldThrowException_WithNullUnitPrice()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var act = () => new OrderItem(productId, "Test Product", 5, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateQuantity_ShouldUpdateQuantityAndTotalPrice()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var orderItem = new OrderItem(productId, "Test Product", 5, unitPrice);

        // Act
        orderItem.UpdateQuantity(10);

        // Assert
        orderItem.Quantity.Should().Be(10);
        orderItem.TotalPrice.Should().Be(new Money(109.90m));
    }

    [Fact]
    public void UpdateQuantity_ShouldThrowException_WithZeroQuantity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var orderItem = new OrderItem(productId, "Test Product", 5, unitPrice);

        // Act
        var act = () => orderItem.UpdateQuantity(0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void UpdateQuantity_ShouldThrowException_WithNegativeQuantity()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(10.99m);
        var orderItem = new OrderItem(productId, "Test Product", 5, unitPrice);

        // Act
        var act = () => orderItem.UpdateQuantity(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity must be greater than zero*");
    }

    [Fact]
    public void OrderItem_ShouldCalculateTotalPrice_Correctly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var unitPrice = new Money(19.99m);
        var quantity = 3;

        // Act
        var orderItem = new OrderItem(productId, "Test Product", quantity, unitPrice);

        // Assert
        orderItem.TotalPrice.Amount.Should().Be(59.97m);
    }
}
