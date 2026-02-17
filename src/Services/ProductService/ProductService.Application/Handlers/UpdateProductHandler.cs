using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class UpdateProductHandler
{
    private readonly IProductRepository _productRepository;

    public UpdateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(command.Id, cancellationToken);
        if (product == null)
        {
            throw new Common.Exceptions.NotFoundException($"Product with ID '{command.Id}' not found");
        }

        product.UpdateDetails(command.Name, command.Description, command.Price);
        product.UpdateStock(command.StockQuantity);

        if (command.DiscountPrice.HasValue)
        {
            product.SetDiscount(command.DiscountPrice.Value);
        }
        else
        {
            product.RemoveDiscount();
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        return ProductDto.FromEntity(product);
    }
}
