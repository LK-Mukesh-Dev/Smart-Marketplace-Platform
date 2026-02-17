using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Order_ShouldBeCreated_WithValidData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var address = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), "Product 1", 2, new Money(10.99m))
        };
        var shippingCost = new Money(5.00m);
        var tax = new Money(2.00m);

        // Act
        var order = new Order(userId, address, items, shippingCost, tax);

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().NotBeEmpty();
        order.OrderNumber.Should().NotBeNullOrEmpty();
        order.UserId.Should().Be(userId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Amount.Should().Be(21.98m);
        order.GrandTotal.Amount.Should().Be(28.98m);
    }

    [Fact]
    public void Order_ShouldTransitionToConfirmed_WhenConfirmed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var address = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), "Product 1", 1, new Money(10.00m))
        };
        var order = new Order(userId, address, items, Money.Zero(), Money.Zero());

        // Act
        order.ConfirmOrder();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Order_ShouldThrowException_WhenConfirmingNonPendingOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var address = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), "Product 1", 1, new Money(10.00m))
        };
        var order = new Order(userId, address, items, Money.Zero(), Money.Zero());
        order.ConfirmOrder();

        // Act & Assert
        var act = () => order.ConfirmOrder();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Order_ShouldCancel_WithReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var address = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), "Product 1", 1, new Money(10.00m))
        };
        var order = new Order(userId, address, items, Money.Zero(), Money.Zero());
        var reason = "Customer requested cancellation";

        // Act
        order.Cancel(reason);

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.Notes.Should().Be(reason);
        order.CancelledAt.Should().NotBeNull();
    }
}
