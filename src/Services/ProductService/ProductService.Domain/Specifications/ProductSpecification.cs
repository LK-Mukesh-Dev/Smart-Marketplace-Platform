using ProductService.Domain.Entities;

namespace ProductService.Domain.Specifications;

public class ProductSpecification
{
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SellerId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductStatus? Status { get; set; }
    public bool? InStock { get; set; }
    public List<string>? Tags { get; set; }

    public IQueryable<Product> Apply(IQueryable<Product> query)
    {
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLowerInvariant();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchLower) || 
                p.Description.ToLower().Contains(searchLower) ||
                p.SKU.ToLower().Contains(searchLower));
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == CategoryId.Value);
        }

        if (SellerId.HasValue)
        {
            query = query.Where(p => p.SellerId == SellerId.Value);
        }

        if (MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= MinPrice.Value);
        }

        if (MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= MaxPrice.Value);
        }

        if (Status.HasValue)
        {
            query = query.Where(p => p.Status == Status.Value);
        }

        if (InStock.HasValue && InStock.Value)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        if (Tags != null && Tags.Any())
        {
            query = query.Where(p => p.Tags.Any(t => Tags.Contains(t)));
        }

        query = query.Where(p => !p.IsDeleted);

        return query;
    }
}
