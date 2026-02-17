using FluentAssertions;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;
using InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Tests.Integration;

public class StockMovementRepositoryIntegrationTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly IStockMovementRepository _repository;

    public StockMovementRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryDbContext(options);
        _repository = new StockMovementRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddStockMovementToDatabase()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var movement = new StockMovement(
            productId,
            StockMovementType.StockIn,
            50,
            100,
            150,
            "REF123",
            "Stock replenishment");

        // Act
        await _repository.CreateAsync(movement);

        // Assert
        var savedMovement = await _context.StockMovements.FindAsync(movement.Id);
        savedMovement.Should().NotBeNull();
        savedMovement!.ProductId.Should().Be(productId);
        savedMovement.MovementType.Should().Be(StockMovementType.StockIn);
        savedMovement.Quantity.Should().Be(50);
    }

    [Fact]
    public async Task GetByProductIdAsync_ShouldReturnMovementsForProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var movement1 = new StockMovement(productId, StockMovementType.StockIn, 50, 100, 150, "REF1", "Restock");
        var movement2 = new StockMovement(productId, StockMovementType.StockOut, 20, 150, 130, "SALE1", "Sale");
        var movement3 = new StockMovement(Guid.NewGuid(), StockMovementType.StockIn, 30, 50, 80, "REF2", "Restock");

        await _repository.CreateAsync(movement1);
        await _repository.CreateAsync(movement2);
        await _repository.CreateAsync(movement3);

        // Act
        var result = await _repository.GetByProductIdAsync(productId);

        // Assert
        result.Should().HaveCount(2);
        result.All(m => m.ProductId == productId).Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
