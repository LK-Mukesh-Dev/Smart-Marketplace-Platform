using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Tests.Integration;

public class OrderRepositoryIntegrationTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly IOrderRepository _repository;

    public OrderRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        _repository = new OrderRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddOrderToDatabase()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var result = await _repository.CreateAsync(order);

        // Assert
        result.Should().NotBeNull();
        var savedOrder = await _context.Orders.FindAsync(order.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.UserId.Should().Be(order.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        await _repository.CreateAsync(order);

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeOrderItems()
    {
        // Arrange
        var order = CreateTestOrder();
        await _repository.CreateAsync(order);

        // Act
        var result = await _repository.GetByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByOrderNumberAsync_ShouldReturnOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        await _repository.CreateAsync(order);

        // Act
        var result = await _repository.GetByOrderNumberAsync(order.OrderNumber);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order1 = CreateTestOrder(userId);
        var order2 = CreateTestOrder(userId);
        var order3 = CreateTestOrder(Guid.NewGuid());

        await _repository.CreateAsync(order1);
        await _repository.CreateAsync(order2);
        await _repository.CreateAsync(order3);

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.All(o => o.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        await _repository.CreateAsync(order);

        // Act
        order.ConfirmOrder();
        await _repository.UpdateAsync(order);

        // Assert
        var updatedOrder = await _repository.GetByIdAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderService.Domain.Enums.OrderStatus.Confirmed);
    }

    private Order CreateTestOrder(Guid? userId = null)
    {
        var actualUserId = userId ?? Guid.NewGuid();
        var address = new Address("123 Main St", "New York", "NY", "USA", "10001");
        var items = new List<OrderItem>
        {
            new OrderItem(Guid.NewGuid(), "Test Product", 1, new Money(49.99m))
        };
        var shippingCost = new Money(5.99m);
        var tax = new Money(5.00m);

        return new Order(actualUserId, address, items, shippingCost, tax);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
