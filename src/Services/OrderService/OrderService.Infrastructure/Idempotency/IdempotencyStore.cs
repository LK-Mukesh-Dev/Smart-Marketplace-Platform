using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace OrderService.Infrastructure.Idempotency;

public interface IIdempotencyStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public class IdempotencyStore : IIdempotencyStore
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);

    public IdempotencyStore(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(value))
            return null;

        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var serializedValue = JsonSerializer.Serialize(value);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        await _cache.SetStringAsync(key, serializedValue, options, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(value);
    }
}
