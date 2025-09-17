using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Settings;
using StackExchange.Redis;
using System.Text.Json;

namespace NotificationService.Infrastructure.Caching;

/// <summary>
/// Redis implementation of the cache service
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IOptions<RedisSettings> settings)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _settings = settings.Value;
        _database = _connectionMultiplexer.GetDatabase(_settings.Database);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var value = await _database.StringGetAsync(fullKey);
        
        if (!value.HasValue)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch
        {
            // If deserialization fails, remove the corrupted key
            await _database.KeyDeleteAsync(fullKey);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
        
        var expiry = expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);
        
        await _database.StringSetAsync(fullKey, serializedValue, expiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        await _database.KeyDeleteAsync(fullKey);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        return await _database.KeyExistsAsync(fullKey);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var fullPattern = GetFullKey(pattern);
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        var keys = server.Keys(database: _settings.Database, pattern: fullPattern);
        
        var keyArray = keys.ToArray();
        if (keyArray.Length > 0)
        {
            await _database.KeyDeleteAsync(keyArray);
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var fullKeys = keys.Select(GetFullKey).ToArray();
        var redisKeys = fullKeys.Select(k => new RedisKey(k)).ToArray();
        
        var values = await _database.StringGetAsync(redisKeys);
        var result = new Dictionary<string, T?>();
        
        for (int i = 0; i < keys.Count(); i++)
        {
            var originalKey = keys.ElementAt(i);
            var value = values[i];
            
            if (value.HasValue)
            {
                try
                {
                    result[originalKey] = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                }
                catch
                {
                    // If deserialization fails, add null value
                    result[originalKey] = default;
                    // Remove corrupted key
                    await _database.KeyDeleteAsync(fullKeys[i]);
                }
            }
            else
            {
                result[originalKey] = default;
            }
        }
        
        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var expiry = expiration ?? TimeSpan.FromMinutes(_settings.DefaultExpirationMinutes);
        var tasks = new List<Task>();
        
        foreach (var kvp in keyValuePairs)
        {
            var fullKey = GetFullKey(kvp.Key);
            var serializedValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
            tasks.Add(_database.StringSetAsync(fullKey, serializedValue, expiry));
        }
        
        await Task.WhenAll(tasks);
    }

    private string GetFullKey(string key)
    {
        return $"{_settings.KeyPrefix}{key}";
    }
}