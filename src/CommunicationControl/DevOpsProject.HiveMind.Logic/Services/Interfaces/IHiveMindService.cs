using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IHiveMindService
    {
        Task ConnectHive(string? overrideHiveId = null);
        bool AddInterference(InterferenceModel interferenceModel);
        void RemoveInterference(Guid interferenceId);
        HiveTelemetryModel GetCurrentTelemetry();
        HiveTelemetryModel? GetTelemetry(string hiveId);
        Task<bool> UpdateHiveIdentityAsync(string hiveId, bool reconnect);
        bool UpdateTelemetry(string hiveId, Location? location, float? height, float? speed, bool? isMoving);
    }
}