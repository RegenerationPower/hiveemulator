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

### Redis
```bash
docker run --name redis -d -p 6379:6379 redis
```

### Map Component
```bash
cd src/MapClient

npm install

npm run dev
```

### Communiction Control
```bash
cd src/CommunicationControl

dotnet run  --project DevOpsProject/DevOpsProject.CommunicationControl.API.csproj
```

### Hive Mind
```bash
cd src/CommunicationControl

dotnet run  --project DevOpsProject.HiveMind.API/DevOpsProject.HiveMind.API.csproj
```


## Usage

1. Map Control is available at http://localhost:3000
2. Redis - Get available keys:
   ```bash
        docker exec -it redis redis-cli
        keys *
        get [hiveKey]
    ```

3. Communication Control Swagger: http://localhost:8080

### HiveMind Drone Relay API
- `GET /api/v1/drones` – list the drones currently registered in the HiveMind swarm cache.
- `PUT /api/v1/drones` – register or update a drone payload with its type (Scout/Striker/Relay) and weighted connections.
- `DELETE /api/v1/drones/{droneId}` – remove a drone from the swarm graph.
- `GET /api/v1/drones/{droneId}/analysis?minWeight=0.5` – evaluate whether HiveMind can reach a drone through relay links that meet the specified minimum connection weight (defaults to `0.5`).
- `GET /api/v1/drones/{droneId}/commands` – retrieve all pending commands for a drone with numbering (returns 204 No Content if no commands available).
- `POST /api/v1/drones/{droneId}/commands` – send a command directly to a specific drone (for HiveMind to control drones). **Note:** Cannot send individual commands to drones that are in a Hive. Use Hive command endpoint instead.

### HiveMind Hive Management API
- `POST /api/v1/hives` – create a new Hive with ID and optional name. Hive cannot be updated after creation.
- `GET /api/v1/hives` – get all Hives.
- `GET /api/v1/hives/{hiveId}` – get a specific Hive by ID.
- `DELETE /api/v1/hives/{hiveId}` – delete a Hive and remove all its drones from the Hive.

### HiveMind Hive and Drone Communication API
- `GET /api/v1/hives/{hiveId}/drones` – get all drones in a specific Hive (swarm group).
- `POST /api/v1/hives/{hiveId}/drones/{droneId}/join` – allow a drone to join/connect to a specific Hive. The drone must be registered first via `PUT /api/v1/drones`. A drone cannot be in multiple Hives simultaneously.
- `GET /api/v1/hives/{hiveId}/drones/{droneId}/connected` – get information about drones connected to the specified drone within the same Hive (based on connection graph).
- `POST /api/v1/hives/{hiveId}/commands` – send a command to all drones in a Hive. Individual commands for each drone are cleared and replaced with the new Hive command.
- `POST /api/v1/hives/{hiveId}/drones/{droneId}/commands/mesh?minWeight=0.5` – send a command to a drone through the Mesh network using relay drones. Finds the shortest route with the best connection quality and sends relay commands to intermediate drones for forwarding. Returns route information including path, minimum link weight, hop count, and number of relays used.

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