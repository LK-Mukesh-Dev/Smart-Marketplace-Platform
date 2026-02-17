using FluentAssertions;
using ProductService.Domain.Entities;
using Xunit;

namespace ProductService.Tests.UnitTests.Domain;

public class ProductEntityTests
{
    [Fact]
    public void Product_ShouldBeCreated_WithValidData()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var sku = "TEST-001";
        var price = 99.99m;
        var stock = 10;
        var categoryId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        // Act
        var product = new Product(name, description, sku, price, stock, categoryId, sellerId);

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.SKU.Should().Be(sku);
        product.Price.Should().Be(price);
        product.StockQuantity.Should().Be(stock);
        product.CategoryId.Should().Be(categoryId);
        product.SellerId.Should().Be(sellerId);
        product.Status.Should().Be(ProductStatus.Active);
        product.ViewCount.Should().Be(0);
        product.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "Description", "SKU-001", 10.0, 5)]
    [InlineData("Name", "Description", "", 10.0, 5)]
    [InlineData("Name", "Description", "SKU-001", 0, 5)]
    [InlineData("Name", "Description", "SKU-001", -10, 5)]
    [InlineData("Name", "Description", "SKU-001", 10, -1)]
    public void Product_ShouldThrowException_WithInvalidData(
        string name, string description, string sku, decimal price, int stock)
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        // Act
        Action act = () => new Product(name, description, sku, price, stock, categoryId, sellerId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateProduct()
    {
        // Arrange
        var product = CreateTestProduct();
        var newName = "Updated Product";
        var newDescription = "Updated Description";
        var newPrice = 149.99m;

        // Act
        product.UpdateDetails(newName, newDescription, newPrice);

        // Assert
        product.Name.Should().Be(newName);
        product.Description.Should().Be(newDescription);
        product.Price.Should().Be(newPrice);
    }

    [Fact]
    public void DeductStock_ShouldReduceQuantity()
    {
        // Arrange
        var product = CreateTestProduct();
        var initialStock = product.StockQuantity;
        var deductAmount = 3;

        // Act
        product.DeductStock(deductAmount);

        // Assert
        product.StockQuantity.Should().Be(initialStock - deductAmount);
    }

    [Fact]
    public void DeductStock_ShouldThrowException_WhenInsufficientStock()
    {
        // Arrange
        var product = CreateTestProduct();
        var deductAmount = product.StockQuantity + 1;

        // Act
        Action act = () => product.DeductStock(deductAmount);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient stock");
    }

    [Fact]
    public void AddStock_ShouldIncreaseQuantity()
    {
        // Arrange
        var product = CreateTestProduct();
        var initialStock = product.StockQuantity;
        var addAmount = 5;

        // Act
        product.AddStock(addAmount);

        // Assert
        product.StockQuantity.Should().Be(initialStock + addAmount);
    }

    [Fact]
    public void SetDiscount_ShouldApplyDiscount()
    {
        // Arrange
        var product = CreateTestProduct();
        var discountPrice = 79.99m;

        // Act
        product.SetDiscount(discountPrice);

        // Assert
        product.DiscountPrice.Should().Be(discountPrice);
        product.GetEffectivePrice().Should().Be(discountPrice);
    }

    [Fact]
    public void SetDiscount_ShouldThrowException_WhenInvalidPrice()
    {
        // Arrange
        var product = CreateTestProduct();
        var invalidDiscount = product.Price + 10;

        // Act
        Action act = () => product.SetDiscount(invalidDiscount);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RemoveDiscount_ShouldClearDiscount()
    {
        // Arrange
        var product = CreateTestProduct();
        product.SetDiscount(79.99m);

        // Act
        product.RemoveDiscount();

        // Assert
        product.DiscountPrice.Should().BeNull();
        product.GetEffectivePrice().Should().Be(product.Price);
    }

    [Fact]
    public void IncrementViewCount_ShouldIncreaseCount()
    {
        // Arrange
        var product = CreateTestProduct();
        var initialCount = product.ViewCount;

        // Act
        product.IncrementViewCount();

        // Assert
        product.ViewCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public void Deactivate_ShouldSetStatusToInactive()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.Deactivate();

        // Assert
        product.Status.Should().Be(ProductStatus.Inactive);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        product.SoftDelete();

        // Assert
        product.IsDeleted.Should().BeTrue();
        product.Status.Should().Be(ProductStatus.Inactive);
    }

    [Fact]
    public void IsInStock_ShouldReturnTrue_WhenActiveAndHasStock()
    {
        // Arrange
        var product = CreateTestProduct();

        // Act
        var inStock = product.IsInStock();

        // Assert
        inStock.Should().BeTrue();
    }

    [Fact]
    public void IsInStock_ShouldReturnFalse_WhenNoStock()
    {
        // Arrange
        var product = CreateTestProduct();
        product.DeductStock(product.StockQuantity);

        // Act
        var inStock = product.IsInStock();

        // Assert
        inStock.Should().BeFalse();
    }

    private static Product CreateTestProduct()
    {
        return new Product(
            "Test Product",
            "Test Description",
            "TEST-001",
            99.99m,
            10,
            Guid.NewGuid(),
            Guid.NewGuid());
    }
}
