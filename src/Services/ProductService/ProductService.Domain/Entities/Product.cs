namespace ProductService.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public Guid SellerId { get; private set; }
    public ProductStatus Status { get; private set; }
    public List<string> ImageUrls { get; private set; } = new();
    public List<string> Tags { get; private set; } = new();
    public int ViewCount { get; private set; }
    public decimal? DiscountPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    private Product() { }

    public Product(
        string name,
        string description,
        string sku,
        decimal price,
        int stockQuantity,
        Guid categoryId,
        Guid sellerId,
        List<string>? imageUrls = null,
        List<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required", nameof(sku));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));

        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        SKU = sku;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        SellerId = sellerId;
        Status = ProductStatus.Active;
        ImageUrls = imageUrls ?? new List<string>();
        Tags = tags ?? new List<string>();
        ViewCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void UpdateDetails(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required", nameof(name));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));

        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDiscount(decimal discountPrice)
    {
        if (discountPrice <= 0 || discountPrice >= Price)
            throw new ArgumentException("Invalid discount price", nameof(discountPrice));

        DiscountPrice = discountPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDiscount()
    {
        DiscountPrice = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        Status = ProductStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal GetEffectivePrice() => DiscountPrice ?? Price;

    public bool IsInStock() => StockQuantity > 0 && !IsDeleted && Status == ProductStatus.Active;
}

public enum ProductStatus
{
    Active,
    Inactive,
    OutOfStock,
    Discontinued
}
