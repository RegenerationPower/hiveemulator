using Asp.Versioning;
using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.DTO.hive;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsProject.CommunicationControl.API.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/hivemind")]
    public class HiveMindMeshIntegrationController : ControllerBase
    {
        private readonly IHiveMindMeshIntegrationService _meshIntegrationService;
        private readonly ILogger<HiveMindMeshIntegrationController> _logger;

        public HiveMindMeshIntegrationController(
            IHiveMindMeshIntegrationService meshIntegrationService,
            ILogger<HiveMindMeshIntegrationController> logger)
        {
            _meshIntegrationService = meshIntegrationService;
            _logger = logger;
        }

        [HttpPost("hives")]
        public async Task<IActionResult> CreateHive([FromBody] HiveCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.HiveId))
            {
                return BadRequest(new { message = "HiveId is required" });
            }

            var response = await _meshIntegrationService.CreateHiveAsync(request);
            return response == null
                ? StatusCode(502, new { message = "HiveMind did not return a response for hive creation." })
                : Ok(response);
        }

        [HttpPost("drones/batch")]
        public async Task<IActionResult> BatchCreateDrones([FromBody] BatchCreateDronesRequest request)
        {
            var response = await _meshIntegrationService.BatchCreateDronesAsync(request);
            if (response == null)
            {
                return StatusCode(502, new { message = "HiveMind did not return a response for batch drone creation." });
            }

            return Ok(response);
        }

        [HttpPost("hives/{hiveId}/drones/batch-join")]
        public async Task<IActionResult> BatchJoinDrones(string hiveId, [FromBody] BatchJoinDronesRequest request)
        {
            var response = await _meshIntegrationService.BatchJoinDronesAsync(hiveId, request);
            if (response == null)
            {
                return StatusCode(502, new { message = $"HiveMind did not return a response for batch join in hive {hiveId}." });
            }

            return Ok(response);
        }

        [HttpPost("hives/{hiveId}/topology/rebuild")]
        public async Task<IActionResult> RebuildTopology(string hiveId, [FromBody] TopologyRebuildRequest request)
        {
            var response = await _meshIntegrationService.RebuildTopologyAsync(hiveId, request);
            if (response == null)
            {
                return StatusCode(502, new { message = $"HiveMind did not rebuild topology for hive {hiveId}." });
            }

            return Ok(response);
        }

        [HttpPost("hives/{hiveId}/topology/connect-hivemind")]
        public async Task<IActionResult> ConnectHiveMind(string hiveId, [FromBody] ConnectToHiveMindRequest request)
        {
            var response = await _meshIntegrationService.ConnectHiveMindAsync(hiveId, request);
            if (response == null)
            {
                return StatusCode(502, new { message = $"HiveMind did not connect hive {hiveId} to relay topology." });
            }

            return Ok(response);
        }

        [HttpGet("hives/{hiveId}/topology/connectivity")]
        public async Task<IActionResult> GetConnectivity(string hiveId)
        {
            var response = await _meshIntegrationService.GetConnectivityAsync(hiveId);
            if (response == null)
            {
                return StatusCode(502, new { message = $"HiveMind connectivity response for hive {hiveId} is unavailable." });
            }

            return Ok(response);
        }

        [HttpPost("drones/connections/degrade")]
        public async Task<IActionResult> DegradeConnection([FromBody] DegradeConnectionRequest request)
        {
            var response = await _meshIntegrationService.DegradeConnectionAsync(request);
            if (response == null)
            {
                return StatusCode(502, new { message = "HiveMind failed to degrade the connection." });
            }

            return Ok(response);
        }

        [HttpPost("drones/connections/batch-degrade")]
        public async Task<IActionResult> BatchDegradeConnections([FromBody] BatchDegradeConnectionsRequest request)
        {
            var response = await _meshIntegrationService.BatchDegradeConnectionsAsync(request);
            if (response == null)
            {
                return StatusCode(502, new { message = "HiveMind failed to process batch connection degradation." });
            }

            return Ok(response);
        }
    }
}

