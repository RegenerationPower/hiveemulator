using DevOpsProject.HiveMind.Logic.Patterns.Command;
using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Patterns.Factory;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.HiveMind.Logic.Services;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.Commands.HiveMind;

namespace DevOpsProject.HiveMind.API.DI
{
    public static class LogicConfiguration
    {
        public static IServiceCollection AddHiveMindLogic(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ICommandHandler<MoveHiveMindCommand>, MoveHiveMindCommandHandler>();
            serviceCollection.AddScoped<ICommandHandler<StopHiveMindCommand>, StopHiveMindCommandHandler>();
            serviceCollection.AddScoped<ICommandHandler<AddInterferenceToHiveMindCommand>, AddInterferenceToHiveMindCommandHandler>();
            serviceCollection.AddScoped<ICommandHandler<DeleteInterferenceFromHiveMindCommand>, DeleteInterferenceFromHiveMindCommandHandler>();
            serviceCollection.AddScoped<ICommandHandlerFactory, CommandHandlerFactory>();

            serviceCollection.AddScoped<IHiveMindService, HiveMindService>();
            serviceCollection.AddScoped<IHiveMindMovingService, HiveMindMovingService>();
            serviceCollection.AddScoped<IDroneRelayService, DroneRelayService>();

            return serviceCollection;
        }
    }
}
