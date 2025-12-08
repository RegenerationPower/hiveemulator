# Hive Emulator

- [Hive Emulator](#hive-emulator)
  - [About](#about)
  - [Installation](#installation)
    - [Redis](#redis)
    - [Map Component](#map-component)
    - [Communiction Control](#communiction-control)
    - [Hive Mind](#hive-mind)
  - [Usage](#usage)
  - [Build](#build)
    - [Map Clinet](#map-clinet)
    - [Communiction Control](#communiction-control-1)
    - [Hive Mind](#hive-mind-1)
    - [Communiction Control](#communiction-control-2)

## About
This is a demo project used in the Uni DevOps course

## Installation

### Швидкий старт з Docker Compose (рекомендовано)

Запуск всієї системи одним командою:

```bash
docker compose up --build
```

Це запустить:
- **Communication Control API** на порту 8080
- **HiveMind API** на порту 5149
- **Map Client** на порту 3000
- **Redis** на порту 6379
- **PostgreSQL** на порту 5432

### Ручна установка (для розробки)

#### Redis
```bash
docker run --name redis -d -p 6379:6379 redis
```

#### Map Component
```bash
cd src/MapClient
npm install
npm run dev
```

#### Communication Control
```bash
cd src/CommunicationControl
dotnet run --project DevOpsProject/DevOpsProject.CommunicationControl.API.csproj
```

#### HiveMind
```bash
cd src/CommunicationControl
dotnet run --project DevOpsProject.HiveMind.API/DevOpsProject.HiveMind.API.csproj
```


## Usage

### Доступні сервіси

1. **Map Client** - http://localhost:3000
   - Візуалізація дронів на карті

2. **Communication Control API** - http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - Центральний контролер системи
   - Проксі-ендпоінти до HiveMind

3. **HiveMind API** - http://localhost:5149
   - Swagger UI: http://localhost:5149/swagger
   - Управління роєм дронів
   - Mesh-маршрутизація та топології

4. **Redis** - localhost:6379
   - Message bus для телеметрії
   - Перевірка ключів:
     ```bash
     docker exec -it redis redis-cli
     keys *
     get [hiveKey]
     ```

5. **PostgreSQL** - localhost:5432
   - База даних для зберігання телеметрії

### HiveMind Drone Relay API
- `GET /api/v1/drones` – list the drones currently registered in the HiveMind swarm cache.
- `PUT /api/v1/drones` – register or update a drone payload with its type (Scout/Striker/Relay) and weighted connections.
- `POST /api/v1/drones/batch` – create or update multiple drones in a single request. Request body: `{ "drones": [ { "id": "...", "type": "Scout|Striker|Relay", "connections": [...] }, ... ] }`. Returns summary with counts of created, updated, and failed drones. See `example_batch_drones.json` for a sample with 10 interconnected drones.
- `DELETE /api/v1/drones/{droneId}` – remove a drone from the swarm graph.
- `GET /api/v1/drones/{droneId}/analysis?minWeight=0.5` – evaluate whether HiveMind can reach a drone through relay links that meet the specified minimum connection weight (defaults to `0.5`).
- `GET /api/v1/drones/{droneId}/commands` – retrieve all pending commands for a drone with numbering (returns 204 No Content if no commands available).
- `POST /api/v1/drones/{droneId}/commands` – send a command directly to a specific drone (for HiveMind to control drones). **Note:** Cannot send individual commands to drones that are in a Hive. Use Hive command endpoint instead.

### HiveMind Hive Management API
- `POST /api/v1/hives` – create a new Hive with ID and optional name. Hive cannot be updated after creation.
- `GET /api/v1/hives` – get all Hives.
- `GET /api/v1/hives/{hiveId}` – get a specific Hive by ID.
- `DELETE /api/v1/hives/{hiveId}` – delete a Hive and remove all its drones from the Hive.
- `GET /api/v1/hive/identity` – show the Hive ID that HiveMind currently uses for telemetry/commands.
- `POST /api/v1/hive/identity` – change the Hive ID at runtime. Request body: `{ "hiveId": "125", "reconnect": true }`. If `reconnect` is true (default), HiveMind stops telemetry, re-registers the hive with Communication Control, and restarts telemetry under the new ID. **Identity can only be updated to hives that already exist in HiveMind. Create the hive first via `POST /api/v1/hives` or the CommunicationControl proxy endpoint.**

### HiveMind Hive and Drone Communication API
- `GET /api/v1/hives/{hiveId}/drones` – get all drones in a specific Hive (swarm group).
- `POST /api/v1/hives/{hiveId}/drones/{droneId}/join` – allow a drone to join/connect to a specific Hive. The drone must be registered first via `PUT /api/v1/drones`. A drone cannot be in multiple Hives simultaneously.
- `POST /api/v1/hives/{hiveId}/drones/batch-join` – add multiple drones to a Hive at once. Request body: `{ "droneIds": ["drone-001", "drone-002", ...] }`. Returns summary with counts of joined, already in hive, and failed drones. See `example_batch_join_hive.json` for a sample with 10 drones.
- `DELETE /api/v1/hives/{hiveId}/drones/{droneId}` – remove a drone from a Hive so it can join another one. Clears its individual command queue.
- `POST /api/v1/hives/{hiveId}/drones/batch-leave` – remove multiple drones from a Hive at once. Request body: `{ "droneIds": ["drone-001", "drone-002", ...] }`. Returns counts of removed, not-in-hive, and failed drones.
- `GET /api/v1/hives/{hiveId}/drones/{droneId}/connected` – get information about drones connected to the specified drone within the same Hive (based on connection graph).
- `POST /api/v1/hives/{hiveId}/commands` – send a command to all drones in a Hive. Individual commands for each drone are cleared and replaced with the new Hive command.
- `POST /api/v1/hives/{hiveId}/drones/{droneId}/commands/mesh?minWeight=0.5` – send a command to a drone through the Mesh network using relay drones. Finds the shortest route with the best connection quality and sends relay commands to intermediate drones for forwarding. Returns route information including path, minimum link weight, hop count, and number of relays used.

- ### HiveMind Topology Management API
- `POST /api/v1/hives/{hiveId}/topology/rebuild` – rebuild topology between drones in a Hive. Supports `mesh` (full mesh - every drone connects to every other), `star` (one central hub), or `dual_star` (two hubs) topologies. Request body: `{ "topologyType": "mesh|star|dual_star", "defaultWeight": 0.8 }`. See example files: `example_topology_rebuild_mesh.json`, `example_topology_rebuild_star.json`, `example_topology_rebuild_dual_star.json`.
- `POST /api/v1/hives/{hiveId}/topology/connect-hivemind` – register one or more relay drones as entry points between this Hive and HiveMind. This call **does not** modify the swarm’s connection graph; it only stores which relays HiveMind should start from when building mesh routes. Request body: `{ "entryRelayIds": ["relay-1", "relay-2"] }` (optional list; HiveMind auto-selects available relay drones if omitted).
- `GET /api/v1/hives/{hiveId}/topology/connectivity` – analyze connectivity of the swarm in a Hive. Returns information about connected components, isolated groups, and whether all drones are connected.

### HiveMind Connection Degradation API (Emulation)
- `POST /api/v1/drones/connections/degrade` – degrade (change weight of) a connection between two drones. Used for emulating connection degradation. Request body: `{ "fromDroneId": "drone-001", "toDroneId": "drone-002", "newWeight": 0.3 }`. Weight must be between 0.0 and 1.0. Lower values indicate degraded connection. Updates both directions of the connection (bidirectional). **If `newWeight` is `0` or less, the connection is removed entirely (channel disappears).**
- `POST /api/v1/drones/connections/batch-degrade` – degrade multiple connections at once. Request body: `{ "connections": [ { "fromDroneId": "...", "toDroneId": "...", "newWeight": 0.3 }, ... ] }`. See example file: `example_batch_degrade_connections.json`.

### Communication Control ↔ HiveMind Integration API
These endpoints live in `CommunicationControl.API` and proxy requests to HiveMind so that the entire mesh lifecycle can be observed directly in the Communication Control terminal (all responses are streamed from HiveMind).

- `POST /api/v1/hivemind/drones/batch` – forward batch drone creation/update to HiveMind.
- `POST /api/v1/hivemind/hives/{hiveId}/drones/batch-join` – add many drones to a Hive via Communication Control.
- `POST /api/v1/hivemind/hives/{hiveId}/topology/rebuild` – rebuild mesh/star/dual_star topologies (proxied to HiveMind).
- `POST /api/v1/hivemind/hives/{hiveId}/topology/connect-hivemind` – connect Hive to relay hubs through HiveMind.
- `GET /api/v1/hivemind/hives/{hiveId}/topology/connectivity` – fetch connectivity snapshots (components, isolated groups, etc.).
- `POST /api/v1/hivemind/drones/connections/degrade` – degrade/remove a connection using Communication Control.
- `POST /api/v1/hivemind/drones/connections/batch-degrade` – batch version of connection degradation.
- `POST /api/v1/hivemind/hives` – create a Hive directly inside HiveMind (proxy to `POST /api/v1/hives`). Use this before switching telemetry identity if the hive does not yet exist.

> When HiveMind sends telemetry to Communication Control, the controller now automatically fetches a connectivity snapshot through this integration layer and logs the component summary, so both services participate in the workflow.

## Build

### Map Clinet
cd src/MapClient

npm install
npm run build

### Communiction Control
cd src/CommunicationControl
dotnet publish -p:PublishProfile=FolderProfile --artifacts-path=build/CommunicationControl DevOpsProject/DevOpsProject.CommunicationControl.API.csproj 

### Hive Mind
### Communiction Control
cd src/CommunicationControl
dotnet publish -p:PublishProfile=FolderProfile --artifacts-path=build/HiveMind DevOpsProject/DevOpsProject.HiveMind.API.csproj