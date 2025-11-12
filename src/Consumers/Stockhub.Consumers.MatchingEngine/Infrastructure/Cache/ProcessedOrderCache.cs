using Microsoft.Extensions.Caching.Memory;
using Stockhub.Consumers.MatchingEngine.Application.Cache;

namespace Stockhub.Consumers.MatchingEngine.Infrastructure.Cache;

internal sealed class ProcessedOrderCache(IMemoryCache cache) : IProcessedOrderCache
{
    private const long MaxEntries = 100_000;
    private readonly IMemoryCache _cache = cache;
    private readonly MemoryCacheEntryOptions _options = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        Size = 1
    };

    public bool Exists(Guid orderId) => _cache.TryGetValue(orderId, out _);

    public void Add(Guid orderId)
    {
        if (_cache is MemoryCache mem && mem.Count >= MaxEntries)
        {
            mem.Compact(0.1);
        }

        _cache.Set(orderId, true, _options);
    }

    public void Remove(Guid orderId)
    {
        _cache.Remove(orderId);
    }
}
