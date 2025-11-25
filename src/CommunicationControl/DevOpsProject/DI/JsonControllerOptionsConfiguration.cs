namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class JsonControllerOptionsConfiguration
    {
        public static IServiceCollection AddJsonControllerOptionsConfiguration(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            return serviceCollection;
        }
    }
}
