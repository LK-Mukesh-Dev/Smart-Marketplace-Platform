using Common.Models;
using ProductService.Application.DTOs;
using ProductService.Application.Queries;
using ProductService.Domain.Interfaces;
using ProductService.Domain.Specifications;

namespace ProductService.Application.Handlers;

public class GetProductsHandler
{
    private readonly IProductRepository _productRepository;

    public GetProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResult<ProductDto>> HandleAsync(
        GetProductsQuery query, 
        CancellationToken cancellationToken = default)
    {
        var specification = new ProductSpecification
        {
            SearchTerm = query.SearchTerm,
            CategoryId = query.CategoryId,
            SellerId = query.SellerId,
            MinPrice = query.MinPrice,
            MaxPrice = query.MaxPrice,
            Status = query.Status,
            InStock = query.InStock,
            Tags = query.Tags
        };

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            specification,
            query.PageNumber,
            query.PageSize,
            query.SortBy,
            query.Ascending,
            cancellationToken);

        var productDtos = items.Select(ProductDto.FromEntity).ToList();

        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
