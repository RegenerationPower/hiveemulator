using DevOpsProject.Shared.Configuration;

namespace DevOpsProject.HiveMind.API.DI
{
    public static class OptionsConfiguration
    {
        public static IServiceCollection AddOptionsConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<HiveCommunicationConfig>(configuration.GetSection("CommunicationConfiguration"));

            return serviceCollection;
        }
    }
}
