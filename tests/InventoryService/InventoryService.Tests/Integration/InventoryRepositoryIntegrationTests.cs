using FluentAssertions;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;
using InventoryService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Tests.Integration;

public class InventoryRepositoryIntegrationTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly IInventoryRepository _repository;

    public InventoryRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new InventoryDbContext(options);
        _repository = new InventoryRepository(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddInventoryItemToDatabase()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 100);

        // Act
        await _repository.CreateAsync(inventoryItem);

        // Assert
        var savedItem = await _context.InventoryItems.FindAsync(inventoryItem.Id);
        savedItem.Should().NotBeNull();
        savedItem!.ProductId.Should().Be(productId);
        savedItem.QuantityAvailable.Should().Be(100);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnInventoryItem()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 100);
        await _repository.CreateAsync(inventoryItem);

        // Act
        var result = await _repository.GetByIdAsync(inventoryItem.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(inventoryItem.Id);
        result.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task GetByProductIdAsync_ShouldReturnInventoryItem()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 100);
        await _repository.CreateAsync(inventoryItem);

        // Act
        var result = await _repository.GetByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateInventoryItem()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var inventoryItem = new InventoryItem(productId, "Test Product", "SKU123", 100);
        await _repository.CreateAsync(inventoryItem);

        // Act
        inventoryItem.Reserve(50);
        await _repository.UpdateAsync(inventoryItem);

        // Assert
        var updatedItem = await _repository.GetByIdAsync(inventoryItem.Id);
        updatedItem!.QuantityAvailable.Should().Be(50);
        updatedItem.QuantityReserved.Should().Be(50);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_ShouldReturnOnlyLowStockItems()
    {
        // Arrange
        var item1 = new InventoryItem(Guid.NewGuid(), "Product 1", "SKU1", 5, reorderLevel: 10);
        var item2 = new InventoryItem(Guid.NewGuid(), "Product 2", "SKU2", 50, reorderLevel: 10);
        await _repository.CreateAsync(item1);
        await _repository.CreateAsync(item2);

        // Act
        var result = await _repository.GetLowStockItemsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().ProductId.Should().Be(item1.ProductId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
