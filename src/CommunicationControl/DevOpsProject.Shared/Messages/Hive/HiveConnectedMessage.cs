using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Messages
{
    public class HiveConnectedMessage : BaseMessage
    {
        public bool IsSuccessfullyConnected { get; set; }
        public HiveModel Hive { get; set; }
        public HiveOperationalArea InitialOperationalArea {  get; set; }
    }
}
