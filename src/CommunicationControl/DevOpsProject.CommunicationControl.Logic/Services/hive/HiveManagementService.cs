using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Exceptions;
using DevOpsProject.Shared.Messages;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.Logic.Services;

public class HiveManagementService : IHiveManagementService
{
    
    private readonly ISpatialService _spatialService;
    private readonly IRedisKeyValueService _redisService;
    private readonly RedisKeys _redisKeys;
    private readonly IPublishService _messageBus;
    private readonly ILogger<HiveManagementService> _logger;

    public HiveManagementService(ISpatialService spatialService, IRedisKeyValueService redisService, IOptionsSnapshot<RedisKeys> redisKeysSnapshot, 
        IPublishService messageBus, ILogger<HiveManagementService> logger)
    {
        _spatialService = spatialService;
        _redisService = redisService;
        _redisKeys = redisKeysSnapshot.Value;
        _messageBus = messageBus;
        _logger = logger;
    }
    
    public async Task<HiveModel> GetHiveModel(string hiveId)
    {
        var result = await _redisService.GetAsync<HiveModel>(GetHiveKey(hiveId));
        return result;
    }
    
    
    public async Task<List<HiveModel>> GetAllHives()
    {
        var result = await _redisService.GetAllAsync<HiveModel>($"{_redisKeys.HiveKey}:");
        return result;
    }
    
    public async Task<bool> DisconnectHive(string hiveId)
    {
        bool isSuccessfullyDisconnected = false;
        try
        {
            var result = await _redisService.DeleteAsync(GetHiveKey(hiveId));
            isSuccessfullyDisconnected = result;
            return result;
        }
        finally
        {
            await _messageBus.Publish(new HiveDisconnectedMessage
            {
                HiveID = hiveId,
                IsSuccessfullyDisconnected = isSuccessfullyDisconnected
            });
        }
    }
    
    public async Task<HiveOperationalArea> ConnectHive(HiveModel model)
        {
            bool isHiveAlreadyConnected = await IsHiveConnected(model.HiveID);
            if (isHiveAlreadyConnected)
            {
                _logger.LogWarning("Reconnect Hive request: {@model}", model);
            }
            else
            {
                _logger.LogInformation("Trying to connect Hive: {@model}", model);
            }
            bool result = await _redisService.SetAsync(GetHiveKey(model.HiveID), model);
            if (result)
            {
                _logger.LogInformation("Successfully connected Hive: {@model}", model);
                var operationalArea = _spatialService.GetHiveOperationalArea(model);
                if (isHiveAlreadyConnected)
                {
                    await _messageBus.Publish(new HiveReconnectedMessage
                    {
                        HiveID = model.HiveID,
                        Hive = model,
                        InitialOperationalArea = operationalArea,
                        IsSuccessfullyReconnected = result
                    });
                }
                else
                {
                    await _messageBus.Publish(new HiveConnectedMessage
                    {
                        HiveID = model.HiveID,
                        Hive = model,
                        InitialOperationalArea = operationalArea,
                        IsSuccessfullyConnected = result
                    });
                }
                return operationalArea;
            }
            else
            {
                _logger.LogError("Failed to connect Hive: {@model}", model);
                throw new HiveConnectionException($"Failed to connect hive for HiveId: {model.HiveID}");
            }
        }

        public async Task<bool> IsHiveConnected(string hiveId)
        {
            string hiveKey = GetHiveKey(hiveId);
            return await _redisService.CheckIfKeyExists(hiveKey);
        }

        private string GetHiveKey(string hiveId)
        {
            return $"{_redisKeys.HiveKey}:{hiveId}";
        }
}