using ProductService.Domain.Entities;

namespace ProductService.Application.Commands;

public class CreateProductCommand
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public Guid SellerId { get; set; }
    public List<string>? ImageUrls { get; set; }
    public List<string>? Tags { get; set; }
}
