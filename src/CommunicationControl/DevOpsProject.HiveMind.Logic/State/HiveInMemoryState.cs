#nullable enable
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using System.Linq;

namespace DevOpsProject.HiveMind.Logic.State
{
    public enum HiveMembershipResult
    {
        Added,
        AlreadyInTargetHive,
        InAnotherHive
    }

    public static class HiveInMemoryState
    {
        private static readonly object _hiveIdLock = new();
        private static readonly object _operationalAreaLock = new();
        private static readonly object _telemetryLock = new();
        private static readonly object _movementLock = new();
        private static readonly object _interferenceLock = new();
        private static readonly object _droneLock = new();
        private static readonly object _droneCommandLock = new();
        private static readonly object _hiveDroneLock = new();
        private static readonly object _hiveLock = new();

        private static HiveOperationalArea _operationalArea;

        private static bool _isTelemetryRunning;
        private static bool _isMoving;

        private static Location? _currentLocation;
        private static Location? _destination;

        private static List<InterferenceModel> _interferences = new List<InterferenceModel>();
        private static readonly Dictionary<string, Drone> _drones = new();
        private static readonly Dictionary<string, Queue<DroneCommand>> _droneCommands = new();
        private static readonly Dictionary<string, HashSet<string>> _hiveDrones = new(); // HiveID -> Set of Drone IDs
        private static readonly Dictionary<string, string> _droneHiveMapping = new(); // DroneID -> HiveID
        private static readonly Dictionary<string, Hive> _hives = new(); // HiveID -> Hive
        private static readonly Dictionary<string, HashSet<string>> _hiveEntryRelays = new(); // HiveID -> Relay IDs
        private static string? _hiveId;
        public static string? GetHiveId()
        {
            lock (_hiveIdLock)
            {
                return _hiveId;
            }
        }

        public static void SetHiveId(string hiveId)
        {
            lock (_hiveIdLock)
            {
                _hiveId = hiveId;
            }
        }

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

        public static bool RemoveDrone(string droneId)
        {
            lock (_droneLock)
            {
                return _drones.Remove(droneId);
            }
        }

        public static Drone? GetDrone(string droneId)
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

        public static void AddDroneCommand(DroneCommand command)
        {
            if (command == null)
            {
                return;
            }

            lock (_droneCommandLock)
            {
                if (!_droneCommands.ContainsKey(command.TargetDroneId))
                {
                    _droneCommands[command.TargetDroneId] = new Queue<DroneCommand>();
                }
                _droneCommands[command.TargetDroneId].Enqueue(command);
            }
        }

        public static DroneCommand? GetNextDroneCommand(string droneId)
        {
            lock (_droneCommandLock)
            {
                if (_droneCommands.TryGetValue(droneId, out var queue) && queue.Count > 0)
                {
                    return queue.Dequeue();
                }
                return null;
            }
        }

        public static IReadOnlyCollection<DroneCommand> GetAllDroneCommands(string droneId)
        {
            lock (_droneCommandLock)
            {
                if (_droneCommands.TryGetValue(droneId, out var queue))
                {
                    return queue.ToList();
                }
                return Array.Empty<DroneCommand>();
            }
        }

        public static void ClearDroneCommands(string droneId)
        {
            lock (_droneCommandLock)
            {
                if (_droneCommands.TryGetValue(droneId, out var queue))
                {
                    queue.Clear();
                }
            }
        }

        public static void RemoveDroneCommands(string droneId)
        {
            lock (_droneCommandLock)
            {
                _droneCommands.Remove(droneId);
            }
        }

        public static HiveMembershipResult AddDroneToHive(string hiveId, string droneId)
        {
            lock (_hiveDroneLock)
            {
                // Check if drone is already in another hive
                if (_droneHiveMapping.TryGetValue(droneId, out var existingHiveId))
                {
                    if (existingHiveId == hiveId)
                    {
                        // Already in this hive
                        return HiveMembershipResult.AlreadyInTargetHive;
                    }
                    // Drone is in another hive
                    return HiveMembershipResult.InAnotherHive;
                }

                // Add drone to hive
                if (!_hiveDrones.ContainsKey(hiveId))
                {
                    _hiveDrones[hiveId] = new HashSet<string>();
                }
                _hiveDrones[hiveId].Add(droneId);
                _droneHiveMapping[droneId] = hiveId;
                return HiveMembershipResult.Added;
            }
        }

        public static bool RemoveDroneFromHive(string droneId)
        {
            lock (_hiveDroneLock)
            {
                if (!_droneHiveMapping.TryGetValue(droneId, out var hiveId))
                {
                    return false;
                }

                if (_hiveDrones.TryGetValue(hiveId, out var drones))
                {
                    drones.Remove(droneId);
                    if (drones.Count == 0)
                    {
                        _hiveDrones.Remove(hiveId);
                    }
                }
                _droneHiveMapping.Remove(droneId);
                return true;
            }
        }

        public static IReadOnlyCollection<string> GetHiveDrones(string hiveId)
        {
            lock (_hiveDroneLock)
            {
                if (_hiveDrones.TryGetValue(hiveId, out var drones))
                {
                    return drones.ToList();
                }
                return Array.Empty<string>();
            }
        }

        public static string? GetDroneHive(string droneId)
        {
            lock (_hiveDroneLock)
            {
                return _droneHiveMapping.TryGetValue(droneId, out var hiveId) ? hiveId : null;
            }
        }

        public static void SetHiveEntryRelays(string hiveId, IEnumerable<string> relayIds)
        {
            lock (_hiveDroneLock)
            {
                _hiveEntryRelays[hiveId] = relayIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).ToHashSet();
            }
        }

        public static IReadOnlyCollection<string> GetHiveEntryRelays(string hiveId)
        {
            lock (_hiveDroneLock)
            {
                if (_hiveEntryRelays.TryGetValue(hiveId, out var relays))
                {
                    return relays.ToList();
                }
                return Array.Empty<string>();
            }
        }

        public static void AddHive(Hive hive)
        {
            if (hive == null || string.IsNullOrWhiteSpace(hive.Id))
            {
                return;
            }

            lock (_hiveLock)
            {
                _hives[hive.Id] = new Hive
                {
                    Id = hive.Id,
                    Name = hive.Name,
                    CreatedAt = hive.CreatedAt
                };
            }
        }

        public static bool RemoveHive(string hiveId)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                return false;
            }

            lock (_hiveLock)
            {
                return _hives.Remove(hiveId);
            }
        }

        public static Hive? GetHive(string hiveId)
        {
            if (string.IsNullOrWhiteSpace(hiveId))
            {
                return null;
            }

            lock (_hiveLock)
            {
                if (_hives.TryGetValue(hiveId, out var hive))
                {
                    return new Hive
                    {
                        Id = hive.Id,
                        Name = hive.Name,
                        CreatedAt = hive.CreatedAt
                    };
                }
                return null;
            }
        }

        public static IReadOnlyCollection<Hive> GetAllHives()
        {
            lock (_hiveLock)
            {
                return _hives.Values
                    .Select(h => new Hive
                    {
                        Id = h.Id,
                        Name = h.Name,
                        CreatedAt = h.CreatedAt
                    })
                    .ToList();
            }
        }
    }
}
