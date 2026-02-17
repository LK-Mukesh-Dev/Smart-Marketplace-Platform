using FluentAssertions;
using OrderService.Application.Commands;
using OrderService.Application.DTOs;
using OrderService.Application.Handlers;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using Moq;

namespace OrderService.Tests.Application;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new CreateOrderHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateOrder_WithValidCommand()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var command = new CreateOrderCommand
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
                    ProductId = productId,
                    ProductName = "Test Product",
                    Quantity = 2,
                    UnitPrice = 49.99m,
                    Currency = "USD"
                }
            },
            ShippingCost = 5.99m,
            Tax = 10.00m
        };

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Status.Should().Be("Pending");
        result.PaymentStatus.Should().Be("Pending");
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductId.Should().Be(productId);
        result.Items.First().Quantity.Should().Be(2);
        result.TotalAmount.Should().Be(99.98m);
        result.ShippingCost.Should().Be(5.99m);
        result.Tax.Should().Be(10.00m);
        result.GrandTotal.Should().Be(115.97m);

        _orderRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateOrder_WithMultipleItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var command = new CreateOrderCommand
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
                    ProductId = productId1,
                    ProductName = "Product 1",
                    Quantity = 2,
                    UnitPrice = 49.99m,
                    Currency = "USD"
                },
                new OrderItemDto
                {
                    ProductId = productId2,
                    ProductName = "Product 2",
                    Quantity = 1,
                    UnitPrice = 29.99m,
                    Currency = "USD"
                }
            },
            ShippingCost = 5.99m,
            Tax = 13.00m
        };

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalAmount.Should().Be(129.97m);
        result.GrandTotal.Should().Be(148.96m);

        _orderRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateOrderNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateOrderCommand
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

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.OrderNumber.Should().NotBeNullOrEmpty();
        result.OrderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetCreatedAtDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateOrderCommand
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

        _orderRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
