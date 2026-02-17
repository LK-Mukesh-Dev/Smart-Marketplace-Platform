using FluentAssertions;
using Moq;
using ProductService.Application.Commands;
using ProductService.Application.Handlers;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests.UnitTests.Application;

public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _handler = new CreateProductHandler(_mockProductRepository.Object, _mockCategoryRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateProduct_WhenValidCommand()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category("Test Category", "Test Description");

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Description = "Test Description",
            SKU = "TEST-001",
            Price = 99.99m,
            StockQuantity = 10,
            CategoryId = categoryId,
            SellerId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(category);

        _mockProductRepository.Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .ReturnsAsync((Product p, CancellationToken ct) => p);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.SKU.Should().Be(command.SKU);
        result.Price.Should().Be(command.Price);
        _mockProductRepository.Verify(r => r.CreateAsync(It.IsAny<Product>(), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenSkuExists()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            SKU = "EXISTING-SKU",
            Name = "Test",
            Description = "Test",
            Price = 10,
            StockQuantity = 1,
            CategoryId = Guid.NewGuid(),
            SellerId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.ExistsAsync(command.SKU, default))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenCategoryNotFound()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test",
            Description = "Test",
            SKU = "TEST-001",
            Price = 10,
            StockQuantity = 1,
            CategoryId = Guid.NewGuid(),
            SellerId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Category?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<Common.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowException_WhenCategoryIsInactive()
    {
        // Arrange
        var category = new Category("Test", "Test");
        category.Deactivate();

        var command = new CreateProductCommand
        {
            Name = "Test",
            Description = "Test",
            SKU = "TEST-001",
            Price = 10,
            StockQuantity = 1,
            CategoryId = category.Id,
            SellerId = Guid.NewGuid()
        };

        _mockProductRepository.Setup(r => r.ExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        _mockCategoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(category);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inactive category*");
    }
}
