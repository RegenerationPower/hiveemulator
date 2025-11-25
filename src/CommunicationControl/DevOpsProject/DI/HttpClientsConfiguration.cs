using DevOpsProject.Shared.Clients;
using Polly;
using Polly.Extensions.Http;

namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class HttpClientsConfiguration
    {
        public static IServiceCollection AddHttpClientsConfiguration(this IServiceCollection serviceCollection)
        {
            var hiveRetryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            serviceCollection.AddHttpClient<CommunicationControlHttpClient>()
                .AddPolicyHandler(hiveRetryPolicy);

            return serviceCollection;
        }
    }
}
