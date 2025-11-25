using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace DevOpsProject.CommunicationControl.Logic.Services
{
    public class RedisKeyValueService : IRedisKeyValueService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisKeyValueService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var db = _connectionMultiplexer.GetDatabase();
            var json = await db.StringGetAsync(key);

            return json.HasValue ? JsonSerializer.Deserialize<T>(json) : default;
        }

        public async Task<List<T>> GetAllAsync<T>(string keyPattern)
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{keyPattern}*");

            var resultList = new List<T>();
            foreach ( var key in keys)
            {
                var entry = await GetAsync<T>(key);
                if (entry != null)
                {
                    resultList.Add(entry);
                }
            }

            return resultList;
        }

        public async Task<bool> UpdateAsync<T>(string key, Action<T> updateAction)
        {
            var db = _connectionMultiplexer.GetDatabase();
            var jsonData = await db.StringGetAsync(key);
            if (!jsonData.HasValue) return false;

            var obj = JsonSerializer.Deserialize<T>(jsonData);
            if (obj == null) return false;

            updateAction(obj);
            return await SetAsync(key, obj);
        }

        public async Task<bool> SetAsync<T>(string key, T value)
        {
            var db = _connectionMultiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(value);

            return await db.StringSetAsync(key, json);
        }

        public async Task<bool> CheckIfKeyExists(string key)
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.KeyExistsAsync(key);
        }

        public async Task<bool> DeleteAsync(string key)
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }
    }
}
