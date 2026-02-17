using PaymentService.Domain.Interfaces;
using System.Collections.Concurrent;

namespace PaymentService.Infrastructure.Idempotency;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<Guid, Guid> _store = new();

    public Task<bool> ExistsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(orderId));
    }

    public Task SaveAsync(Guid orderId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        _store.TryAdd(orderId, paymentId);
        return Task.CompletedTask;
    }

    public Task<Guid?> GetPaymentIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(orderId, out var paymentId);
        return Task.FromResult(paymentId == Guid.Empty ? (Guid?)null : paymentId);
    }
}
