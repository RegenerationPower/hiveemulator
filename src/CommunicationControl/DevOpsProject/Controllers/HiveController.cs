using Asp.Versioning;
using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.CommunicationControl.Logic.Services.Interference;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.DTO.hive;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsProject.CommunicationControl.API.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/hive")]
    public class HiveController : Controller
    {
        private readonly IHiveManagementService _hiveManagementService;
        private readonly ITelemetryService _telemetryService;
        private readonly IInterferenceManagementService _interferenceManagementService;
        private readonly ILogger<HiveController> _logger;

        public HiveController(ILogger<HiveController> logger, IHiveManagementService hiveManagementService, 
            ITelemetryService telemetryService, IInterferenceManagementService interferenceManagementService)
        {
            _logger = logger;
            _hiveManagementService = hiveManagementService;
            _telemetryService = telemetryService;
            _interferenceManagementService = interferenceManagementService;
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect(HiveConnectRequest request)
        {
            var hiveModel = new HiveModel
            {
                HiveID = request.HiveID,
                HiveIP = request.HiveIP,
                HivePort = request.HivePort,
                HiveSchema = request.HiveSchema
            };

            var hiveOperationalArea = await  _hiveManagementService.ConnectHive(hiveModel);
            var interferences = await _interferenceManagementService.GetAllInterferences();

            var connectResponse = new HiveConnectResponse
            {
                ConnectResult = true,
                OperationalArea = hiveOperationalArea,
                Interferences = interferences
            };

            return Ok(connectResponse);
        }

        [HttpPost("telemetry")]
        public async Task<IActionResult> Telemetry(HiveTelemetryRequest request)
        {
            var hiveTelemetryModel = new HiveTelemetryModel
            {
                HiveID = request.HiveID,
                Location = request.Location,
                Speed = request.Speed,
                Height = request.Height,
                State = request.State,
                Timestamp = DateTime.UtcNow
            };

            bool isHiveConnected = await  _hiveManagementService.IsHiveConnected(request.HiveID);
            if (isHiveConnected)
            {
                var telemetryUpdateTimestamp = await _telemetryService.AddTelemetry(hiveTelemetryModel);
                var telemetryResponse = new HiveTelemetryResponse
                {
                    Timestamp = telemetryUpdateTimestamp
                };

                return Ok(telemetryResponse);
            }
            else
            {
                _logger.LogWarning("Failed to write telemetry. Hive with HiveID: {hiveId} is not connected. Request: {@request}", request.HiveID, request);
                return NotFound($"Failed to write telemetry. Hive with HiveID: {request.HiveID} is not connected");
            }
        }

    }
}
