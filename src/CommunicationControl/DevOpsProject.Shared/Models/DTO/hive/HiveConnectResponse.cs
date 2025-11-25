namespace DevOpsProject.Shared.Models.DTO.hive
{
    public class HiveConnectResponse
    {
        public bool ConnectResult { get; set; }
        public HiveOperationalArea OperationalArea { get; set; }
        public List<InterferenceModel> Interferences { get; set; }
    }
}
