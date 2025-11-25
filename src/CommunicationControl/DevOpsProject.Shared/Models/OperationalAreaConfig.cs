namespace DevOpsProject.Shared.Models
{
    public class OperationalAreaConfig
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float Radius_KM { get; set; }
        public float InitialHeight_KM { get; set; }
        public float InitialSpeed_KM { get; set; }
        public int TelemetryInterval_MS { get; set; } 
        public int PingInterval_MS { get; set; }
    }
}
