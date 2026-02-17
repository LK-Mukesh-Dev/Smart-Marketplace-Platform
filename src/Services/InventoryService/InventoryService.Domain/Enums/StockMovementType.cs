namespace InventoryService.Domain.Enums;

public enum StockMovementType
{
    StockIn = 0,
    StockOut = 1,
    Reserved = 2,
    Released = 3,
    Adjustment = 4,
    Damaged = 5,
    Returned = 6
}
