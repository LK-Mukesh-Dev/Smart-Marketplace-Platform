namespace InventoryService.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public string Sku { get; private set; }
    public int QuantityAvailable { get; private set; }
    public int QuantityReserved { get; private set; }
    public int ReorderLevel { get; private set; }
    public int MaxStockLevel { get; private set; }
    public DateTime LastRestocked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private InventoryItem() { }

    public InventoryItem(Guid productId, string productName, string sku, int initialQuantity, int reorderLevel = 10, int maxStockLevel = 1000)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));
        if (initialQuantity < 0)
            throw new ArgumentException("Initial quantity cannot be negative", nameof(initialQuantity));
        if (reorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative", nameof(reorderLevel));

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        QuantityAvailable = initialQuantity;
        QuantityReserved = 0;
        ReorderLevel = reorderLevel;
        MaxStockLevel = maxStockLevel;
        LastRestocked = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public int TotalQuantity => QuantityAvailable + QuantityReserved;

    public bool IsLowStock => QuantityAvailable <= ReorderLevel;

    public bool CanReserve(int quantity)
    {
        return quantity > 0 && QuantityAvailable >= quantity;
    }

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new InvalidOperationException($"Insufficient stock. Available: {QuantityAvailable}, Requested: {quantity}");

        QuantityAvailable -= quantity;
        QuantityReserved += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (quantity > QuantityReserved)
            throw new InvalidOperationException($"Cannot release {quantity} units. Only {QuantityReserved} reserved");

        QuantityReserved -= quantity;
        QuantityAvailable += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmReservation(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (quantity > QuantityReserved)
            throw new InvalidOperationException($"Cannot confirm {quantity} units. Only {QuantityReserved} reserved");

        QuantityReserved -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        QuantityAvailable += quantity;
        LastRestocked = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException($"Cannot remove {quantity} units. Only {QuantityAvailable} available");

        QuantityAvailable -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustStock(int newQuantity)
    {
        if (newQuantity < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(newQuantity));

        QuantityAvailable = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateReorderLevel(int newReorderLevel)
    {
        if (newReorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative", nameof(newReorderLevel));

        ReorderLevel = newReorderLevel;
        UpdatedAt = DateTime.UtcNow;
    }
}
