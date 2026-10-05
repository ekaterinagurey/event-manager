using EventManager.Events.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventManager.Events.Infrastructure.Cache
{
    public class CacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<CacheService> _logger;
        private readonly IDatabase _db;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CacheService(IConnectionMultiplexer redis,
                            ILogger<CacheService> logger)
        {
            _redis = redis;
            _logger = logger;
            _db = _redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            try
            {
                RedisValue value = await _db.StringGetAsync(key);

                if (value.IsNullOrEmpty)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(value!, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Ошибка десериализации ключа {Key}. Запрос пойдет в базу данных.", key);
                return default;
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or IOException)
            {
                _logger.LogError(ex, "Ошибка чтения ключа '{Key}' из Redis. Запрос пойдет в базу данных.", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value, JsonOptions);
                await _db.StringSetAsync(key, serialized, expiration);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or IOException)
            {
                _logger.LogError(ex, "Ошибка записи ключа '{Key}' в Redis. Выполнение продолжается без кэша.", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or IOException)
            {
                _logger.LogError(ex, "Ошибка удаления ключа '{Key}' из Redis.", key);
            }
        }
    }
}
