using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces;

public interface IStockMovementRepository
{
    Task<List<StockMovement>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<StockMovement> CreateAsync(StockMovement movement, CancellationToken cancellationToken = default);
}
