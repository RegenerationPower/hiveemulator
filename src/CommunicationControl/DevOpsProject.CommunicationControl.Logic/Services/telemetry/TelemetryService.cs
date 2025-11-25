using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Messages;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.Logic.Services.telemetry;

public class TelemetryService(
    IRedisKeyValueService redisService,
    IPublishService messageBus,
    ILogger<TelemetryService> logger,
    IOptionsSnapshot<RedisKeys> redisKeysSnapshot)
    : ITelemetryService
{
    private readonly IRedisKeyValueService _redisService = redisService;
    private readonly IPublishService _messageBus = messageBus;
    private readonly RedisKeys _redisKeys = redisKeysSnapshot.Value;
    private readonly ILogger<TelemetryService> _logger = logger;

    public async Task<DateTime> AddTelemetry(HiveTelemetryModel model)
    {
        string hiveKey = GetHiveKey(model.HiveID);
        bool result = await _redisService.UpdateAsync(hiveKey, (HiveModel hive) =>
        {
            hive.Telemetry = model;
        });

        if (result)
        {
            _logger.LogInformation("Telemetry updated for HiveID: {hiveId}. Updated telemetry timestamp: {timestamp}", model.HiveID, model.Timestamp);
        }
        else
        {
            _logger.LogError("Failed to update Telemetry - Redis update issue. HiveID: {hiveId}, Telemetry model: {@telemetry}", model.HiveID, model);
        }

        await _messageBus.Publish(new TelemetrySentMessage
        {
            HiveID = model.HiveID,
            Telemetry = model,
            IsSuccessfullySent = result
        });
        return model.Timestamp;
    }
    
    private string GetHiveKey(string hiveId)
    {
        return $"{_redisKeys.HiveKey}:{hiveId}";
    }
    
}