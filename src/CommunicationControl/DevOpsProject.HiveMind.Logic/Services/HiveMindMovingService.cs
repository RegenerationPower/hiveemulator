using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.HiveMind.Logic.Services
{
    public class HiveMindMovingService : IHiveMindMovingService
    {
        private readonly ILogger<HiveMindMovingService> _logger;
        private Timer _movementTimer;
        public HiveMindMovingService(ILogger<HiveMindMovingService> logger)
        {
            _logger = logger;
        }

        public void MoveToLocation(Location destination)
        {
            lock (typeof(HiveInMemoryState))
            {
                if (HiveInMemoryState.OperationalArea == null || HiveInMemoryState.CurrentLocation == null)
                {
                    _logger.LogWarning("Cannot start moving: OperationalArea or CurrentLocation is not set.");
                    return;
                }

                // If already moving - stop movement
                if (HiveInMemoryState.IsMoving)
                {
                    _logger.LogWarning("Previous movement command terminated. Previous destination: {@destination}, Current Location: {@current}, new destination: {@destination}", HiveInMemoryState.Destination, HiveInMemoryState.CurrentLocation, destination);
                    StopMovement();
                }

                HiveInMemoryState.Destination = destination;
                HiveInMemoryState.IsMoving = true;

                _logger.LogInformation($"Received move command: Moving towards {destination}");

                // Start the movement timer if not already running
                if (_movementTimer == null)
                {
                    // TODO: Recalculating position each N seconds
                    int intervalFromSeconds = 3;
                    _movementTimer = new Timer(UpdateMovement, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalFromSeconds));
                    _logger.LogInformation("Movement timer started. Destination: {@destination}, recalculation interval: {interval}", destination, intervalFromSeconds);
                }
            }
        }

        public void StopHiveMindMoving(bool immediateStop)
        {
            lock (typeof(HiveInMemoryState))
            {
                if (HiveInMemoryState.OperationalArea == null || HiveInMemoryState.CurrentLocation == null)
                {
                    _logger.LogWarning("Cannot stop moving: OperationalArea or CurrentLocation is not set.");
                    return;
                }

                if (!HiveInMemoryState.IsMoving)
                {
                    _logger.LogWarning("Hive mind already stopped moving. Previous destination: {@destination}, Current Location: {@current}", HiveInMemoryState.Destination, HiveInMemoryState.CurrentLocation);
                    return;
                }

                _logger.LogInformation("Hive mind stopped moving. Current location: {@currentLocation}, Destination: {@destination}", HiveInMemoryState.CurrentLocation, 
                    HiveInMemoryState.Destination);
                StopMovement();
            }
        }

        private void UpdateMovement(object state)
        {
            lock (typeof(HiveInMemoryState))
            {
                var currentLocation = HiveInMemoryState.CurrentLocation;
                var destination = HiveInMemoryState.Destination;

                if (currentLocation == null || destination == null)
                {
                    StopMovement();
                    return;
                }

                if (AreLocationsEqual(currentLocation.Value, destination.Value))
                {
                    _logger.LogInformation("Reached destination. Current location: {@currentLocation}, Destination: {@destination}", currentLocation, destination);
                    StopMovement();
                    return;
                }

                Location newLocation = CalculateNextPosition(currentLocation.Value, destination.Value, 0.1f);
                HiveInMemoryState.CurrentLocation = newLocation;
            }
        }

        private void StopMovement()
        {
            _movementTimer?.Dispose();
            _movementTimer = null;
            HiveInMemoryState.IsMoving = false;
            HiveInMemoryState.Destination = null;
        }

        private static bool AreLocationsEqual(Location loc1, Location loc2)
        {
            const float tolerance = 0.000001f;
            return Math.Abs(loc1.Latitude - loc2.Latitude) < tolerance &&
                   Math.Abs(loc1.Longitude - loc2.Longitude) < tolerance;
        }

        private static Location CalculateNextPosition(Location current, Location destination, float stepSize)
        {
            float newLat = current.Latitude + (destination.Latitude - current.Latitude) * stepSize;
            float newLon = current.Longitude + (destination.Longitude - current.Longitude) * stepSize;
            return new Location
            {
                Latitude = newLat,
                Longitude = newLon
            };
        }
    }
}
