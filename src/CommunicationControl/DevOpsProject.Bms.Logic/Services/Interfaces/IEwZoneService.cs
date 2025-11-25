using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Entities;

namespace DevOpsProject.Bms.Logic.Services.Interfaces;

public interface IEwZoneService
{
    Task UpsertZoneAsync(InterferenceModel model, CancellationToken ct = default);
    Task DeactivateZoneAsync(Guid interferenceId, CancellationToken ct = default);

    Task<List<EwZone>> GetActiveZonesAsync(CancellationToken ct = default);
    Task<List<EwZoneHistory>> GetZonesHistoryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}