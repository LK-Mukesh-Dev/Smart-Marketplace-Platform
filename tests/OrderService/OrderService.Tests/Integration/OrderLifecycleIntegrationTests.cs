using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Handlers;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Tests.Integration;

public class OrderLifecycleIntegrationTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly CreateOrderHandler _createOrderHandler;
    private readonly UpdateOrderStatusHandler _updateOrderStatusHandler;

    public OrderLifecycleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _orderRepository = new OrderRepository(_context);
        _createOrderHandler = new CreateOrderHandler(_orderRepository);
        _updateOrderStatusHandler = new UpdateOrderStatusHandler(_orderRepository);
    }

    [Fact]
    public async Task CompleteOrderLifecycle_ShouldProgressThroughAllStates()
    {
        // Arrange - Create order
        var userId = Guid.NewGuid();
        var createCommand = new CreateOrderCommand
        {
            UserId = userId,
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001"
            },
            Items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    Quantity = 2,
                    UnitPrice = 49.99m,
                    Currency = "USD"
                }
            },
            ShippingCost = 5.99m,
            Tax = 10.00m
        };

        // Act & Assert - Create Order
        var orderDto = await _createOrderHandler.HandleAsync(createCommand);
        orderDto.Should().NotBeNull();
        orderDto.Status.Should().Be("Pending");

        // Act & Assert - Confirm Order
        var confirmCommand = new UpdateOrderStatusCommand
        {
            OrderId = orderDto.Id,
            NewStatus = "Confirmed"
        };
        var confirmResult = await _updateOrderStatusHandler.HandleAsync(confirmCommand);
        confirmResult.Should().BeTrue();

        // Act & Assert - Process Order
        var processCommand = new UpdateOrderStatusCommand
        {
            OrderId = orderDto.Id,
            NewStatus = "Processing"
        };
        var processResult = await _updateOrderStatusHandler.HandleAsync(processCommand);
        processResult.Should().BeTrue();

        // Act & Assert - Ship Order
        var shipCommand = new UpdateOrderStatusCommand
        {
            OrderId = orderDto.Id,
            NewStatus = "Shipped"
        };
        var shipResult = await _updateOrderStatusHandler.HandleAsync(shipCommand);
        shipResult.Should().BeTrue();

        // Act & Assert - Deliver Order
        var deliverCommand = new UpdateOrderStatusCommand
        {
            OrderId = orderDto.Id,
            NewStatus = "Delivered"
        };
        var deliverResult = await _updateOrderStatusHandler.HandleAsync(deliverCommand);
        deliverResult.Should().BeTrue();

        // Verify final state
        var finalOrder = await _orderRepository.GetByIdAsync(orderDto.Id);
        finalOrder!.Status.Should().Be(OrderStatus.Delivered);
        finalOrder.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task OrderCancellation_ShouldWorkAtAnyStage()
    {
        // Arrange - Create and confirm order
        var userId = Guid.NewGuid();
        var createCommand = CreateTestOrderCommand(userId);
        var orderDto = await _createOrderHandler.HandleAsync(createCommand);

        var confirmCommand = new UpdateOrderStatusCommand
        {
            OrderId = orderDto.Id,
            NewStatus = "Confirmed"
        };
        await _updateOrderStatusHandler.HandleAsync(confirmCommand);

        // Act - Cancel order
        var cancelCommand = new UpdateOrderStatusCommand
        {
            OrderId = orderDto.Id,
            NewStatus = "Cancelled",
            Reason = "Customer requested cancellation"
        };
        var cancelResult = await _updateOrderStatusHandler.HandleAsync(cancelCommand);

        // Assert
        cancelResult.Should().BeTrue();

        var order = await _orderRepository.GetByIdAsync(orderDto.Id);
        order!.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
        order.Notes.Should().Be("Customer requested cancellation");
    }

    [Fact]
    public async Task MultipleOrders_ShouldBeTrackedIndependently()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order1Command = CreateTestOrderCommand(userId);
        var order2Command = CreateTestOrderCommand(userId);
        var order3Command = CreateTestOrderCommand(userId);

        // Act - Create multiple orders
        var order1 = await _createOrderHandler.HandleAsync(order1Command);
        var order2 = await _createOrderHandler.HandleAsync(order2Command);
        var order3 = await _createOrderHandler.HandleAsync(order3Command);

        // Confirm first order
        await _updateOrderStatusHandler.HandleAsync(new UpdateOrderStatusCommand
        {
            OrderId = order1.Id,
            NewStatus = "Confirmed"
        });

        // Process second order (skip confirm for testing)
        var order2Entity = await _orderRepository.GetByIdAsync(order2.Id);
        order2Entity!.ConfirmOrder();
        order2Entity.StartProcessing();
        await _orderRepository.UpdateAsync(order2Entity);

        // Cancel third order
        await _updateOrderStatusHandler.HandleAsync(new UpdateOrderStatusCommand
        {
            OrderId = order3.Id,
            NewStatus = "Cancelled",
            Reason = "Test cancellation"
        });

        // Assert
        var userOrders = await _orderRepository.GetByUserIdAsync(userId);
        userOrders.Should().HaveCount(3);

        var finalOrder1 = userOrders.First(o => o.Id == order1.Id);
        finalOrder1.Status.Should().Be(OrderStatus.Confirmed);

        var finalOrder2 = userOrders.First(o => o.Id == order2.Id);
        finalOrder2.Status.Should().Be(OrderStatus.Processing);

        var finalOrder3 = userOrders.First(o => o.Id == order3.Id);
        finalOrder3.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task OrderWithMultipleItems_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createCommand = new CreateOrderCommand
        {
            UserId = userId,
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001"
            },
            Items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product 1",
                    Quantity = 2,
                    UnitPrice = 49.99m,
                    Currency = "USD"
                },
                new OrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product 2",
                    Quantity = 3,
                    UnitPrice = 29.99m,
                    Currency = "USD"
                }
            },
            ShippingCost = 10.00m,
            Tax = 15.00m
        };

        // Act
        var orderDto = await _createOrderHandler.HandleAsync(createCommand);

        // Assert
        orderDto.Items.Should().HaveCount(2);
        orderDto.TotalAmount.Should().Be(189.95m); // (2 * 49.99) + (3 * 29.99)
        orderDto.GrandTotal.Should().Be(214.95m); // 189.95 + 10.00 + 15.00
    }

    [Fact]
    public async Task PaymentCompletion_ShouldUpdatePaymentStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createCommand = CreateTestOrderCommand(userId);
        var orderDto = await _createOrderHandler.HandleAsync(createCommand);

        var order = await _orderRepository.GetByIdAsync(orderDto.Id);

        // Act
        order!.MarkPaymentCompleted();
        await _orderRepository.UpdateAsync(order);

        // Assert
        var updatedOrder = await _orderRepository.GetByIdAsync(orderDto.Id);
        updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public async Task PaymentFailure_ShouldUpdateOrderAndPaymentStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createCommand = CreateTestOrderCommand(userId);
        var orderDto = await _createOrderHandler.HandleAsync(createCommand);

        var order = await _orderRepository.GetByIdAsync(orderDto.Id);

        // Act
        order!.MarkPaymentFailed();
        await _orderRepository.UpdateAsync(order);

        // Assert
        var updatedOrder = await _orderRepository.GetByIdAsync(orderDto.Id);
        updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.Failed);
        updatedOrder.Status.Should().Be(OrderStatus.Failed);
    }

    private CreateOrderCommand CreateTestOrderCommand(Guid userId)
    {
        return new CreateOrderCommand
        {
            UserId = userId,
            ShippingAddress = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                Country = "USA",
                PostalCode = "10001"
            },
            Items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 49.99m,
                    Currency = "USD"
                }
            },
            ShippingCost = 5.99m,
            Tax = 5.00m
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
