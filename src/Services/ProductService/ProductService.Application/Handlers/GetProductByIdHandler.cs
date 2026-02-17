using ProductService.Application.DTOs;
using ProductService.Application.Queries;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Handlers;

public class GetProductByIdHandler
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(query.Id, cancellationToken);
        
        if (product == null)
        {
            return null;
        }

        product.IncrementViewCount();
        await _productRepository.UpdateAsync(product, cancellationToken);

        return ProductDto.FromEntity(product);
    }
}
