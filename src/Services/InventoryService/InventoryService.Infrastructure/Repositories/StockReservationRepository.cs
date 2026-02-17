using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Data;

namespace InventoryService.Infrastructure.Repositories;

public class StockReservationRepository : IStockReservationRepository
{
    private readonly InventoryDbContext _context;

    public StockReservationRepository(InventoryDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.StockReservations
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.StockReservations
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.StockReservations
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < now)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockReservation> CreateAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.StockReservations.AddAsync(reservation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task UpdateAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        _context.StockReservations.Update(reservation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
