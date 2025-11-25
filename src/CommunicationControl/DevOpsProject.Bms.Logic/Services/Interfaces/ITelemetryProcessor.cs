using StackExchange.Redis;

namespace DevOpsProject.Bms.Logic.Services.Interfaces;

public interface ITelemetryProcessor
{
    Task ProcessMessageAsync(RedisValue rawMessage, CancellationToken ct = default);
}