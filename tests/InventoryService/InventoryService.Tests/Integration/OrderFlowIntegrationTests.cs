using FluentAssertions;
using InventoryService.Application.EventHandlers;
using InventoryService.Application.Events;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;
using InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace InventoryService.Tests.Integration;

public class OrderFlowIntegrationTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockReservationRepository _reservationRepository;
    private readonly IStockMovementRepository _movementRepository;
    private readonly Mock<IDistributedLock> _distributedLockMock;
    private readonly Mock<ILogger<OrderCreatedEventHandler>> _orderCreatedLoggerMock;
    private readonly Mock<ILogger<PaymentFailedEventHandler>> _paymentFailedLoggerMock;
    private readonly OrderCreatedEventHandler _orderCreatedHandler;
    private readonly PaymentFailedEventHandler _paymentFailedHandler;

    public OrderFlowIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryDbContext(options);
        _inventoryRepository = new InventoryRepository(_context);
        _reservationRepository = new StockReservationRepository(_context);
        _movementRepository = new StockMovementRepository(_context);
        _distributedLockMock = new Mock<IDistributedLock>();
        _orderCreatedLoggerMock = new Mock<ILogger<OrderCreatedEventHandler>>();
        _paymentFailedLoggerMock = new Mock<ILogger<PaymentFailedEventHandler>>();

        _distributedLockMock
            .Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _distributedLockMock
            .Setup(x => x.ReleaseLockAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderCreatedHandler = new OrderCreatedEventHandler(
            _inventoryRepository,
            _reservationRepository,
            _movementRepository,
            _distributedLockMock.Object,
            _orderCreatedLoggerMock.Object);

        _paymentFailedHandler = new PaymentFailedEventHandler(
            _inventoryRepository,
            _reservationRepository,
            _movementRepository,
            _distributedLockMock.Object,
            _paymentFailedLoggerMock.Object);
    }

    [Fact]
    public async Task CompleteOrderFlow_ShouldReserveStock()
    {
        // Arrange - Setup inventory
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 100);
        await _inventoryRepository.CreateAsync(inventoryItem);

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = orderId,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent { ProductId = productId, Quantity = 10 }
            }
        };

        // Act - Reserve stock for order
        var reserveResult = await _orderCreatedHandler.HandleAsync(orderCreatedEvent);

        // Assert - Verify reservation
        reserveResult.Success.Should().BeTrue();

        var updatedItem = await _inventoryRepository.GetByProductIdAsync(productId);
        updatedItem!.QuantityAvailable.Should().Be(90);
        updatedItem.QuantityReserved.Should().Be(10);

        var reservation = await _reservationRepository.GetByOrderIdAsync(orderId);
        reservation.Should().NotBeNull();
        reservation!.Status.Should().Be(ReservationStatus.Reserved);

        var movements = await _movementRepository.GetByProductIdAsync(productId);
        movements.Should().HaveCount(1);
        movements.First().MovementType.Should().Be(StockMovementType.Reserved);
    }

    [Fact]
    public async Task OrderCancellationFlow_ShouldReleaseReservedStock()
    {
        // Arrange - Setup inventory and create reservation
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 100);
        await _inventoryRepository.CreateAsync(inventoryItem);

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = orderId,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent { ProductId = productId, Quantity = 10 }
            }
        };

        await _orderCreatedHandler.HandleAsync(orderCreatedEvent);

        // Act - Cancel order (payment failed)
        var paymentFailedEvent = new PaymentFailedEvent
        {
            OrderId = orderId,
            Reason = "Payment declined"
        };

        var releaseResult = await _paymentFailedHandler.HandleAsync(paymentFailedEvent);

        // Assert - Verify stock released
        releaseResult.Should().BeTrue();

        var updatedItem = await _inventoryRepository.GetByProductIdAsync(productId);
        updatedItem!.QuantityAvailable.Should().Be(100);
        updatedItem.QuantityReserved.Should().Be(0);

        var reservation = await _reservationRepository.GetByOrderIdAsync(orderId);
        reservation!.Status.Should().Be(ReservationStatus.Released);

        var movements = await _movementRepository.GetByProductIdAsync(productId);
        movements.Should().HaveCount(2);
        movements.Should().Contain(m => m.MovementType == StockMovementType.Reserved);
        movements.Should().Contain(m => m.MovementType == StockMovementType.Released);
    }

    [Fact]
    public async Task MultipleProductOrderFlow_ShouldHandleCorrectly()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var item1 = new InventoryItem(productId1, "Product 1", "SKU1", 100);
        var item2 = new InventoryItem(productId2, "Product 2", "SKU2", 50);

        await _inventoryRepository.CreateAsync(item1);
        await _inventoryRepository.CreateAsync(item2);

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = orderId,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent { ProductId = productId1, Quantity = 10 },
                new OrderItemEvent { ProductId = productId2, Quantity = 5 }
            }
        };

        // Act - Reserve stock
        var reserveResult = await _orderCreatedHandler.HandleAsync(orderCreatedEvent);

        // Assert
        reserveResult.Success.Should().BeTrue();

        var updatedItem1 = await _inventoryRepository.GetByProductIdAsync(productId1);
        updatedItem1!.QuantityAvailable.Should().Be(90);
        updatedItem1.QuantityReserved.Should().Be(10);

        var updatedItem2 = await _inventoryRepository.GetByProductIdAsync(productId2);
        updatedItem2!.QuantityAvailable.Should().Be(45);
        updatedItem2.QuantityReserved.Should().Be(5);
    }

    [Fact]
    public async Task InsufficientStockFlow_ShouldFailGracefully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 5);
        await _inventoryRepository.CreateAsync(inventoryItem);

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = orderId,
            Items = new List<OrderItemEvent>
            {
                new OrderItemEvent { ProductId = productId, Quantity = 10 }
            }
        };

        // Act
        var result = await _orderCreatedHandler.HandleAsync(orderCreatedEvent);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient stock");

        var item = await _inventoryRepository.GetByProductIdAsync(productId);
        item!.QuantityAvailable.Should().Be(5);
        item.QuantityReserved.Should().Be(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
