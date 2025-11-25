namespace DevOpsProject.HiveMind.API.DI
{
    public static class CorsConfiguration
    {
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection serviceCollection, string corsPolicyName)
        {
            serviceCollection.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicyName,
                    policy =>
                    {
                        policy.AllowAnyOrigin() //SECURITY WARNING ! Never allow all origins
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            return serviceCollection;
        }
    }
}
