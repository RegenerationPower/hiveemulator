namespace DevOpsProject.Shared.Models
{
    public class HiveModel
    {
        public string HiveID { get; set; }
        public string HiveIP { get; set; }
        public int HivePort { get; set; }
        public string HiveSchema { get; set; }
        public HiveTelemetryModel Telemetry { get; set; }
    }
}
