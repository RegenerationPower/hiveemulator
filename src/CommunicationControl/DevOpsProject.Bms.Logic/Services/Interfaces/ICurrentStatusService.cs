using DevOpsProject.Shared.Models;

namespace DevOpsProject.Bms.Logic.Services.Interfaces;

public interface ICurrentStatusService
{
    Task UpsertHiveStatusAsync(HiveTelemetryModel telemetry, bool isInEwZone, CancellationToken ct = default);
    Task<List<HiveStatus>> GetAllStatusesAsync(CancellationToken ct = default);
    Task<HiveStatus?> GetStatusAsync(string hiveId, CancellationToken ct = default);

}