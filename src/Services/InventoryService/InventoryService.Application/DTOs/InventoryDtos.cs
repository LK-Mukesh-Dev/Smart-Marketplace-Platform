namespace InventoryService.Application.DTOs;

public record InventoryItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
    public int TotalQuantity { get; init; }
    public int ReorderLevel { get; init; }
    public int MaxStockLevel { get; init; }
    public bool IsLowStock { get; init; }
    public DateTime LastRestocked { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateInventoryItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public int InitialQuantity { get; init; }
    public int ReorderLevel { get; init; } = 10;
    public int MaxStockLevel { get; init; } = 1000;
}

public record UpdateStockDto
{
    public int Quantity { get; init; }
    public string? Notes { get; init; }
}

public record ReserveStockDto
{
    public Guid ProductId { get; init; }
    public Guid OrderId { get; init; }
    public int Quantity { get; init; }
}

public record StockCheckDto
{
    public Guid ProductId { get; init; }
    public int QuantityAvailable { get; init; }
    public int QuantityReserved { get; init; }
    public bool CanFulfill { get; init; }
}
