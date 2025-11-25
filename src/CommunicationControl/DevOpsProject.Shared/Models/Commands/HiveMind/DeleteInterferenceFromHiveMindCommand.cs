namespace DevOpsProject.Shared.Models.Commands.HiveMind
{
    public class DeleteInterferenceFromHiveMindCommand : HiveMindCommand
    {
        public Guid InterferenceId { get; init; }
    }
}
