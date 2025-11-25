namespace DevOpsProject.Shared.Models.Commands.HiveMind
{
    public class AddInterferenceToHiveMindCommand : HiveMindCommand
    {
        public InterferenceModel Interference { get; init; }
    }
}
