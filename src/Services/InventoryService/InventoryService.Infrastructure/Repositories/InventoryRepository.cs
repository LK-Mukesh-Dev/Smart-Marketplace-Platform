using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;

namespace InventoryService.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;

    public InventoryRepository(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .Where(i => i.QuantityAvailable <= i.ReorderLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        await _context.InventoryItems.AddAsync(item, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _context.InventoryItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .AnyAsync(i => i.ProductId == productId, cancellationToken);
    }
}
