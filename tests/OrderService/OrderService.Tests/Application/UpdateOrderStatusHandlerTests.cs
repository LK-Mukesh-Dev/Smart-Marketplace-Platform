using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;
using Moq;

namespace OrderService.Tests.Application;

public class UpdateOrderStatusHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly UpdateOrderStatusHandler _handler;

    public UpdateOrderStatusHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new UpdateOrderStatusHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldConfirmOrder_WhenStatusIsPending()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder();

        var command = new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            NewStatus = "Confirmed"
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldProcessOrder_WhenStatusIsConfirmed()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder();
        order.ConfirmOrder();

        var command = new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            NewStatus = "Processing"
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Processing);

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldShipOrder_WhenStatusIsProcessing()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder();
        order.ConfirmOrder();
        order.StartProcessing();

        var command = new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            NewStatus = "Shipped"
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeliverOrder_WhenStatusIsShipped()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder();
        order.ConfirmOrder();
        order.StartProcessing();
        order.Ship();

        var command = new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            NewStatus = "Delivered"
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.CompletedAt.Should().NotBeNull();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelOrder_WithReason()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateTestOrder();

        var command = new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            NewStatus = "Cancelled",
            Reason = "Customer requested cancellation"
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _orderRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
        order.Notes.Should().Be("Customer requested cancellation");

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFalse_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new UpdateOrderStatusCommand
        {
            OrderId = orderId,
            NewStatus = "Confirmed"
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().BeFalse();

        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Order CreateTestOrder()
    {
        var userId = Guid.NewGuid();
        var address = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), "Test Product", 1, new Money(49.99m))
        };
        var shippingCost = new Money(5.99m);
        var tax = new Money(5.00m);

        return new Order(userId, address, items, shippingCost, tax);
    }
}
