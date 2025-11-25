using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Clients;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Messages;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.HiveMind;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.Logic.Services;

public class HiveCommandService: IHiveCommandService
{

    private readonly IPublishService _messageBus;
    private readonly CommunicationControlHttpClient _hiveHttpClient;
    private readonly ILogger<HiveCommandService> _logger;
    private readonly IOptionsMonitor<ComControlCommunicationConfiguration> _communicationControlConfiguration;
    private readonly IHiveManagementService _hiveManagementService;

    public HiveCommandService(IPublishService messageBus, CommunicationControlHttpClient hiveHttpClient, 
        ILogger<HiveCommandService> logger, IOptionsMonitor<ComControlCommunicationConfiguration> communicationControlConfiguration, IHiveManagementService hiveManagementService)
    {
        _messageBus = messageBus;
        _hiveHttpClient = hiveHttpClient;
        _logger = logger;
        _communicationControlConfiguration = communicationControlConfiguration;
        _hiveManagementService = hiveManagementService;
    }
    
    public async Task<string> SendHiveStopSignal(string hiveId)
        {
            var hive = await _hiveManagementService.GetHiveModel(hiveId);
            if (hive == null)
            {
                _logger.LogError("Sending Hive Stop signal: Hive not found for HiveID: {hiveId}", hiveId);
                return null;
            }

            bool isSuccessfullySent = false;
            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            var command = new StopHiveMindCommand
            {
                CommandType = HiveMindState.Stop,
                StopImmediately = true,
                Timestamp = DateTime.Now
            };
            try
            {
                var result = await _hiveHttpClient.SendHiveControlCommandAsync(hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);
                isSuccessfullySent = true;
                return result;
            }
            finally
            {
                if (isSuccessfullySent)
                {
                    await _messageBus.Publish(new StopHiveMessage
                    {
                        IsImmediateStop = true,
                        HiveID = hiveId
                    });
                }
                else
                {
                    _logger.LogError("Failed to send stop command for Hive: {@hive}, path: {path}, \n Command: {@command}", hive, hiveMindPath, command);
                }

            }
        }

        public async Task<string> SendHiveControlSignal(string hiveId, Location destination)
        {
            var hive = await _hiveManagementService.GetHiveModel(hiveId);
            if (hive == null)
            {
                _logger.LogError("Sending Hive Control signal: Hive not found for HiveID: {hiveId}", hiveId);
                return null;
            }

            bool isSuccessfullySent = false;
            string hiveMindPath = _communicationControlConfiguration.CurrentValue.HiveMindPath;
            var command = new MoveHiveMindCommand
            {
                CommandType = HiveMindState.Move,
                Destination = destination,
                Timestamp = DateTime.Now
            };
            try
            {
                var result = await _hiveHttpClient.SendHiveControlCommandAsync(hive.HiveSchema, hive.HiveIP, hive.HivePort, hiveMindPath, command);
                isSuccessfullySent = true;
                return result;
            }
            finally
            {
                if (isSuccessfullySent)
                {
                    await _messageBus.Publish(new MoveHiveMessage
                    {
                        IsSuccessfullySent = isSuccessfullySent,
                        Destination = destination,
                        HiveID = hiveId
                    });
                }
                else
                {
                    _logger.LogError("Failed to send control command for Hive: {@hive}, path: {path}, \n Command: {@command}", hive, hiveMindPath, command);
                }
                
            }
        }
}