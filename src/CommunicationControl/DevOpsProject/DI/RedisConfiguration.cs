using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.CommunicationControl.Logic.Services;
using DevOpsProject.Shared.Configuration;
using StackExchange.Redis;

namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class RedisConfiguration
    {
        public static IServiceCollection AddRedis(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var redisConfiguration = configuration.GetSection("Redis").Get<RedisOptions>();
            var redis = ConnectionMultiplexer.Connect(redisConfiguration.ConnectionString);

            serviceCollection.AddSingleton<IConnectionMultiplexer>(redis);

            serviceCollection.Configure<RedisOptions>(
                configuration.GetSection("Redis"));

            serviceCollection.Configure<RedisKeys>(
                configuration.GetSection("RedisKeys"));

            serviceCollection.AddTransient<IRedisKeyValueService, RedisKeyValueService>();

            // add message bus here - currently using Redis implementation
            serviceCollection.AddTransient<IPublishService, RedisPublishService>();

            return serviceCollection;
        }
    }
}
