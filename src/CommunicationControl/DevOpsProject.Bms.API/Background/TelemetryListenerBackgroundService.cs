using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DevOpsProject.Bms.API.Background;

public class TelemetryListenerBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisOptions _redisOptions;
    private readonly ILogger<TelemetryListenerBackgroundService> _logger;

    public TelemetryListenerBackgroundService(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        IOptions<RedisOptions> redisOptions,
        ILogger<TelemetryListenerBackgroundService> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _redisOptions = redisOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(_redisOptions.PublishChannel, async (channel, message) =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ITelemetryProcessor>();

                await processor.ProcessMessageAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process BMS message: {message}", message.ToString());
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}