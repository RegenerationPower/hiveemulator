using DevOpsProject.Shared.Models;
using System.Linq;

namespace DevOpsProject.HiveMind.Logic.State
{
    public static class HiveInMemoryState
    {
        private static readonly object _operationalAreaLock = new();
        private static readonly object _telemetryLock = new();
        private static readonly object _movementLock = new();
        private static readonly object _interferenceLock = new();
        private static readonly object _droneLock = new();

        private static HiveOperationalArea _operationalArea;

        private static bool _isTelemetryRunning;
        private static bool _isMoving;

        private static Location? _currentLocation;
        private static Location? _destination;

        private static List<InterferenceModel> _interferences = new List<InterferenceModel>();
        private static readonly Dictionary<Guid, Drone> _drones = new();

        public static HiveOperationalArea OperationalArea
        {
            get
            {
                lock (_operationalAreaLock)
                {
                    return _operationalArea;
                }
            }
            set
            {
                lock (_operationalAreaLock)
                {
                    _operationalArea = value;
                }
            }
        }

        public static List<InterferenceModel> Interferences
        {
            get
            {
                lock (_interferenceLock)
                {
                    return _interferences.ToList();
                }
            }
            set
            {
                lock (_interferenceLock)
                {
                    _interferences = value;
                }
            }
        }

        public static bool AddInterference(InterferenceModel interferenceModel)
        {
            lock (_interferenceLock)
            {
                if (_interferences.FirstOrDefault(i => i.Id == interferenceModel.Id) is not null)
                    return false;
                
                _interferences.Add(interferenceModel);
                return true;
            }
        }

        public static void RemoveInterference(Guid interferenceId)
        {
            lock (_interferenceLock)
            {
                var interferenceToRemove = _interferences.FirstOrDefault(i => i.Id == interferenceId);
                if (interferenceToRemove is not null) 
                    _interferences.Remove(interferenceToRemove);
            }
        }

        public static bool IsTelemetryRunning
        {
            get
            {
                lock (_telemetryLock)
                {
                    return _isTelemetryRunning;
                }
            }
            set
            {
                lock (_telemetryLock)
                {
                    _isTelemetryRunning = value;
                }
            }
        }

        public static bool IsMoving
        {
            get 
            { 
                lock (_movementLock) 
                { 
                    return _isMoving; 
                } 
            }
            set 
            { 
                lock (_movementLock) 
                { 
                    _isMoving = value; 
                } 
            }
        }

        public static Location? CurrentLocation
        {
            get
            {
                lock (_movementLock) { return _currentLocation; }
            }
            set
            {
                lock (_movementLock) { _currentLocation = value; }
            }
        }

        public static Location? Destination
        {
            get
            {
                lock (_movementLock) { return _destination; }
            }
            set
            {
                lock (_movementLock) { _destination = value; }
            }
        }

        public static IReadOnlyCollection<Drone> Drones
        {
            get
            {
                lock (_droneLock)
                {
                    return _drones.Values.Select(CloneDrone).ToList();
                }
            }
        }

        public static bool UpsertDrone(Drone drone)
        {
            if (drone == null)
            {
                return false;
            }

            lock (_droneLock)
            {
                bool isNew = !_drones.ContainsKey(drone.Id);
                _drones[drone.Id] = CloneDrone(drone);
                return isNew;
            }
        }

        public static bool RemoveDrone(Guid droneId)
        {
            lock (_droneLock)
            {
                return _drones.Remove(droneId);
            }
        }

        public static Drone? GetDrone(Guid droneId)
        {
            lock (_droneLock)
            {
                if (_drones.TryGetValue(droneId, out var drone))
                {
                    return CloneDrone(drone);
                }

                return null;
            }
        }

        private static Drone CloneDrone(Drone drone)
        {
            return new Drone
            {
                Id = drone.Id,
                Type = drone.Type,
                Connections = drone.Connections
                    .Select(connection => new DroneConnection
                    {
                        TargetDroneId = connection.TargetDroneId,
                        Weight = connection.Weight
                    }).ToList()
            };
        }
    }
}
