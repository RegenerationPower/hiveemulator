using DevOpsProject.Shared.Models;

namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces;

public interface IHiveCommandService
{
    Task<string> SendHiveControlSignal(string hiveId, Location destination);
    Task<string> SendHiveStopSignal(string hiveId);
}