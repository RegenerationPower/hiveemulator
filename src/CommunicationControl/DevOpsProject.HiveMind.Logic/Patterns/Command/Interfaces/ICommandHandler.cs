using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces
{
    public interface ICommandHandler
    {
        Task HandleAsync(HiveMindCommand command);
    }

    public interface ICommandHandler<T> : ICommandHandler where T : HiveMindCommand
    {
        Task HandleAsync(T command);

        async Task ICommandHandler.HandleAsync(HiveMindCommand command)
        {
            await HandleAsync((T)command);
        }
    }
}
