using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Stockhub.Common.Application.Caching;

namespace Stockhub.Common.Infrastructure.Caching;

internal sealed class CacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        byte[]? bytes = await cache.GetAsync(key, cancellationToken);
        return bytes is null ? default : Deserialize<T>(bytes);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        byte[] bytes = Serialize(value);
        return cache.SetAsync(key, bytes, CacheOptions.Create(expiration), cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RefreshAsync(key, cancellationToken);

    private static byte[] Serialize<T>(T value)
    {
        if (value is null)
        {
            return [];
        }

        return JsonSerializer.SerializeToUtf8Bytes(value);
    }

    private static T? Deserialize<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes);
    }
}
