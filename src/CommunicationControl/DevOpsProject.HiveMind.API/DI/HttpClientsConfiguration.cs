using DevOpsProject.Shared.Clients;
using Polly.Extensions.Http;
using Polly;

namespace DevOpsProject.HiveMind.API.DI
{
    public static class HttpClientsConfiguration
    {
        public static IServiceCollection AddHttpClientsConfiguration(this IServiceCollection serviceCollection)
        {
            var communicationControlTelemetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            serviceCollection.AddHttpClient<HiveMindHttpClient>()
                .AddPolicyHandler(communicationControlTelemetryPolicy);
            serviceCollection.AddHttpClient("HiveConnectClient");

            return serviceCollection;
        }
    }
}
