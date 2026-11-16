using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace gymappAPI.Services;

/// <summary>
/// Thin JSON wrapper over IDistributedCache (Redis, or in-memory when Redis isn't configured).
/// Cache failures are logged and never break a request: we just fall back to the database.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            if (cached is not null)
                return JsonSerializer.Deserialize<T>(cached)!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for {Key}", key);
        }

        var value = await factory();

        // Don't cache "not found" results, so newly created data shows up immediately.
        if (value is null)
            return value;

        try
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for {Key}", key);
        }

        return value;
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache invalidation failed for {Key}", key);
        }
    }
}
