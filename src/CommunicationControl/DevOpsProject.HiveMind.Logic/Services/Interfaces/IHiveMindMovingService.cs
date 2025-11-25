using DevOpsProject.Shared.Models;

namespace DevOpsProject.HiveMind.Logic.Services.Interfaces
{
    public interface IHiveMindMovingService
    {
        void MoveToLocation(Location destination);
        void StopHiveMindMoving(bool immediateStop);
    }
}
