namespace DevOpsProject.Shared.Configuration
{
    public class ComControlCommunicationConfiguration
    {
        public string HiveMindSchema { get; set; } = "http";
        public string HiveMindHost { get; set; } = "localhost";
        public int HiveMindPort { get; set; } = 5149;
        public string HiveMindPath { get; set; } = "api/v1";
    }
}
