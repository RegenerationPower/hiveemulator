namespace DevOpsProject.Shared.Messages;

public class EwZoneDeletedMessage: BaseMessage
{
    public string MessageType { get; set; } = nameof(EwZoneDeletedMessage);
    public Guid InterferenceId { get; set; }
}