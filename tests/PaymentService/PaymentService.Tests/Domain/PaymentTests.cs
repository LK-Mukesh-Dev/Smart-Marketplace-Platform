using FluentAssertions;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void Payment_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var orderId = Guid.NewGuid();
        var amount = 99.99m;
        var payment = new Payment(orderId, amount);

        // Assert
        payment.Should().NotBeNull();
        payment.Id.Should().NotBeEmpty();
        payment.OrderId.Should().Be(orderId);
        payment.Amount.Should().Be(amount);
        payment.Status.Should().Be(PaymentStatus.Initiated);
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        payment.TransactionId.Should().BeNull();
        payment.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Payment_ShouldThrowException_WithEmptyOrderId()
    {
        // Arrange & Act
        var act = () => new Payment(Guid.Empty, 99.99m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*OrderId cannot be empty*");
    }

    [Fact]
    public void Payment_ShouldThrowException_WithZeroAmount()
    {
        // Arrange & Act
        var act = () => new Payment(Guid.NewGuid(), 0m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount must be greater than zero*");
    }

    [Fact]
    public void Payment_ShouldThrowException_WithNegativeAmount()
    {
        // Arrange & Act
        var act = () => new Payment(Guid.NewGuid(), -10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount must be greater than zero*");
    }

    [Fact]
    public void MarkProcessing_ShouldUpdateStatus_FromInitiated()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);

        // Act
        payment.MarkProcessing();

        // Assert
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public void MarkProcessing_ShouldThrowException_FromNonInitiatedStatus()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkProcessing();

        // Act
        var act = () => payment.MarkProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark payment as processing*");
    }

    [Fact]
    public void MarkSuccess_ShouldUpdateStatus_WithTransactionId()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkProcessing();
        var transactionId = "TXN-12345";

        // Act
        payment.MarkSuccess(transactionId, "Payment approved");

        // Assert
        payment.Status.Should().Be(PaymentStatus.Success);
        payment.TransactionId.Should().Be(transactionId);
        payment.GatewayResponse.Should().Be("Payment approved");
        payment.CompletedAt.Should().NotBeNull();
        payment.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkSuccess_ShouldThrowException_WithEmptyTransactionId()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkProcessing();

        // Act
        var act = () => payment.MarkSuccess("", "Response");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Transaction ID is required*");
    }

    [Fact]
    public void MarkSuccess_ShouldThrowException_FromFailedStatus()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkFailed("Test failure");

        // Act
        var act = () => payment.MarkSuccess("TXN-12345");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark payment as success*");
    }

    [Fact]
    public void MarkFailed_ShouldUpdateStatus_WithReason()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        var failureReason = "Insufficient funds";

        // Act
        payment.MarkFailed(failureReason);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be(failureReason);
        payment.FailedAt.Should().NotBeNull();
        payment.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkFailed_ShouldThrowException_FromSuccessStatus()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkProcessing();
        payment.MarkSuccess("TXN-12345");

        // Act
        var act = () => payment.MarkFailed("Test failure");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark successful payment as failed*");
    }

    [Fact]
    public void CanRetry_ShouldReturnTrue_ForFailedPayment()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkFailed("Test failure");

        // Act
        var canRetry = payment.CanRetry();

        // Assert
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_ShouldReturnTrue_ForInitiatedPayment()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);

        // Act
        var canRetry = payment.CanRetry();

        // Assert
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_ShouldReturnFalse_ForSuccessfulPayment()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkProcessing();
        payment.MarkSuccess("TXN-12345");

        // Act
        var canRetry = payment.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void CanRetry_ShouldReturnFalse_ForProcessingPayment()
    {
        // Arrange
        var payment = new Payment(Guid.NewGuid(), 99.99m);
        payment.MarkProcessing();

        // Act
        var canRetry = payment.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }
}
