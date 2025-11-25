using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Messages
{
    public class HiveDisconnectedMessage : BaseMessage
    {
        public bool IsSuccessfullyDisconnected { get; set; }
    }
}
