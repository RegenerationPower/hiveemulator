using Asp.Versioning;
using Asp.Versioning.Builder;
using DevOpsProject.HiveMind.API.DI;
using DevOpsProject.HiveMind.API.Middleware;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.HiveMind.Logic.State;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Enums;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Commands.Drone;
using DevOpsProject.Shared.Models.DTO.Common;
using DevOpsProject.Shared.Models.DTO.Drone;
using DevOpsProject.Shared.Models.DTO.Hive;
using DevOpsProject.Shared.Models.DTO.Topology;
using Microsoft.AspNetCore.Mvc;
using DevOpsProject.Shared.Models.HiveMindCommands;
using FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

builder.Services.AddApiVersioningConfiguration();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HiveMind - V1", Version = "v1.0" });
});

builder.Services.AddOptionsConfiguration(builder.Configuration);

//builder.Services.AddValidatorsConfiguration();
builder.Services.AddHiveMindLogic();

builder.Services.AddHttpClientsConfiguration();

string corsPolicyName = "HiveMindCorsPolicy";
builder.Services.AddCorsConfiguration(corsPolicyName);

builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();
builder.Services.AddProblemDetails();

// Configure JSON options to serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var hiveMindService = scope.ServiceProvider.GetRequiredService<IHiveMindService>();
        await hiveMindService.ConnectHive();
    }
    catch (Exception ex)
    {
        logger.LogError($"Error occured while connecting Hive to Communication Control. \nException text: {ex.Message}");
        Environment.Exit(1);
    }
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(corsPolicyName);

//app.UseHttpsRedirection();

app.UseAuthorization();

ApiVersionSet apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();
RouteGroupBuilder groupBuilder = app.MapGroup("api/v{apiVersion:apiVersion}").WithApiVersionSet(apiVersionSet);

// Ping endpoint
groupBuilder.MapGet("ping", ([FromQuery] DateTime? timestamp, [FromQuery] string? hiveID, IOptionsSnapshot<HiveCommunicationConfig> config) =>
{
    var response = new PingResponse
    {
        Status = "OK",
        Timestamp = DateTime.UtcNow
    };
    return Results.Ok(response);
});

groupBuilder.MapPost("command", async (HiveMindCommand command, [FromServices]ICommandHandlerFactory factory, [FromServices]IHiveMindService hiveMindService) =>
{
    var handler = factory.GetHandler(command);
    await handler.HandleAsync(command);
    
    // Return HiveMind telemetry as response
    var telemetry = hiveMindService.GetCurrentTelemetry();
    return Results.Ok(telemetry);
});

groupBuilder.MapGet("drones", ([FromServices] IDroneRelayService relayService) =>
{
    var drones = relayService.GetSwarm();
    return Results.Ok(drones);
});

groupBuilder.MapPut("drones", ([FromBody] Drone drone, [FromServices] IDroneRelayService relayService) =>
{
    bool isNew = relayService.UpsertDrone(drone);
    if (isNew)
    {
        return Results.Created($"/api/v1/drones/{drone.Id}", new { message = $"Drone {drone.Id} successfully created", droneId = drone.Id });
    }
    else
    {
        return Results.Ok(new { message = $"Drone {drone.Id} already exists and was updated", droneId = drone.Id });
    }
});

groupBuilder.MapPost("drones/batch", ([FromBody] BatchCreateDronesRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = relayService.BatchCreateDrones(request);
    
    if (response.Failed > 0 && response.Created == 0 && response.Updated == 0)
    {
        return Results.BadRequest(response);
    }

    return Results.Ok(response);
});

// Delete all drones endpoint
groupBuilder.MapDelete("drones", ([FromServices] IDroneRelayService relayService) =>
{
    var removedCount = relayService.RemoveAllDrones();
    return Results.Ok(new 
    { 
        message = $"All drones removed successfully",
        removedCount = removedCount
    });
});

groupBuilder.MapDelete("drones/{droneId}", (string droneId, [FromServices] IDroneRelayService relayService) =>
{
    var removed = relayService.RemoveDrone(droneId);
    return removed ? Results.NoContent() : Results.NotFound();
});

groupBuilder.MapGet("drones/{droneId}/analysis", (string droneId, [FromQuery] double? minWeight, [FromServices] IDroneRelayService relayService) =>
{
    var analysis = relayService.AnalyzeConnection(droneId, minWeight ?? 0.5);
    return Results.Ok(analysis);
});

groupBuilder.MapGet("hive/identity", () =>
{
    var hiveId = HiveInMemoryState.GetHiveId();
    return Results.Ok(new { hiveId });
});

groupBuilder.MapPost("hive/identity", async ([FromBody] HiveIdentityUpdateRequest request, [FromServices] IHiveMindService hiveMindService) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.HiveId))
    {
        return Results.BadRequest(new { message = "HiveId is required" });
    }

    var success = await hiveMindService.UpdateHiveIdentityAsync(request.HiveId, request.Reconnect);
    if (!success)
    {
        return Results.BadRequest(new { message = "Failed to update Hive identity. Ensure hiveId is provided." });
    }

    return Results.Ok(new { hiveId = request.HiveId.Trim(), reconnected = request.Reconnect });
});

// Hive management endpoints
groupBuilder.MapPost("hives", ([FromBody] HiveCreateRequest request, [FromServices] IHiveService hiveService) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.HiveId))
    {
        return Results.BadRequest(new { message = "Hive ID is required" });
    }

    try
    {
        var hive = hiveService.CreateHive(request.HiveId, request.Name);
        return Results.Created($"/api/v1/hives/{hive.Id}", hive);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

groupBuilder.MapGet("hives", ([FromServices] IHiveService hiveService) =>
{
    var hives = hiveService.GetAllHives();
    return Results.Ok(new { hives, count = hives.Count });
});

groupBuilder.MapGet("hives/{hiveId}", (string hiveId, [FromServices] IHiveService hiveService) =>
{
    var hive = hiveService.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found" });
    }
    return Results.Ok(hive);
});

// Delete all hives endpoint
groupBuilder.MapDelete("hives", ([FromServices] IHiveService hiveService) =>
{
    var deletedCount = hiveService.DeleteAllHives();
    return Results.Ok(new 
    { 
        message = $"All hives deleted successfully",
        deletedCount = deletedCount
    });
});

groupBuilder.MapDelete("hives/{hiveId}", (string hiveId, [FromServices] IHiveService hiveService) =>
{
    var deleted = hiveService.DeleteHive(hiveId);
    return deleted ? Results.NoContent() : Results.NotFound(new { message = $"Hive {hiveId} not found" });
});

groupBuilder.MapGet("hives/{hiveId}/drones", (string hiveId, [FromServices] IDroneCommandService commandService) =>
{
    var drones = commandService.GetHiveDrones(hiveId);
    return Results.Ok(new { hiveId, drones, count = drones.Count });
});

// Drone communication endpoints within Hive context
groupBuilder.MapPost("hives/{hiveId}/drones/{droneId}/join", (string hiveId, string droneId, [FromBody] DroneJoinRequest request, [FromServices] IDroneCommandService commandService) =>
{
    var joinRequest = request ?? new DroneJoinRequest { DroneId = droneId };
    if (string.IsNullOrWhiteSpace(joinRequest.DroneId))
    {
        joinRequest.DroneId = droneId;
    }
    
    var response = commandService.JoinDrone(hiveId, joinRequest);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

groupBuilder.MapPost("hives/{hiveId}/drones/batch-join", (string hiveId, [FromBody] BatchJoinDronesRequest request, [FromServices] IDroneCommandService commandService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = commandService.BatchJoinDrones(hiveId, request);
    
    if (response.Failed > 0 && response.Joined == 0 && response.AlreadyInHive == 0)
    {
        return Results.BadRequest(response);
    }

    return Results.Ok(response);
});

groupBuilder.MapDelete("hives/{hiveId}/drones/{droneId}", (string hiveId, string droneId, [FromServices] IDroneCommandService commandService) =>
{
    var hive = HiveInMemoryState.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found." });
    }

    var currentHiveId = HiveInMemoryState.GetDroneHive(droneId);
    if (currentHiveId == null)
    {
        return Results.BadRequest(new { message = $"Drone {droneId} is not part of any Hive." });
    }

    if (!string.Equals(currentHiveId, hiveId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = $"Drone {droneId} belongs to Hive {currentHiveId}, not {hiveId}." });
    }

    var removed = commandService.RemoveDroneFromHive(hiveId, droneId);
    return removed ? Results.NoContent() : Results.BadRequest(new { message = $"Failed to remove drone {droneId} from Hive {hiveId}." });
});

groupBuilder.MapPost("hives/{hiveId}/drones/batch-leave", (string hiveId, [FromBody] BatchRemoveDronesRequest request, [FromServices] IDroneCommandService commandService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = commandService.BatchRemoveDrones(hiveId, request);
    if (response.Failed == response.TotalRequested && response.Removed == 0 && response.NotInHive == 0)
    {
        return Results.BadRequest(response);
    }

    return Results.Ok(response);
});

groupBuilder.MapGet("hives/{hiveId}/drones/{droneId}/connected", (string hiveId, string droneId, [FromServices] IDroneCommandService commandService) =>
{
    var connectedDrones = commandService.GetConnectedDrones(hiveId, droneId);
    return Results.Ok(new { hiveId, droneId, connectedDrones, count = connectedDrones.Count });
});

// Topology management endpoints
groupBuilder.MapPost("hives/{hiveId}/topology/rebuild", (string hiveId, [FromBody] TopologyRebuildRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    // Use hiveId from URL
    request.HiveId = hiveId;

    var response = relayService.RebuildTopology(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

groupBuilder.MapPost("hives/{hiveId}/topology/connect-hivemind", (string hiveId, [FromBody] ConnectToHiveMindRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    // Use hiveId from URL
    request.HiveId = hiveId;

    var response = relayService.ConnectToHiveMind(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

groupBuilder.MapGet("hives/{hiveId}/topology/connectivity", (string hiveId, [FromServices] IDroneRelayService relayService) =>
{
    var response = relayService.AnalyzeSwarmConnectivity(hiveId);
    return Results.Ok(response);
});

// Connection degradation endpoints (for emulating connection degradation)
groupBuilder.MapPost("drones/connections/degrade", ([FromBody] DegradeConnectionRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = relayService.DegradeConnection(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
});

groupBuilder.MapPost("drones/connections/batch-degrade", ([FromBody] BatchDegradeConnectionsRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = relayService.BatchDegradeConnections(request);
    return Results.Ok(response);
});

// Mesh command endpoint - send command through mesh network (only for drones in Hive)
groupBuilder.MapPost("hives/{hiveId}/drones/{droneId}/commands/mesh", (string hiveId, string droneId, [FromBody] DroneCommand command, [FromQuery] double? minWeight, [FromServices] IDroneRelayService relayService) =>
{
    if (command == null)
    {
        return Results.BadRequest(new { message = "Command cannot be null" });
    }
    
    // Check if Hive exists
    var hive = HiveInMemoryState.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found." });
    }
    
    // Check if drone exists
    var drone = HiveInMemoryState.GetDrone(droneId);
    if (drone == null)
    {
        return Results.NotFound(new { message = $"Drone {droneId} not found. Cannot send command to non-existent drone." });
    }
    
    // Check if drone is in the specified Hive
    var droneHiveId = HiveInMemoryState.GetDroneHive(droneId);
    if (droneHiveId == null || droneHiveId != hiveId)
    {
        return Results.BadRequest(new 
        { 
            message = $"Drone {droneId} is not in Hive {hiveId}. Mesh commands can only be sent to drones that are part of the specified Hive.",
            droneId = droneId,
            hiveId = hiveId,
            actualHiveId = droneHiveId
        });
    }
    
    // Set commandPayload to null for commands that don't need it
    if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
    {
        command.CommandPayload = null;
    }
    
    // Send command via mesh network
    var meshResponse = relayService.SendCommandViaMesh(droneId, command, minWeight ?? 0.5);
    
    if (!meshResponse.Success)
    {
        return Results.BadRequest(meshResponse);
    }
    
    return Results.Ok(meshResponse);
});

// Drone command endpoints (direct to drone, not through Hive)
groupBuilder.MapGet("drones/{droneId}/commands", (string droneId, [FromServices] IDroneCommandService commandService) =>
{
    var commands = commandService.GetAllCommands(droneId);
    if (!commands.Any())
    {
        return Results.NoContent();
    }
    
    var hiveId = HiveInMemoryState.GetDroneHive(droneId);
    
    // Return commands with numbering (index starting from 1)
    var numberedCommands = commands.Select((cmd, index) => new
    {
        order = index + 1,
        command = cmd
    }).ToList();
    
    var response = new
    {
        droneId = droneId,
        hiveId = hiveId,
        totalCommands = commands.Count,
        commands = numberedCommands
    };
    
    return Results.Ok(response);
});

groupBuilder.MapPost("drones/{droneId}/commands", (string droneId, [FromBody] DroneCommand command, [FromServices] IDroneCommandService commandService) =>
{
    if (command == null)
    {
        return Results.BadRequest(new { message = "Command cannot be null" });
    }
    
    // Check if drone exists
    var drone = HiveInMemoryState.GetDrone(droneId);
    if (drone == null)
    {
        return Results.NotFound(new { message = $"Drone {droneId} not found. Cannot send command to non-existent drone." });
    }
    
    // Check if drone is in a Hive
    var hiveId = HiveInMemoryState.GetDroneHive(droneId);
    if (hiveId != null)
    {
        return Results.BadRequest(new 
        { 
            message = $"Drone {droneId} is in Hive {hiveId} and cannot receive individual commands. Use POST /api/v1/hives/{hiveId}/commands to send commands to all drones in the Hive.",
            droneId = droneId,
            hiveId = hiveId
        });
    }
    
    // Set target drone ID from URL
    command.TargetDroneId = droneId;
    
    // Auto-generate timestamp if not provided
    if (command.Timestamp == default)
    {
        command.Timestamp = DateTime.UtcNow;
    }
    
    // Always auto-generate command ID (ignore any provided value)
    command.CommandId = Guid.NewGuid();
    
    // Set commandPayload to null for commands that don't need it
    if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
    {
        command.CommandPayload = null;
    }
    else if (command.CommandType == DroneCommandType.Move)
    {
        // Validate Move command payload
        if (command.CommandPayload == null)
        {
            return Results.BadRequest(new { message = "CommandPayload is required for Move command" });
        }
        
        // Try to deserialize as MoveDroneCommandPayload
        try
        {
            // First, check if the payload contains the correct field names
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(command.CommandPayload);
            var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = payloadDoc.RootElement;
            
            // Check for required fields with correct names (case-insensitive but prefer exact match)
            var hasLat = root.TryGetProperty("lat", out var latElement) || root.TryGetProperty("Lat", out latElement);
            var hasLon = root.TryGetProperty("lon", out var lonElement) || root.TryGetProperty("Lon", out lonElement);
            var hasHeight = root.TryGetProperty("height", out var heightElement) || root.TryGetProperty("Height", out heightElement);
            var hasSpeed = root.TryGetProperty("speed", out var speedElement) || root.TryGetProperty("Speed", out speedElement);
            
            // Check for incorrect field names
            var hasLatitude = root.TryGetProperty("Latitude", out _) || root.TryGetProperty("latitude", out _);
            var hasLongitude = root.TryGetProperty("Longitude", out _) || root.TryGetProperty("longitude", out _);
            var hasAltitude = root.TryGetProperty("Altitude", out _) || root.TryGetProperty("altitude", out _);
            
            var validationErrors = new List<string>();
            
            if (hasLatitude || hasLongitude || hasAltitude)
            {
                validationErrors.Add("Invalid field names detected. Use 'lat', 'lon', 'height', 'speed' instead of 'Latitude', 'Longitude', 'Altitude'");
            }
            
            if (!hasLat || !hasLon || !hasHeight || !hasSpeed)
            {
                var missingFields = new List<string>();
                if (!hasLat && !hasLatitude) missingFields.Add("lat");
                if (!hasLon && !hasLongitude) missingFields.Add("lon");
                if (!hasHeight && !hasAltitude) missingFields.Add("height");
                if (!hasSpeed) missingFields.Add("speed");
                
                if (missingFields.Any())
                {
                    validationErrors.Add($"Missing required fields: {string.Join(", ", missingFields)}");
                }
            }
            
            if (validationErrors.Any())
            {
                return Results.BadRequest(new 
                { 
                    message = "Invalid Move command payload",
                    errors = validationErrors,
                    requiredFields = new { lat = "float", lon = "float", height = "float (> 0)", speed = "float (> 0)" },
                    example = new { lat = 48.719547, lon = 38.092680, height = 100.5, speed = 20.0 }
                });
            }
            
            // Now deserialize with proper field mapping
            var movePayload = System.Text.Json.JsonSerializer.Deserialize<MoveDroneCommandPayload>(payloadJson);
            if (movePayload == null)
            {
                return Results.BadRequest(new { message = "Invalid Move command payload. Failed to deserialize payload." });
            }
            
            // Validate all required fields are present and have valid values
            if (movePayload.Lat == 0 && movePayload.Lon == 0)
            {
                validationErrors.Add("lat and lon cannot both be 0");
            }
            
            if (movePayload.Height <= 0)
            {
                validationErrors.Add("height must be greater than 0");
            }
            
            if (movePayload.Speed <= 0)
            {
                validationErrors.Add("speed must be greater than 0");
            }
            
            if (validationErrors.Any())
            {
                return Results.BadRequest(new 
                { 
                    message = "Invalid Move command payload",
                    errors = validationErrors,
                    requiredFields = new { lat = "float", lon = "float", height = "float (> 0)", speed = "float (> 0)" }
                });
            }
            
            // Replace with properly typed payload
            command.CommandPayload = movePayload;
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new 
            { 
                message = "Invalid Move command payload format",
                error = ex.Message,
                expectedFormat = new { lat = "float", lon = "float", height = "float", speed = "float" },
                example = new { lat = 48.719547, lon = 38.092680, height = 100.5, speed = 20.0 }
            });
        }
    }
    
    commandService.SendCommand(command);
    return Results.Created($"/api/v1/drones/{droneId}/commands/{command.CommandId}", command);
});


// Hive command endpoint - send command to all drones in Hive
groupBuilder.MapPost("hives/{hiveId}/commands", (string hiveId, [FromBody] DroneCommand command, [FromServices] IDroneCommandService commandService) =>
{
    if (command == null)
    {
        return Results.BadRequest(new { message = "Command cannot be null" });
    }
    
    // Check if Hive exists
    var hive = HiveInMemoryState.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found." });
    }
    
    // Auto-generate timestamp if not provided
    if (command.Timestamp == default)
    {
        command.Timestamp = DateTime.UtcNow;
    }
    
    // Always auto-generate command ID (ignore any provided value)
    command.CommandId = Guid.NewGuid();
    
    // Set commandPayload to null for commands that don't need it
    if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
    {
        command.CommandPayload = null;
    }
    else if (command.CommandType == DroneCommandType.Move)
    {
        // Validate Move command payload
        if (command.CommandPayload == null)
        {
            return Results.BadRequest(new { message = "CommandPayload is required for Move command" });
        }
        
        // Try to deserialize as MoveDroneCommandPayload
        try
        {
            // First, check if the payload contains the correct field names
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(command.CommandPayload);
            var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = payloadDoc.RootElement;
            
            // Check for required fields with correct names (case-insensitive but prefer exact match)
            var hasLat = root.TryGetProperty("lat", out var latElement) || root.TryGetProperty("Lat", out latElement);
            var hasLon = root.TryGetProperty("lon", out var lonElement) || root.TryGetProperty("Lon", out lonElement);
            var hasHeight = root.TryGetProperty("height", out var heightElement) || root.TryGetProperty("Height", out heightElement);
            var hasSpeed = root.TryGetProperty("speed", out var speedElement) || root.TryGetProperty("Speed", out speedElement);
            
            // Check for incorrect field names
            var hasLatitude = root.TryGetProperty("Latitude", out _) || root.TryGetProperty("latitude", out _);
            var hasLongitude = root.TryGetProperty("Longitude", out _) || root.TryGetProperty("longitude", out _);
            var hasAltitude = root.TryGetProperty("Altitude", out _) || root.TryGetProperty("altitude", out _);
            
            var validationErrors = new List<string>();
            
            if (hasLatitude || hasLongitude || hasAltitude)
            {
                validationErrors.Add("Invalid field names detected. Use 'lat', 'lon', 'height', 'speed' instead of 'Latitude', 'Longitude', 'Altitude'");
            }
            
            if (!hasLat || !hasLon || !hasHeight || !hasSpeed)
            {
                var missingFields = new List<string>();
                if (!hasLat && !hasLatitude) missingFields.Add("lat");
                if (!hasLon && !hasLongitude) missingFields.Add("lon");
                if (!hasHeight && !hasAltitude) missingFields.Add("height");
                if (!hasSpeed) missingFields.Add("speed");
                
                if (missingFields.Any())
                {
                    validationErrors.Add($"Missing required fields: {string.Join(", ", missingFields)}");
                }
            }
            
            if (validationErrors.Any())
            {
                return Results.BadRequest(new 
                { 
                    message = "Invalid Move command payload",
                    errors = validationErrors,
                    requiredFields = new { lat = "float", lon = "float", height = "float (> 0)", speed = "float (> 0)" },
                    example = new { lat = 48.719547, lon = 38.092680, height = 100.5, speed = 20.0 }
                });
            }
            
            // Now deserialize with proper field mapping
            var movePayload = System.Text.Json.JsonSerializer.Deserialize<MoveDroneCommandPayload>(payloadJson);
            if (movePayload == null)
            {
                return Results.BadRequest(new { message = "Invalid Move command payload. Failed to deserialize payload." });
            }
            
            // Validate all required fields are present and have valid values
            if (movePayload.Lat == 0 && movePayload.Lon == 0)
            {
                validationErrors.Add("lat and lon cannot both be 0");
            }
            
            if (movePayload.Height <= 0)
            {
                validationErrors.Add("height must be greater than 0");
            }
            
            if (movePayload.Speed <= 0)
            {
                validationErrors.Add("speed must be greater than 0");
            }
            
            if (validationErrors.Any())
            {
                return Results.BadRequest(new 
                { 
                    message = "Invalid Move command payload",
                    errors = validationErrors,
                    requiredFields = new { lat = "float", lon = "float", height = "float (> 0)", speed = "float (> 0)" }
                });
            }
            
            // Replace with properly typed payload
            command.CommandPayload = movePayload;
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new 
            { 
                message = "Invalid Move command payload format",
                error = ex.Message,
                expectedFormat = new { lat = "float", lon = "float", height = "float", speed = "float" },
                example = new { lat = 48.719547, lon = 38.092680, height = 100.5, speed = 20.0 }
            });
        }
    }

    var sentCount = commandService.SendCommandToHive(hiveId, command);
    if (sentCount == 0)
    {
        return Results.BadRequest(new { message = $"Hive {hiveId} has no drones. Command not sent." });
    }
    
    return Results.Ok(new 
    { 
        message = $"Command sent to all drones in Hive {hiveId}",
        hiveId = hiveId,
        commandType = command.CommandType,
        dronesAffected = sentCount,
        commandId = command.CommandId
    });
});

app.Run();
