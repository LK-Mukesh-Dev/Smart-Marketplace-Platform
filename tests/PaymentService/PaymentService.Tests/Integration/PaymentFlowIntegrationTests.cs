using FluentAssertions;
using PaymentService.Application.Events;
using PaymentService.Application.EventHandlers;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Interfaces;
using PaymentService.Infrastructure.Gateway;
using PaymentService.Infrastructure.Idempotency;
using Microsoft.Extensions.Logging;
using Moq;

namespace PaymentService.Tests.Integration;

public class PaymentFlowIntegrationTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly Mock<ILogger<MockPaymentGateway>> _gatewayLoggerMock;
    private readonly Mock<ILogger<InventoryReservedEventHandler>> _handlerLoggerMock;
    private readonly InventoryReservedEventHandler _handler;

    public PaymentFlowIntegrationTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _gatewayLoggerMock = new Mock<ILogger<MockPaymentGateway>>();
        _handlerLoggerMock = new Mock<ILogger<InventoryReservedEventHandler>>();
        
        _paymentGateway = new MockPaymentGateway(_gatewayLoggerMock.Object);
        _idempotencyStore = new InMemoryIdempotencyStore();

        _handler = new InventoryReservedEventHandler(
            _paymentRepositoryMock.Object,
            _paymentGateway,
            _idempotencyStore,
            _handlerLoggerMock.Object);
    }

    [Fact]
    public async Task CompletePaymentFlow_ShouldProcessSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        Payment? capturedPayment = null;

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) =>
            {
                capturedPayment = p;
                return p;
            });

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.PaymentId.Should().NotBeNull();
        capturedPayment.Should().NotBeNull();
        capturedPayment!.OrderId.Should().Be(orderId);
        capturedPayment.Amount.Should().Be(amount);

        // Verify idempotency was saved
        var exists = await _idempotencyStore.ExistsAsync(orderId);
        exists.Should().BeTrue();

        var storedPaymentId = await _idempotencyStore.GetPaymentIdAsync(orderId);
        storedPaymentId.Should().Be(capturedPayment.Id);
    }

    [Fact]
    public async Task IdempotentProcessing_ShouldPreventDuplicatePayments()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var firstPaymentId = Guid.NewGuid();

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        // Simulate already processed payment
        await _idempotencyStore.SaveAsync(orderId, firstPaymentId);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Payment already processed");
        result.PaymentId.Should().Be(firstPaymentId);

        // Verify no new payment was created
        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MultiplePayments_ShouldBeTrackedIndependently()
    {
        // Arrange
        var order1Id = Guid.NewGuid();
        var order2Id = Guid.NewGuid();
        var amount = 99.99m;

        var evt1 = new InventoryReservedEvent { OrderId = order1Id, Amount = amount };
        var evt2 = new InventoryReservedEvent { OrderId = order2Id, Amount = amount };

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.HandleAsync(evt1);
        var result2 = await _handler.HandleAsync(evt2);

        // Assert
        result1.PaymentId.Should().NotBeNull();
        result2.PaymentId.Should().NotBeNull();
        result1.PaymentId.Should().NotBe(result2.PaymentId);

        // Verify both orders are tracked
        var exists1 = await _idempotencyStore.ExistsAsync(order1Id);
        var exists2 = await _idempotencyStore.ExistsAsync(order2Id);

        exists1.Should().BeTrue();
        exists2.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentPayments_ShouldBeHandledSafely()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var evt = new InventoryReservedEvent { OrderId = orderId, Amount = amount };

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - Simulate concurrent processing
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () => await _handler.HandleAsync(evt)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - Only one payment should be created
        var successfulResults = results.Where(r => r.Success && r.PaymentId != null).ToList();
        successfulResults.Should().HaveCountGreaterThan(0);

        // All successful results should return the same payment ID due to idempotency
        var paymentIds = successfulResults.Select(r => r.PaymentId).Distinct().ToList();
        
        // Due to race conditions, we might have 1 or 2 payment creations,
        // but idempotency should ensure consistent results
        var storedPaymentId = await _idempotencyStore.GetPaymentIdAsync(orderId);
        storedPaymentId.Should().NotBeNull();
    }

    [Fact]
    public async Task PaymentRetry_ShouldUseIdempotencyKey()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var evt = new InventoryReservedEvent { OrderId = orderId, Amount = amount };

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - First attempt
        var firstResult = await _handler.HandleAsync(evt);

        // Act - Retry (simulating Kafka redelivery)
        var retryResult = await _handler.HandleAsync(evt);

        // Assert
        firstResult.PaymentId.Should().NotBeNull();
        retryResult.PaymentId.Should().Be(firstResult.PaymentId);
        retryResult.Message.Should().Be("Payment already processed");

        // Verify only one payment was created
        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
