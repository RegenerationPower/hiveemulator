using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.CommunicationControl.Logic.Services.Interference;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Exceptions;
using DevOpsProject.Shared.Messages;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.HiveMind;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.Logic.Services;

public class InterferenceManagementService: IInterferenceManagementService
{
    private readonly IRedisKeyValueService _redisService;
    private readonly RedisKeys _redisKeys;
    private readonly CommunicationControlHttpClient _hiveHttpClient;
    private readonly IPublishService _messageBus;
    private readonly ILogger<InterferenceManagementService> _logger;
    private readonly IOptionsMonitor<ComControlCommunicationConfiguration> _communicationControlConfiguration;
    private readonly IHiveManagementService _hiveManagementService;

    public InterferenceManagementService(IRedisKeyValueService redisService, IOptionsSnapshot<RedisKeys> redisKeysSnapshot, 
        CommunicationControlHttpClient hiveHttpClient, ILogger<InterferenceManagementService> logger, IOptionsMonitor<ComControlCommunicationConfiguration> communicationControlConfiguration, IHiveManagementService hiveManagementService, IPublishService messageBus)
    {
        _redisService = redisService;
        _redisKeys = redisKeysSnapshot.Value;
        _hiveHttpClient = hiveHttpClient;
        _logger = logger;
        _communicationControlConfiguration = communicationControlConfiguration;
        _hiveManagementService = hiveManagementService;
        _messageBus = messageBus;
    }
    public async Task<InterferenceModel> GetInterferenceModel(Guid interferenceId)
        {
            var result = await _redisService.GetAsync<InterferenceModel>(GetInterferenceKey(interferenceId));
            return result;
        }


        public async Task<List<InterferenceModel>> GetAllInterferences()
        {
            var result = await _redisService.GetAllAsync<InterferenceModel>($"{_redisKeys.InterferenceKey}:");
            return result;
        }

        #region Interference
        public async Task<Guid> SetInterference(InterferenceModel model)
        {
            bool result = await _redisService.SetAsync(GetInterferenceKey(model.Id), model);
            if (result)
            {
                _logger.LogInformation("Successfully added interference: {@model}", model);
                await _messageBus.Publish(new EwZoneAddedMessage
                {
                    Interference = model,
                    InterferenceId = model.Id
                });
            }
            else
            {
                _logger.LogError("Failed to connect add Interference: {@model}", model);
                throw new HiveConnectionException("Failed to add interference");
            }

            return model.Id;
        }

        public async Task<bool> DeleteInterference(Guid interferenceId)
        {
            var result = await _redisService.DeleteAsync(GetInterferenceKey(interferenceId));

            if (result)
            {
                _logger.LogInformation("Successfully deleted interference {interferenceId}", interferenceId);

                await _messageBus.Publish(new EwZoneDeletedMessage
                {
                    HiveID = null,
                    InterferenceId = interferenceId
                });
            }
            else
            {
                _logger.LogWarning("Interference {interferenceId} was not deleted", interferenceId);
            }

            return result;
        }

        public async Task NotifyHivesOnDeletedInterference(Guid interferenceId)
        {
            var hives = await _hiveManagementService.GetAllHives();
            var interference = await GetInterferenceModel(interferenceId);

            if (interference is not null)
            {
                _logger.LogError("Interference {interferenceId} was not deleted, notification is not feasible", interferenceId);
                return;
            }

            if (hives.Count == 0)
            {
                _logger.LogInformation("No hives to notify about deleted interference {interferenceId}", interferenceId);
                return;
            }

            var command = new DeleteInterferenceFromHiveMindCommand
            {
                CommandType = HiveMindState.DeleteInterference,
                InterferenceId = interferenceId
            };

            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            string[] hiveIds = hives.Select(h => h.HiveID).ToArray();
            _logger.LogInformation("Notifying {count} hives about deleted interference {interferenceId}: {hiveIds}", hives.Count, interferenceId, hiveIds);

            var notificationTasks = hives.Select(async hive =>
            {
                try
                {
                    var result = await _hiveHttpClient.SendHiveControlCommandAsync(
                        hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);

                    _logger.LogInformation(
                        "Successfully notified hive {hiveId} about deleted interference {interferenceId}",
                        hive.HiveID, interferenceId);

                    return (HiveId: hive.HiveID, Success: true, Error: (Exception)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to notify hive {hiveId} about deleted interference {interferenceId}",
                        hive.HiveID, interferenceId);

                    return (HiveId: hive.HiveID, Success: false, Error: ex);
                }
            });

            var results = await Task.WhenAll(notificationTasks);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            if (failureCount > 0)
            {
                var failedHives = string.Join(", ", results.Where(r => !r.Success).Select(r => r.HiveId));
                _logger.LogWarning(
                    "Deleted nterference notification complete for {interferenceId}: {success}/{total} succeeded. Failed hives: {failedHives}",
                    interferenceId, successCount, hives.Count, failedHives);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully notified all {total} hives about deleted interference {interferenceId}",
                    hives.Count, interferenceId);
            }
        }
        public async Task NotifyHivesAboutAddedInterference(Guid interferenceId)
        {
            var hives = await _hiveManagementService.GetAllHives();
            var interference = await GetInterferenceModel(interferenceId);

            if (interference == null)
            {
                _logger.LogError("Interference {interferenceId} not found for notification", interferenceId);
                return;
            }

            if (hives.Count == 0)
            {
                _logger.LogInformation("No hives to notify about interference {interferenceId}", interferenceId);
                return;
            }

            var command = new AddInterferenceToHiveMindCommand
            {
                CommandType = HiveMindState.SetInterference,
                Interference = interference,
            };

            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            string[] hiveIds = hives.Select(h => h.HiveID).ToArray();
            _logger.LogInformation("Notifying {count} hives about interference {interferenceId}: {hiveIds}", hives.Count, interferenceId, hiveIds);

            var notificationTasks = hives.Select(async hive =>
            {
                try
                {


                    var result = await _hiveHttpClient.SendHiveControlCommandAsync(
                        hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);
                    
                    _logger.LogInformation(
                        "Successfully notified hive {hiveId} about interference {interferenceId}", 
                        hive.HiveID, interferenceId);
                    
                    return (HiveId: hive.HiveID, Success: true, Error: (Exception)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to notify hive {hiveId} about interference {interferenceId}", 
                        hive.HiveID, interferenceId);
                    
                    return (HiveId: hive.HiveID, Success: false, Error: ex);
                }
            });

            var results = await Task.WhenAll(notificationTasks);

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);
            
            if (failureCount > 0)
            {
                var failedHives = string.Join(", ", results.Where(r => !r.Success).Select(r => r.HiveId));
                _logger.LogWarning(
                    "Interference notification complete for {interferenceId}: {success}/{total} succeeded. Failed hives: {failedHives}",
                    interferenceId, successCount, hives.Count, failedHives);
            }
            else
            {
                _logger.LogInformation(
                    "Successfully notified all {total} hives about interference {interferenceId}",
                    hives.Count, interferenceId);
            }
        }

        private string GetInterferenceKey(Guid interferenceId)
        {
            return $"{_redisKeys.InterferenceKey}:{interferenceId.ToString()}";
        }

        #endregion
}