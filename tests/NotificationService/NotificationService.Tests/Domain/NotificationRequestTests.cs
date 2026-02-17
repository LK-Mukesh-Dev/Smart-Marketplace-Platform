using FluentAssertions;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Tests.Domain;

public class NotificationRequestTests
{
    [Fact]
    public void NotificationRequest_ShouldBeCreated_WithValidData()
    {
        // Arrange & Act
        var userId = Guid.NewGuid();
        var request = new NotificationRequest(
            userId,
            EventType.OrderPlaced,
            NotificationChannel.Email,
            "user@example.com",
            "Order Confirmation",
            "Your order has been placed successfully");

        // Assert
        request.Should().NotBeNull();
        request.Id.Should().NotBeEmpty();
        request.UserId.Should().Be(userId);
        request.EventType.Should().Be(EventType.OrderPlaced);
        request.Channel.Should().Be(NotificationChannel.Email);
        request.Recipient.Should().Be("user@example.com");
        request.Subject.Should().Be("Order Confirmation");
        request.Body.Should().Contain("placed successfully");
        request.Status.Should().Be(NotificationStatus.Pending);
        request.RetryCount.Should().Be(0);
        request.MaxRetries.Should().Be(3);
        request.IdempotencyKey.Should().NotBeNullOrEmpty();
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void NotificationRequest_ShouldThrowException_WithEmptyUserId()
    {
        // Arrange & Act
        var act = () => new NotificationRequest(
            Guid.Empty,
            EventType.OrderPlaced,
            NotificationChannel.Email,
            "user@example.com",
            "Subject",
            "Body");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User ID cannot be empty*");
    }

    [Fact]
    public void NotificationRequest_ShouldThrowException_WithEmptyRecipient()
    {
        // Arrange & Act
        var act = () => new NotificationRequest(
            Guid.NewGuid(),
            EventType.OrderPlaced,
            NotificationChannel.Email,
            "",
            "Subject",
            "Body");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Recipient cannot be empty*");
    }

    [Fact]
    public void NotificationRequest_ShouldGenerateIdempotencyKey()
    {
        // Arrange & Act
        var userId = Guid.NewGuid();
        var request = new NotificationRequest(
            userId,
            EventType.PaymentSuccess,
            NotificationChannel.Sms,
            "+1234567890",
            "Payment Confirmed",
            "Your payment was successful");

        // Assert
        request.IdempotencyKey.Should().Be($"{userId}:PaymentSuccess:Sms");
    }

    [Fact]
    public void MarkAsSent_ShouldUpdateStatus_AndSetSentAt()
    {
        // Arrange
        var request = CreateTestNotification();

        // Act
        request.MarkAsSent();

        // Assert
        request.Status.Should().Be(NotificationStatus.Sent);
        request.SentAt.Should().NotBeNull();
        request.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsSent_ShouldThrowException_WhenAlreadySent()
    {
        // Arrange
        var request = CreateTestNotification();
        request.MarkAsSent();

        // Act
        var act = () => request.MarkAsSent();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already marked as sent*");
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatus_AndSetErrorMessage()
    {
        // Arrange
        var request = CreateTestNotification();
        var errorMessage = "SMTP server unavailable";

        // Act
        request.MarkAsFailed(errorMessage);

        // Assert
        request.Status.Should().Be(NotificationStatus.Failed);
        request.FailedAt.Should().NotBeNull();
        request.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void MarkAsFailed_ShouldThrowException_WhenAlreadySent()
    {
        // Arrange
        var request = CreateTestNotification();
        request.MarkAsSent();

        // Act
        var act = () => request.MarkAsFailed("Error");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark sent notification as failed*");
    }

    [Fact]
    public void IncrementRetryCount_ShouldIncreaseCount_AndUpdateStatus()
    {
        // Arrange
        var request = CreateTestNotification();

        // Act
        request.IncrementRetryCount();

        // Assert
        request.RetryCount.Should().Be(1);
        request.Status.Should().Be(NotificationStatus.Retrying);
    }

    [Fact]
    public void IncrementRetryCount_ShouldThrowException_WhenMaxRetriesExceeded()
    {
        // Arrange
        var request = CreateTestNotification();
        request.IncrementRetryCount();
        request.IncrementRetryCount();
        request.IncrementRetryCount();

        // Act
        var act = () => request.IncrementRetryCount();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Max retries*exceeded*");
    }

    [Fact]
    public void IncrementRetryCount_ShouldThrowException_WhenAlreadySent()
    {
        // Arrange
        var request = CreateTestNotification();
        request.MarkAsSent();

        // Act
        var act = () => request.IncrementRetryCount();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot retry sent notification*");
    }

    [Fact]
    public void CanRetry_ShouldReturnTrue_ForPendingNotification()
    {
        // Arrange
        var request = CreateTestNotification();

        // Act
        var canRetry = request.CanRetry();

        // Assert
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void CanRetry_ShouldReturnFalse_ForSentNotification()
    {
        // Arrange
        var request = CreateTestNotification();
        request.MarkAsSent();

        // Act
        var canRetry = request.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void CanRetry_ShouldReturnFalse_WhenMaxRetriesReached()
    {
        // Arrange
        var request = CreateTestNotification();
        request.IncrementRetryCount();
        request.IncrementRetryCount();
        request.IncrementRetryCount();

        // Act
        var canRetry = request.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpirationTimeExceeded()
    {
        // Arrange
        var request = CreateTestNotification();
        var expirationTime = TimeSpan.FromMilliseconds(-1);

        // Act
        var isExpired = request.IsExpired(expirationTime);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenNotExpired()
    {
        // Arrange
        var request = CreateTestNotification();
        var expirationTime = TimeSpan.FromHours(1);

        // Act
        var isExpired = request.IsExpired(expirationTime);

        // Assert
        isExpired.Should().BeFalse();
    }

    private NotificationRequest CreateTestNotification()
    {
        return new NotificationRequest(
            Guid.NewGuid(),
            EventType.OrderPlaced,
            NotificationChannel.Email,
            "test@example.com",
            "Test Subject",
            "Test Body");
    }
}
