using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);
    Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid productId, CancellationToken cancellationToken = default);
}
