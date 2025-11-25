using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Options;

namespace DevOpsProject.CommunicationControl.Logic.Services
{
    public class SpatialService : ISpatialService
    {
        private readonly IOptionsMonitor<OperationalAreaConfig> _operationalAreaConfig;

        public SpatialService(IOptionsMonitor<OperationalAreaConfig> operationalAreaConfig)
        {
            _operationalAreaConfig = operationalAreaConfig;
        }

        public HiveOperationalArea GetHiveOperationalArea(HiveModel hiveModel)
        {
            var operationalArea = new HiveOperationalArea
            {
                RadiusKM = _operationalAreaConfig.CurrentValue.Radius_KM,
                InitialLocation = new Location
                {
                    Latitude = _operationalAreaConfig.CurrentValue.Latitude,
                    Longitude = _operationalAreaConfig.CurrentValue.Longitude
                },
                InitialHeight = _operationalAreaConfig.CurrentValue.InitialHeight_KM,
                Speed = _operationalAreaConfig.CurrentValue.InitialSpeed_KM,
                TelemetryIntervalMs = _operationalAreaConfig.CurrentValue.TelemetryInterval_MS,
                PingIntervalMs = _operationalAreaConfig.CurrentValue.PingInterval_MS
            };

            return operationalArea;
        }
    }
}
