using FluentAssertions;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;
using InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Tests.Integration;

public class StockReservationRepositoryIntegrationTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly IStockReservationRepository _repository;

    public StockReservationRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryDbContext(options);
        _repository = new StockReservationRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddReservationToDatabase()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var reservation = new StockReservation(productId, orderId, 10);

        // Act
        await _repository.CreateAsync(reservation);

        // Assert
        var savedReservation = await _context.StockReservations.FindAsync(reservation.Id);
        savedReservation.Should().NotBeNull();
        savedReservation!.ProductId.Should().Be(productId);
        savedReservation.OrderId.Should().Be(orderId);
        savedReservation.Quantity.Should().Be(10);
        savedReservation.Status.Should().Be(ReservationStatus.Reserved);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReservation()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var reservation = new StockReservation(productId, orderId, 10);
        await _repository.CreateAsync(reservation);

        // Act
        var result = await _repository.GetByIdAsync(reservation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reservation.Id);
        result.Quantity.Should().Be(10);
    }

    [Fact]
    public async Task GetByOrderIdAsync_ShouldReturnReservationForOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var reservation = new StockReservation(Guid.NewGuid(), orderId, 10);

        await _repository.CreateAsync(reservation);

        // Act
        var result = await _repository.GetByOrderIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateReservation()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var reservation = new StockReservation(productId, orderId, 10);
        await _repository.CreateAsync(reservation);

        // Act
        reservation.Confirm();
        await _repository.UpdateAsync(reservation);

        // Assert
        var updatedReservation = await _repository.GetByIdAsync(reservation.Id);
        updatedReservation!.Status.Should().Be(ReservationStatus.Confirmed);
        updatedReservation.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetExpiredReservationsAsync_ShouldReturnExpiredReservations()
    {
        // Arrange
        var expiredReservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 10, expirationMinutes: -10);
        var activeReservation = new StockReservation(Guid.NewGuid(), Guid.NewGuid(), 5, expirationMinutes: 30);

        await _repository.CreateAsync(expiredReservation);
        await _repository.CreateAsync(activeReservation);

        // Act
        var result = await _repository.GetExpiredReservationsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(expiredReservation.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
