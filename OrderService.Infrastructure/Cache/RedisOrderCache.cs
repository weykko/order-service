using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using StackExchange.Redis;

namespace OrderService.Infrastructure.Cache;

/// <summary>
/// Двухуровневый кеш заказов: L1 — in-memory (короткий TTL), L2 — Redis.
/// </summary>
public class RedisOrderCache : IOrderCache
{
    private const string KeyPrefix = "order:";
    private static readonly TimeSpan MemoryTtl = TimeSpan.FromMinutes(1);

    private readonly IDatabase _redis;
    private readonly IMemoryCache _memoryCache;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisOrderCache(IConnectionMultiplexer redis, IMemoryCache memoryCache)
    {
        _redis = redis.GetDatabase();
        _memoryCache = memoryCache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<OrderResponseDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(BuildKey(id), out OrderResponseDto? cached))
            return cached;

        var redisValue = await _redis.StringGetAsync(BuildKey(id));
        if (!redisValue.HasValue)
            return null;

        var order = JsonSerializer.Deserialize<OrderResponseDto>(redisValue!, _jsonOptions);
        if (order != null)
            _memoryCache.Set(BuildKey(id), order, MemoryTtl);

        return order;
    }

    public async Task SetAsync(Guid id, OrderResponseDto order, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(order, _jsonOptions);
        await _redis.StringSetAsync(BuildKey(id), serialized, ttl);
        _memoryCache.Set(BuildKey(id), order, MemoryTtl);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _redis.KeyDeleteAsync(BuildKey(id));
        _memoryCache.Remove(BuildKey(id));
    }

    private static string BuildKey(Guid id) => $"{KeyPrefix}{id}";
}
