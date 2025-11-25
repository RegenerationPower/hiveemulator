namespace DevOpsProject.Shared.Messages
{
    public class StopHiveMessage : BaseMessage
    {
        public bool IsImmediateStop { get; set; }
        public DateTime StopTimestamp { get; set; }
    }
}
