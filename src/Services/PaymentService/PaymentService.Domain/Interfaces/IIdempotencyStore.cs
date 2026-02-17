namespace PaymentService.Domain.Interfaces;

public interface IIdempotencyStore
{
    Task<bool> ExistsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task SaveAsync(Guid orderId, Guid paymentId, CancellationToken cancellationToken = default);
    Task<Guid?> GetPaymentIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
