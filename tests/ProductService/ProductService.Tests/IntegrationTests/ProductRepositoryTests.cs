using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;
using ProductService.Infrastructure.Repositories;
using Xunit;

namespace ProductService.Tests.IntegrationTests;

public class ProductRepositoryTests : IDisposable
{
    private readonly ProductDbContext _context;
    private readonly ProductRepository _repository;
    private readonly Guid _testCategoryId;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestProductDb_{Guid.NewGuid()}")
            .Options;

        _context = new ProductDbContext(options);
        _repository = new ProductRepository(_context);

        _testCategoryId = SeedDatabase();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddProduct_ToDatabase()
    {
        // Arrange
        var product = CreateTestProduct("New Product", "NEW-SKU-001");

        // Act
        var result = await _repository.CreateAsync(product);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        var savedProduct = await _context.Products.FindAsync(result.Id);
        savedProduct.Should().NotBeNull();
        savedProduct!.Name.Should().Be("New Product");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenExists()
    {
        // Arrange
        var product = CreateTestProduct("Test Product", "TEST-SKU-002");
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySkuAsync_ShouldReturnProduct_WhenExists()
    {
        // Arrange
        var product = CreateTestProduct("SKU Test", "UNIQUE-SKU-003");
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.GetBySkuAsync("UNIQUE-SKU-003");

        // Assert
        result.Should().NotBeNull();
        result!.SKU.Should().Be("UNIQUE-SKU-003");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyProduct()
    {
        // Arrange
        var product = CreateTestProduct("Original", "UPDATE-SKU-004");
        await _repository.CreateAsync(product);
        
        // Load fresh instance
        var productToUpdate = await _repository.GetByIdAsync(product.Id);

        // Act
        productToUpdate!.UpdateDetails("Updated Product", "Updated Description", 199.99m);
        await _repository.UpdateAsync(productToUpdate);

        // Assert
        var updated = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productToUpdate.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Product");
        updated.Price.Should().Be(199.99m);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteProduct()
    {
        // Arrange
        var product = CreateTestProduct("To Delete", "DELETE-SKU-005");
        await _repository.CreateAsync(product);

        // Act
        await _repository.DeleteAsync(product.Id);

        // Assert
        var deleted = await _repository.GetByIdAsync(product.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenSkuExists()
    {
        // Arrange
        var product = CreateTestProduct("Exists Test", "EXISTS-SKU-006");
        await _repository.CreateAsync(product);

        // Act
        var exists = await _repository.ExistsAsync("EXISTS-SKU-006");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenSkuNotExists()
    {
        // Act
        var exists = await _repository.ExistsAsync("NON-EXISTENT-SKU");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByCategoryIdAsync_ShouldReturnProductsInCategory()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", "CAT-SKU-001", _testCategoryId);
        var product2 = CreateTestProduct("Product 2", "CAT-SKU-002", _testCategoryId);
        await _repository.CreateAsync(product1);
        await _repository.CreateAsync(product2);

        // Act
        var results = await _repository.GetByCategoryIdAsync(_testCategoryId);

        // Assert
        results.Should().HaveCount(2);
        results.All(p => p.CategoryId == _testCategoryId).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnMatchingProducts()
    {
        // Arrange
        var product = CreateTestProduct("Laptop Computer", "SEARCH-SKU-001");
        await _repository.CreateAsync(product);

        // Act
        var results = await _repository.SearchAsync("Laptop");

        // Assert
        results.Should().NotBeEmpty();
        results.Any(p => p.Name.Contains("Laptop")).Should().BeTrue();
    }

    private Product CreateTestProduct(string name, string sku, Guid? categoryId = null)
    {
        return new Product(
            name,
            "Test Description",
            sku,
            99.99m,
            10,
            categoryId ?? _testCategoryId,
            Guid.NewGuid());
    }

    private Guid SeedDatabase()
    {
        var category = new Category("Test Category", "Test Description");
        _context.Categories.Add(category);
        _context.SaveChanges();
        return category.Id;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
