using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Messages
{
    public class MoveHiveMessage : BaseMessage
    {
        public bool IsSuccessfullySent { get; set; }
        public Location Destination { get;set; }
    }
}
