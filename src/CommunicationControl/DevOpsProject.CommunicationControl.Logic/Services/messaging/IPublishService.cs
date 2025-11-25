namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces
{
    public interface IPublishService
    {
        Task Publish<T>(T message);
    }
}
