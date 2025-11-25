using DevOpsProject.Shared.Models;

namespace DevOpsProject.CommunicationControl.Logic.Services.Interference;

public interface IInterferenceManagementService
{
    Task<InterferenceModel> GetInterferenceModel(Guid interferenceId);
    Task<List<InterferenceModel>> GetAllInterferences();

    Task<Guid> SetInterference(InterferenceModel model);

    Task<bool> DeleteInterference(Guid interferenceId);

    Task NotifyHivesOnDeletedInterference(Guid interferenceId);

    Task NotifyHivesAboutAddedInterference(Guid interferenceId);
}