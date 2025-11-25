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
using DevOpsProject.Shared.Models.DTO.hive;
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

groupBuilder.MapGet("hives/{hiveId}/drones/{droneId}/connected", (string hiveId, string droneId, [FromServices] IDroneCommandService commandService) =>
{
    var connectedDrones = commandService.GetConnectedDrones(hiveId, droneId);
    return Results.Ok(new { hiveId, droneId, connectedDrones, count = connectedDrones.Count });
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
