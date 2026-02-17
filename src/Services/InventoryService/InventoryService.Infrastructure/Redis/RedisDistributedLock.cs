using InventoryService.Domain.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace InventoryService.Infrastructure.Redis;

public class RedisDistributedLock : IDistributedLock
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLock> _logger;
    private readonly Dictionary<string, string> _lockTokens = new();

    public RedisDistributedLock(IConnectionMultiplexer redis, ILogger<RedisDistributedLock> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var lockToken = Guid.NewGuid().ToString();
            var lockKey = $"lock:{key}";

            // Try to set the lock with NX (only if not exists) and EX (expiration)
            var acquired = await db.StringSetAsync(lockKey, lockToken, expiration, When.NotExists);

            if (acquired)
            {
                _lockTokens[key] = lockToken;
                _logger.LogDebug("Lock acquired for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Failed to acquire lock for key: {Key}", key);
            }

            return acquired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_lockTokens.TryGetValue(key, out var lockToken))
            {
                _logger.LogWarning("No lock token found for key: {Key}", key);
                return false;
            }

            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";

            // Lua script to ensure we only delete the lock if we own it
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end
            ";

            var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockToken });

            _lockTokens.Remove(key);

            var released = (int)result == 1;
            if (released)
            {
                _logger.LogDebug("Lock released for key: {Key}", key);
            }
            else
            {
                _logger.LogWarning("Failed to release lock for key: {Key} - token mismatch", key);
            }

            return released;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key: {Key}", key);
            return false;
        }
    }
}
