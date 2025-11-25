using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command
{
    public class MoveHiveMindCommandHandler : ICommandHandler<MoveHiveMindCommand>
    {
        private readonly IHiveMindMovingService _hiveMindMovingService;

        public MoveHiveMindCommandHandler(IHiveMindMovingService hiveMindMovingService)
        {
            _hiveMindMovingService = hiveMindMovingService;
        }

        public async Task HandleAsync(MoveHiveMindCommand command)
        {

            _hiveMindMovingService.MoveToLocation(command.Destination);
            await Task.CompletedTask;
        }
    }
}
