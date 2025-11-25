using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.Shared.Models.Commands.HiveMind;

namespace DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces
{
    public interface ICommandHandlerFactory
    {
        ICommandHandler GetHandler(HiveMindCommand command);
    }
}
