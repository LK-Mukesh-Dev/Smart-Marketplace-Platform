using EventBus.Abstractions;

namespace ProductService.Infrastructure.Events;

public class ProductCreatedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public Guid SellerId { get; set; }
}

public class ProductUpdatedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

public class ProductViewedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime ViewedAt { get; set; }
}

public class ProductDeletedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
}
