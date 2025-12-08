using DevOpsProject.HiveMind.Logic.Domain.Drone.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Hive.Repositories;
using DevOpsProject.HiveMind.Logic.Domain.Topology.Services;
using DevOpsProject.HiveMind.Logic.Patterns.Command;
using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Patterns.Factory;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.HiveMind.Logic.Services;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.API.DI
{
    public static class LogicConfiguration
    {
        public static IServiceCollection AddHiveMindLogic(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ICommandHandler<MoveHiveMindCommand>, MoveHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<StopHiveMindCommand>, StopHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<AddInterferenceToHiveMindCommand>, AddInterferenceToHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandler<DeleteInterferenceFromHiveMindCommand>, DeleteInterferenceFromHiveMindCommandHandler>();
            serviceCollection.AddTransient<ICommandHandlerFactory, CommandHandlerFactory>();

            // Domain Repositories
            serviceCollection.AddScoped<IDroneRepository, DroneRepository>();
            serviceCollection.AddScoped<IHiveRepository, HiveRepository>();

            // Domain Services
            serviceCollection.AddScoped<ITopologyBuilder, TopologyBuilder>();
            serviceCollection.AddScoped<IConnectivityAnalyzer, ConnectivityAnalyzer>();
            serviceCollection.AddScoped<MeshRoutingService>();
            serviceCollection.AddScoped<IConnectionManager, ConnectionManager>();

            // Application Services
            serviceCollection.AddScoped<IHiveMindService, HiveMindService>();
            serviceCollection.AddScoped<IHiveMindMovingService, HiveMindMovingService>();
            serviceCollection.AddScoped<IDroneRelayService, DroneRelayService>();
            serviceCollection.AddScoped<IDroneCommandService, DroneCommandService>();
            serviceCollection.AddScoped<IHiveService, HiveService>();

            return serviceCollection;
        }
    }
}
