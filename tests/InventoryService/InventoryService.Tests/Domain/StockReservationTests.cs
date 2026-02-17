using FluentAssertions;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;

namespace InventoryService.Tests.Domain;

public class StockReservationTests
{
    [Fact]
    public void StockReservation_ShouldBeCreated_WithDefaultExpiration()
    {
        // Arrange & Act
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var reservation = new StockReservation(productId, orderId, 10);

        // Assert
        reservation.Should().NotBeNull();
        reservation.Id.Should().NotBeEmpty();
        reservation.ProductId.Should().Be(productId);
        reservation.OrderId.Should().Be(orderId);
        reservation.Quantity.Should().Be(10);
        reservation.Status.Should().Be(ReservationStatus.Reserved);
        reservation.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var reservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        reservation.Confirm();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Release_ShouldChangeStatusToReleased()
    {
        // Arrange
        var reservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        reservation.Release("Order cancelled");

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.ReleasedAt.Should().NotBeNull();
        reservation.Reason.Should().Be("Order cancelled");
    }

    [Fact]
    public void MarkExpired_ShouldChangeStatusToExpired()
    {
        // Arrange
        var reservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        reservation.MarkExpired();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Expired);
        reservation.ReleasedAt.Should().NotBeNull();
        reservation.Reason.Should().Be("Reservation expired");
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpirationTimePassed()
    {
        // Arrange
        var reservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 10, expirationMinutes: -1);

        // Assert
        reservation.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenNotYetExpired()
    {
        // Arrange
        var reservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 10, expirationMinutes: 30);

        // Assert
        reservation.IsExpired.Should().BeFalse();
    }
}
