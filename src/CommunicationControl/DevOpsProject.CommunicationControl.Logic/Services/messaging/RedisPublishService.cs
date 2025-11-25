using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace DevOpsProject.CommunicationControl.Logic.Services
{
    public class RedisPublishService : IPublishService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisOptions _redisOptions;

        public RedisPublishService(IConnectionMultiplexer connectionMultiplexer, IOptionsMonitor<RedisOptions> redisOptions)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _redisOptions = redisOptions.CurrentValue;
        }

        public async Task Publish<T>(T message)
        {
            var pubsub = _connectionMultiplexer.GetSubscriber();
            var messageJson = JsonSerializer.Serialize(message);

            if (_redisOptions.PublishChannel != null)
            {
                await pubsub.PublishAsync(_redisOptions.PublishChannel, messageJson);
            }
            else
            {
                throw new Exception($"Error while attempting to publish message to Message Bus, publish channel: {_redisOptions.PublishChannel}");
            }
        }
    }
}
