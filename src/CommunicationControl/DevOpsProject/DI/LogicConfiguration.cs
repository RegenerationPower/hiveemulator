using DevOpsProject.CommunicationControl.Logic.Services;
using DevOpsProject.CommunicationControl.Logic.Services.Interfaces;

namespace DevOpsProject.CommunicationControl.API.DI
{
    public static class LogicConfiguration
    {
        public static IServiceCollection AddCommunicationControlLogic(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ICommunicationControlService, CommunicationControlService>();
            serviceCollection.AddTransient<ISpatialService, SpatialService>();

            return serviceCollection;
        }
    }
}
