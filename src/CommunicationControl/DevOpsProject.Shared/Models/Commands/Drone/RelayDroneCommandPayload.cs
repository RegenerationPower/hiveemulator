namespace DevOpsProject.Shared.Models.Commands.Drone
{
    /// <summary>
    /// Payload for relay command - contains the final command and routing information
    /// </summary>
    public class RelayDroneCommandPayload
    {
        /// <summary>
        /// The final destination drone ID
        /// </summary>
        public string FinalDestinationDroneId { get; set; }

        /// <summary>
        /// The next hop drone ID in the route (null if this is the final hop)
        /// </summary>
        public string? NextHopDroneId { get; set; }

        /// <summary>
        /// The final command to be delivered to the target drone
        /// </summary>
        public DroneCommand FinalCommand { get; set; }

        /// <summary>
        /// The full route path from source to destination
        /// </summary>
        public IReadOnlyCollection<string> RoutePath { get; set; }
    }
}

