using DevOpsProject.Shared.Models;

namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces
{
    public interface ISpatialService
    {
        HiveOperationalArea GetHiveOperationalArea(HiveModel hiveModel);
    }
}
