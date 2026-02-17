using ProductService.Domain.Entities;

namespace ProductService.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public int StockQuantity { get; set; }
    public bool InStock { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid SellerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ProductDto FromEntity(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            EffectivePrice = product.GetEffectivePrice(),
            StockQuantity = product.StockQuantity,
            InStock = product.IsInStock(),
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            SellerId = product.SellerId,
            Status = product.Status.ToString(),
            ImageUrls = product.ImageUrls,
            Tags = product.Tags,
            ViewCount = product.ViewCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
