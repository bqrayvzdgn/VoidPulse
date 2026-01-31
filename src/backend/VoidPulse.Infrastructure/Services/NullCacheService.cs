using VoidPulse.Application.Interfaces;

namespace VoidPulse.Infrastructure.Services;

public class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) => Task.CompletedTask;

    public Task RemoveAsync(string key) => Task.CompletedTask;
}
