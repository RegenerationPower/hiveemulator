#nullable enable
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.Drone;
using DevOpsProject.Shared.Models.DTO.Hive;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IDroneCommandService
    {
        DroneJoinResponse JoinDrone(string hiveId, DroneJoinRequest request);
        BatchJoinDronesResponse BatchJoinDrones(string hiveId, BatchJoinDronesRequest request);
        IReadOnlyCollection<Drone> GetConnectedDrones(string hiveId, string droneId);
        IReadOnlyCollection<Drone> GetHiveDrones(string hiveId);
        bool RemoveDroneFromHive(string hiveId, string droneId);
        BatchRemoveDronesResponse BatchRemoveDrones(string hiveId, BatchRemoveDronesRequest request);
        DroneCommand? GetNextCommand(string droneId);
        IReadOnlyCollection<DroneCommand> GetAllCommands(string droneId);
        void SendCommand(DroneCommand command);
        int SendCommandToHive(string hiveId, DroneCommand command);
    }
}

