using DevOpsProject.Shared.Models;

namespace DevOpsProject.Shared.Messages
{
    public class TelemetrySentMessage : BaseMessage
    {
        public string MessageType { get; set; } = nameof(TelemetrySentMessage);

        public bool IsSuccessfullySent { get; set; }

        public HiveTelemetryModel Telemetry { get; set; } = default!;

    }
}
