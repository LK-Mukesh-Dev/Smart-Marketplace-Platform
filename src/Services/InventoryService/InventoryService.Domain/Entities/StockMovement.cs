using InventoryService.Domain.Enums;

namespace InventoryService.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public int Quantity { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StockMovement() { }

    public StockMovement(
        Guid productId,
        StockMovementType movementType,
        int quantity,
        int quantityBefore,
        int quantityAfter,
        string? reference = null,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        MovementType = movementType;
        Quantity = quantity;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        Reference = reference;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }
}
