using DevOpsProject.Shared.Configuration;


namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class OptionsConfiguration
    {
        public static IServiceCollection AddOptionsConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<OperationalAreaConfig>(configuration.GetSection("OperationalArea"));
            serviceCollection.Configure<ComControlCommunicationConfiguration>(configuration.GetSection("CommunicationConfiguration"));

            return serviceCollection;
        }
    }
}
