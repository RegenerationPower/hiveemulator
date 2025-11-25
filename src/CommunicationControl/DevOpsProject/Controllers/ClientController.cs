using Asp.Versioning;
using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.CommunicationControl.Logic.Services.Interference;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.DTO.hive;
using DevOpsProject.Shared.Models.DTO.Interference;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.API.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/client")]
    public class ClientController : Controller
    {
        private readonly IOptionsMonitor<OperationalAreaConfig> _operationalAreaConfig;
        private readonly ILogger<ClientController> _logger;
        private readonly IHiveManagementService _hiveManagementService;
        private readonly IInterferenceManagementService _interferenceManagementService;
        private readonly IHiveCommandService _hiveCommandService;

        public ClientController(IOptionsMonitor<OperationalAreaConfig> operationalAreaConfig, 
            ILogger<ClientController> logger, IHiveCommandService hiveCommandService, 
            IInterferenceManagementService interferenceManagementService, IHiveManagementService hiveManagementService)
        {
            _operationalAreaConfig = operationalAreaConfig;
            _logger = logger;
            _hiveCommandService = hiveCommandService;
            _interferenceManagementService = interferenceManagementService;
            _hiveManagementService = hiveManagementService;
        }

        [HttpGet("area")]
        public IActionResult GetOperationalArea()
        {
            return Ok(_operationalAreaConfig.CurrentValue);
        }

        [HttpGet("hive/{hiveId}")]
        public async Task<IActionResult> GetHive(string hiveId)
        {
            var hiveExists = await _hiveManagementService.IsHiveConnected(hiveId);
            if (!hiveExists)
            {
                _logger.LogWarning("Failed to get Hive for HiveID: {hiveId}", hiveId);
                return NotFound($"Hive with HiveID: {hiveId} is not found");
            }

            var hiveModel = await _hiveManagementService.GetHiveModel(hiveId);
            return Ok(hiveModel);
        }

        [HttpGet("hive")]
        public async Task<IActionResult> GetHives()
        {
            var hives = await _hiveManagementService.GetAllHives();
            return Ok(hives);
        }

        [HttpGet("interferences")]
        public async Task<IActionResult> GetInterferences()
        {
            var interferences = await _interferenceManagementService.GetAllInterferences();
            return Ok(interferences);
        }

        [HttpDelete("hive/{hiveId}")]
        public async Task<IActionResult> DisconnectHive(string hiveId)
        {
            var disconnetResult = await _hiveManagementService.DisconnectHive(hiveId);
            return Ok(disconnetResult);
        }

        [HttpPatch("hive")]
        public IActionResult SendBulkHiveMovingSignal(MoveHivesRequest request)
        {
            if (request?.Hives == null || !request.Hives.Any())
                return BadRequest("No hive IDs provided.");

            _logger.LogInformation("Hive moving request accepted by enpdoint. Request: {@request}", request);
            foreach (var id in request.Hives)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hiveCommandService.SendHiveControlSignal(id, request.Destination);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send control signal for HiveID: {id} \n Request: {@request}", id, request);
                    }
                });
            }


            return Accepted("Hives are being moved asynchronously.");
        }

        [HttpDelete("interference/{id:guid}")]
        public async Task<IActionResult> DeleteInterference([FromRoute] DeleteInterferenceRequest request)
        {
            _logger.LogInformation("DeleteInterference request: {request}", request);

            var interferenceDeleted = await _interferenceManagementService.DeleteInterference(request.Id);
            if (!interferenceDeleted)
            {
                _logger.LogError("Interference with Id: {id} was not deleted", request.Id);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _interferenceManagementService.NotifyHivesOnDeletedInterference(request.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background notification failed for deleted interference {interferenceId}", request.Id);
                }
            });

            return NoContent();
        } 

        [HttpPost("interference")]
        public async Task<IActionResult> AddInterferenceArea(AddInterferenceRequest request)
        {
            _logger.LogInformation("AddInterferenceArea request: {request}", request);

            if (request == null)
                return BadRequest("Request body is required");

            if (request.RadiusKM <= 0)
                return BadRequest("Radius must be greater than 0");

            var addedInterferenceId = await _interferenceManagementService.SetInterference(new InterferenceModel
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Location = request.Location,
                RadiusKM = request.RadiusKM
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await _interferenceManagementService.NotifyHivesAboutAddedInterference(addedInterferenceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background notification failed for interference {interferenceId}", addedInterferenceId);
                }
            });

            return Ok(new { InterferenceId = addedInterferenceId, Message = "Interference added successfully. Hives are being notified." });
        }

        [HttpPost("hive/stop")]
        public IActionResult SendBulkHiveStopSignal(StopHivesMovementRequest request)
        {
            if (request?.Hives == null || !request.Hives.Any())
                return BadRequest("No hive IDs provided.");

            _logger.LogInformation("Hive stop moving request accepted by enpdoint. Request: {@request}", request);
            foreach (var id in request.Hives)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _hiveCommandService.SendHiveStopSignal(id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send control stop signal for HiveID: {id} \n Request: {@request}", id, request);
                    }
                });
            }


            return Accepted("Hives movement is being stopped asynchronously.");
        }
    }
}
