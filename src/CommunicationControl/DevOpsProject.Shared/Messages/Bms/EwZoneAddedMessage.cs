using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Messages;

public class EwZoneAddedMessage: BaseMessage
{
    public Guid InterferenceId { get; set; }
    public string MessageType { get; set; } = nameof(EwZoneAddedMessage);
    public InterferenceModel Interference { get; set; }
}