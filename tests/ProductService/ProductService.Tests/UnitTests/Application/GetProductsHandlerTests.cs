using FluentAssertions;
using Moq;
using ProductService.Application.Handlers;
using ProductService.Application.Queries;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Domain.Specifications;
using Xunit;

namespace ProductService.Tests.UnitTests.Application;

public class GetProductsHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly GetProductsHandler _handler;

    public GetProductsHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new GetProductsHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPagedProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProduct("Product 1", "SKU-001", 99.99m),
            CreateTestProduct("Product 2", "SKU-002", 149.99m)
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<ProductSpecification>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                default))
            .ReturnsAsync((products, 2));

        var query = new GetProductsQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_ShouldApplyFilters_Correctly()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var query = new GetProductsQuery
        {
            CategoryId = categoryId,
            MinPrice = 50,
            MaxPrice = 200,
            InStock = true
        };

        _mockRepository.Setup(r => r.GetPagedAsync(
                It.Is<ProductSpecification>(s => 
                    s.CategoryId == categoryId &&
                    s.MinPrice == 50 &&
                    s.MaxPrice == 200 &&
                    s.InStock == true),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                default))
            .ReturnsAsync((new List<Product>(), 0));

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        _mockRepository.Verify(r => r.GetPagedAsync(
            It.IsAny<ProductSpecification>(),
            query.PageNumber,
            query.PageSize,
            query.SortBy,
            query.Ascending,
            default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyList_WhenNoProducts()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<ProductSpecification>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                default))
            .ReturnsAsync((new List<Product>(), 0));

        var query = new GetProductsQuery();

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    private static Product CreateTestProduct(string name, string sku, decimal price)
    {
        return new Product(
            name,
            "Test Description",
            sku,
            price,
            10,
            Guid.NewGuid(),
            Guid.NewGuid());
    }
}
