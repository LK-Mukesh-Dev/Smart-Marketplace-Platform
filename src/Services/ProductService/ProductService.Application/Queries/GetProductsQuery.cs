using ProductService.Domain.Entities;

namespace ProductService.Application.Queries;

public class GetProductsQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SellerId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductStatus? Status { get; set; }
    public bool? InStock { get; set; }
    public List<string>? Tags { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool Ascending { get; set; } = false;
}
