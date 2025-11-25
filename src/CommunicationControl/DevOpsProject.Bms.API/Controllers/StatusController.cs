using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.DTO.Bms;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsProject.Bms.API.Controllers;

[ApiController]
[Route("api/v1/status")]
public class StatusController : ControllerBase
{
    private readonly ICurrentStatusService _statusService;

    public StatusController(ICurrentStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public async Task<IEnumerable<HiveStatusDto>> GetAll(CancellationToken ct)
    {
        var entities = await _statusService.GetAllStatusesAsync(ct);

        return entities.Select(e => new HiveStatusDto
        {
            HiveId = e.HiveId,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            Height = e.Height,
            Speed = e.Speed,
            State = e.State,
            IsInEwZone = e.IsInEwZone,
            LastTelemetryTimestampUtc = e.LastTelemetryTimestampUtc
        });
    }

    [HttpGet("{hiveId}")]
    public async Task<ActionResult<HiveStatusDto>> GetOne(string hiveId, CancellationToken ct)
    {
        var e = await _statusService.GetStatusAsync(hiveId, ct);
        if (e == null) return NotFound();

        return new HiveStatusDto
        {
            HiveId = e.HiveId,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            Height = e.Height,
            Speed = e.Speed,
            State = e.State,
            IsInEwZone = e.IsInEwZone,
            LastTelemetryTimestampUtc = e.LastTelemetryTimestampUtc
        };
    }
}