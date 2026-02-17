using Common.Models;
using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class CreateProductHandler
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateProductHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ProductDto> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        if (await _productRepository.ExistsAsync(command.SKU, cancellationToken))
        {
            throw new InvalidOperationException($"Product with SKU '{command.SKU}' already exists");
        }

        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new Common.Exceptions.NotFoundException($"Category with ID '{command.CategoryId}' not found");
        }

        if (!category.IsActive)
        {
            throw new InvalidOperationException("Cannot add product to inactive category");
        }

        var product = new Product(
            command.Name,
            command.Description,
            command.SKU,
            command.Price,
            command.StockQuantity,
            command.CategoryId,
            command.SellerId,
            command.ImageUrls,
            command.Tags);

        var createdProduct = await _productRepository.CreateAsync(product, cancellationToken);

        return ProductDto.FromEntity(createdProduct);
    }
}
