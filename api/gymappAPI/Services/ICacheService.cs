namespace gymappAPI.Services;

public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);

    Task RemoveAsync(string key);
}
