using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default);
    Task<StockReservation> CreateAsync(StockReservation reservation, CancellationToken cancellationToken = default);
    Task UpdateAsync(StockReservation reservation, CancellationToken cancellationToken = default);
}
