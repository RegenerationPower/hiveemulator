using DevOpsProject.Shared.Models;

namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces;

public interface ITelemetryService
{
    Task<DateTime> AddTelemetry(HiveTelemetryModel model);
}