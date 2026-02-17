using FluentAssertions;
using PaymentService.Application.Events;
using PaymentService.Application.EventHandlers;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace PaymentService.Tests.Application;

public class InventoryReservedEventHandlerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly Mock<IIdempotencyStore> _idempotencyStoreMock;
    private readonly Mock<ILogger<InventoryReservedEventHandler>> _loggerMock;
    private readonly InventoryReservedEventHandler _handler;

    public InventoryReservedEventHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _idempotencyStoreMock = new Mock<IIdempotencyStore>();
        _loggerMock = new Mock<ILogger<InventoryReservedEventHandler>>();

        _handler = new InventoryReservedEventHandler(
            _paymentRepositoryMock.Object,
            _paymentGatewayMock.Object,
            _idempotencyStoreMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldProcessPayment_Successfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var transactionId = "TXN-12345";

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _idempotencyStoreMock
            .Setup(x => x.ExistsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentGatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult
            {
                Success = true,
                TransactionId = transactionId,
                GatewayResponse = "Payment approved"
            });

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotencyStoreMock
            .Setup(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Payment successful");
        result.PaymentId.Should().NotBeNull();

        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _paymentGatewayMock.Verify(x => x.ProcessPaymentAsync(orderId, amount, It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyStoreMock.Verify(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandlePaymentFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var errorMessage = "Insufficient funds";

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _idempotencyStoreMock
            .Setup(x => x.ExistsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentGatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                GatewayResponse = "Payment declined"
            });

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotencyStoreMock
            .Setup(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
        result.PaymentId.Should().NotBeNull();

        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyStoreMock.Verify(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnExisting_WhenPaymentAlreadyProcessed()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingPaymentId = Guid.NewGuid();
        var amount = 99.99m;

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _idempotencyStoreMock
            .Setup(x => x.ExistsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _idempotencyStoreMock
            .Setup(x => x.GetPaymentIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPaymentId);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Payment already processed");
        result.PaymentId.Should().Be(existingPaymentId);

        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentGatewayMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleException_Gracefully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _idempotencyStoreMock
            .Setup(x => x.ExistsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Payment processing error");
        result.PaymentId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveIdempotencyKey_OnSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _idempotencyStoreMock
            .Setup(x => x.ExistsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentGatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult
            {
                Success = true,
                TransactionId = "TXN-12345"
            });

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotencyStoreMock
            .Setup(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeTrue();
        _idempotencyStoreMock.Verify(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSaveIdempotencyKey_OnFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 99.99m;

        var evt = new InventoryReservedEvent
        {
            OrderId = orderId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        _idempotencyStoreMock
            .Setup(x => x.ExistsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _paymentRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _paymentGatewayMock
            .Setup(x => x.ProcessPaymentAsync(orderId, amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Card declined"
            });

        _paymentRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotencyStoreMock
            .Setup(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(evt);

        // Assert
        result.Success.Should().BeFalse();
        _idempotencyStoreMock.Verify(x => x.SaveAsync(orderId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
