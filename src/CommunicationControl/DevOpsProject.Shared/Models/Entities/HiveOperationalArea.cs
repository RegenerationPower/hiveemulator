namespace DevOpsProject.Shared.Models
{
    public class HiveOperationalArea
    {
        public double RadiusKM { get; set; }
        public Location InitialLocation { get; set; }
        public float InitialHeight { get; set; }
        public float Speed { get; set; }
        public int TelemetryIntervalMs { get; set; }
        public int PingIntervalMs { get; set; }
    }
}
