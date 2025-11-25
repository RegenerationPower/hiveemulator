using DevOpsProject.HiveMind.Logic.Patterns.Command.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models.HiveMindCommands;

namespace DevOpsProject.HiveMind.Logic.Patterns.Command
{
    public class StopHiveMindCommandHandler : ICommandHandler<StopHiveMindCommand>
    {
        private readonly IHiveMindMovingService _hiveMindMovingService;

        public StopHiveMindCommandHandler(IHiveMindMovingService hiveMindMovingService)
        {
            _hiveMindMovingService = hiveMindMovingService;
        }

        public async Task HandleAsync(StopHiveMindCommand command)
        {

            _hiveMindMovingService.StopHiveMindMoving(command.StopImmediately);
            await Task.CompletedTask;
        }
    }
}
