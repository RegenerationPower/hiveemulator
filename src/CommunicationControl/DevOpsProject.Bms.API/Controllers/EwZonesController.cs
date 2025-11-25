using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.DTO.Bms;
using DevOpsProject.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsProject.Bms.API.Controllers;

[ApiController]
[Route("api/v1/ew-zones")]
public class EwZonesController : ControllerBase
{
    private readonly IEwZoneService _zones;

    public EwZonesController(IEwZoneService zones)
    {
        _zones = zones;
    }

    [HttpGet("active")]
    public async Task<IEnumerable<EwZoneDto>> GetActive(CancellationToken ct)
    {
        var entities = await _zones.GetActiveZonesAsync(ct);

        return entities.Select(z => new EwZoneDto
        {
            Id = z.Id,
            CenterLatitude  = z.CenterLatitude,
            CenterLongitude = z.CenterLongitude,
            RadiusKm        = z.RadiusKm,
            IsActive        = z.IsActive,
            ActiveFromUtc   = z.ActiveFromUtc,
            ActiveToUtc     = z.ActiveToUtc
        });
    }

    [HttpGet("history")]
    public async Task<IEnumerable<EwZoneHistory>> GetHistory(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct)
    {
        return await _zones.GetZonesHistoryAsync(fromUtc, toUtc, ct);
    }
}