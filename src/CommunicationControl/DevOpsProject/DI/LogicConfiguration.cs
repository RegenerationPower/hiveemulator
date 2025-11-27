using DevOpsProject.CommunicationControl.Logic.Services;
using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;
using DevOpsProject.CommunicationControl.Logic.Services.Interference;
using DevOpsProject.CommunicationControl.Logic.Services.telemetry;

namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class LogicConfiguration
    {
        public static IServiceCollection AddCommunicationControlLogic(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISpatialService, SpatialService>();
            serviceCollection.AddScoped<IHiveManagementService, HiveManagementService>();
            serviceCollection.AddScoped<IHiveCommandService, HiveCommandService>();
            serviceCollection.AddScoped<ITelemetryService, TelemetryService>();
            serviceCollection.AddScoped<IInterferenceManagementService, InterferenceManagementService>();
            serviceCollection.AddScoped<IHiveMindMeshIntegrationService, HiveMindMeshIntegrationService>();
            return serviceCollection;
        }
    }
}
