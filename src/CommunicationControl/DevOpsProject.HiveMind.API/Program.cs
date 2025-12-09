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
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "HiveMind API", 
        Version = "v1.0",
        Description = "API для управління роєм дронів, топологіями та mesh-маршрутизацією"
    });
    c.CustomSchemaIds(type => type.FullName);
    c.TagActionsBy(api => new[] { api.GroupName ?? "HiveMind" });
    c.DocInclusionPredicate((name, api) => true);
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
RouteGroupBuilder groupBuilder = app.MapGroup("api/v{apiVersion:apiVersion}")
    .WithApiVersionSet(apiVersionSet)
    .WithTags("HiveMind");

groupBuilder.MapGet("ping", ([FromQuery] DateTime? timestamp, [FromQuery] string? hiveID, IOptionsSnapshot<HiveCommunicationConfig> config) =>
{
    var response = new PingResponse
    {
        Status = "OK",
        Timestamp = DateTime.UtcNow
    };
    return Results.Ok(response);
})
.WithName("Ping")
.WithTags("Health")
.WithSummary("Перевірка доступності")
.WithDescription("Перевіряє доступність сервісу HiveMind")
.Produces<PingResponse>(StatusCodes.Status200OK);

groupBuilder.MapPost("command", async (HiveMindCommand command, [FromServices]ICommandHandlerFactory factory, [FromServices]IHiveMindService hiveMindService) =>
{
    var handler = factory.GetHandler(command);
    await handler.HandleAsync(command);
    
    var telemetry = hiveMindService.GetCurrentTelemetry();
    return Results.Ok(telemetry);
})
.WithName("SendHiveMindCommand")
.WithTags("Commands")
.WithSummary("Відправити команду HiveMind")
.WithDescription("Відправляє команду HiveMind (Move, Stop) та повертає поточну телеметрію")
.Accepts<HiveMindCommand>("application/json")
.Produces<HiveTelemetryModel>(StatusCodes.Status200OK);

groupBuilder.MapPut("hives/{hiveId}/telemetry", (string hiveId, [FromBody] UpdateHiveTelemetryRequest request, [FromServices] IHiveMindService hiveMindService) =>
{
    if (string.IsNullOrWhiteSpace(hiveId))
    {
        return Results.BadRequest(new UpdateHiveTelemetryResponse
        {
            Success = false,
            Message = "Hive ID is required"
        });
    }

    var hiveExists = HiveInMemoryState.GetHive(hiveId) != null;
    if (!hiveExists)
    {
        return Results.NotFound(new UpdateHiveTelemetryResponse
        {
            Success = false,
            Message = $"Hive with ID '{hiveId}' does not exist"
        });
    }

    var updated = hiveMindService.UpdateTelemetry(
        hiveId,
        request.Location,
        request.Height,
        request.Speed,
        request.IsMoving);

    if (updated)
    {
        var telemetry = hiveMindService.GetTelemetry(hiveId);
        if (telemetry == null)
        {
            return Results.NotFound(new UpdateHiveTelemetryResponse
            {
                Success = false,
                Message = $"Hive with ID '{hiveId}' does not exist"
            });
        }

        return Results.Ok(new UpdateHiveTelemetryResponse
        {
            Success = true,
            Message = $"Telemetry updated successfully for Hive {hiveId}",
            Telemetry = telemetry
        });
    }
    else
    {
        return Results.BadRequest(new UpdateHiveTelemetryResponse
        {
            Success = false,
            Message = "No telemetry fields provided to update"
        });
    }
})
.WithName("UpdateHiveTelemetry")
.WithTags("Hives", "Telemetry")
.WithSummary("Оновити телеметрію рою")
.WithDescription("Оновлює телеметрію рою: локацію, висоту, швидкість, стан руху. Всі поля опціональні")
.Accepts<UpdateHiveTelemetryRequest>("application/json")
.Produces<UpdateHiveTelemetryResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

groupBuilder.MapGet("drones", ([FromServices] IDroneRelayService relayService) =>
{
    var drones = relayService.GetSwarm();
    return Results.Ok(drones);
})
.WithName("GetAllDrones")
.WithTags("Drones")
.WithSummary("Отримати всіх дронів")
.WithDescription("Повертає список всіх дронів, зареєстрованих у рої")
.Produces<IReadOnlyCollection<Drone>>(StatusCodes.Status200OK);

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
})
.WithName("UpsertDrone")
.WithTags("Drones")
.WithSummary("Створити або оновити дрона")
.WithDescription("Реєструє нового дрона або оновлює існуючого з вказаним ID, типом та зв'язками")
.Accepts<Drone>("application/json")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status200OK);

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
})
.WithName("BatchCreateDrones")
.WithTags("Drones")
.WithSummary("Масове створення дронів")
.WithDescription("Створює або оновлює кілька дронів одночасно. Повертає статистику: скільки створено, оновлено, не вдалося")
.Accepts<BatchCreateDronesRequest>("application/json")
.Produces<BatchCreateDronesResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

groupBuilder.MapDelete("drones", ([FromServices] IDroneRelayService relayService) =>
{
    var removedCount = relayService.RemoveAllDrones();
    return Results.Ok(new 
    { 
        message = $"All drones removed successfully",
        removedCount = removedCount
    });
})
.WithName("DeleteAllDrones")
.WithTags("Drones")
.WithSummary("Видалити всіх дронів")
.WithDescription("Видаляє всіх дронів з рою, включаючи їх зв'язки та команди")
.Produces(StatusCodes.Status200OK);

groupBuilder.MapDelete("drones/{droneId}", (string droneId, [FromServices] IDroneRelayService relayService) =>
{
    var removed = relayService.RemoveDrone(droneId);
    return removed ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteDrone")
.WithTags("Drones")
.WithSummary("Видалити дрона")
.WithDescription("Видаляє дрона з рою за ID, включаючи всі його зв'язки та команди")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

groupBuilder.MapGet("drones/{droneId}/analysis", (string droneId, [FromQuery] double? minWeight, [FromServices] IDroneRelayService relayService) =>
{
    var analysis = relayService.AnalyzeConnection(droneId, minWeight ?? 0.5);
    return Results.Ok(analysis);
})
.WithName("AnalyzeDroneConnection")
.WithTags("Drones")
.WithSummary("Аналіз зв'язку з дроном")
.WithDescription("Перевіряє, чи можна досягти дрона через mesh-мережу з мінімальною вагою зв'язку. Повертає маршрут та інформацію про зв'язність")
.Produces<DroneConnectionAnalysisResponse>(StatusCodes.Status200OK);

groupBuilder.MapGet("hive/identity", () =>
{
    var hiveId = HiveInMemoryState.GetHiveId();
    return Results.Ok(new { hiveId });
})
.WithName("GetHiveIdentity")
.WithTags("Hives")
.WithSummary("Отримати поточний ID рою")
.WithDescription("Повертає ID рою, який HiveMind використовує для телеметрії та команд")
.Produces(StatusCodes.Status200OK);

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
})
.WithName("UpdateHiveIdentity")
.WithTags("Hives")
.WithSummary("Змінити ID рою")
.WithDescription("Змінює ID рою, який HiveMind використовує. Якщо reconnect=true, перепідключається до Communication Control. Рій повинен існувати")
.Accepts<HiveIdentityUpdateRequest>("application/json")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

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
})
.WithName("GetAllHives")
.WithTags("Hives")
.WithSummary("Отримати всі рої")
.WithDescription("Повертає список всіх роїв, зареєстрованих у системі")
.Produces(StatusCodes.Status200OK);

groupBuilder.MapGet("hives/{hiveId}", (string hiveId, [FromServices] IHiveService hiveService) =>
{
    var hive = hiveService.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found" });
    }
    return Results.Ok(hive);
})
.WithName("GetHive")
.WithTags("Hives")
.WithSummary("Отримати рій за ID")
.WithDescription("Повертає інформацію про конкретний рій за його ID")
.Produces<Hive>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

groupBuilder.MapDelete("hives", ([FromServices] IHiveService hiveService) =>
{
    var deletedCount = hiveService.DeleteAllHives();
    return Results.Ok(new 
    { 
        message = $"All hives deleted successfully",
        deletedCount = deletedCount
    });
})
.WithName("DeleteAllHives")
.WithTags("Hives")
.WithSummary("Видалити всі рої")
.WithDescription("Видаляє всі рої та всіх дронів з них")
.Produces(StatusCodes.Status200OK);

groupBuilder.MapDelete("hives/{hiveId}", (string hiveId, [FromServices] IHiveService hiveService) =>
{
    var deleted = hiveService.DeleteHive(hiveId);
    return deleted ? Results.NoContent() : Results.NotFound(new { message = $"Hive {hiveId} not found" });
})
.WithName("DeleteHive")
.WithTags("Hives")
.WithSummary("Видалити рій")
.WithDescription("Видаляє рій за ID та всіх дронів з нього")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

groupBuilder.MapGet("hives/{hiveId}/drones", (string hiveId, [FromServices] IDroneCommandService commandService) =>
{
    var drones = commandService.GetHiveDrones(hiveId);
    return Results.Ok(new { hiveId, drones, count = drones.Count });
})
.WithName("GetHiveDrones")
.WithTags("Hives", "Drones")
.WithSummary("Отримати дронів рою")
.WithDescription("Повертає список всіх дронів, які належать до конкретного рою")
.Produces(StatusCodes.Status200OK);

groupBuilder.MapPost("hives/{hiveId}/drones/{droneId}/join", (string hiveId, string droneId, [FromBody] DroneJoinRequest request, [FromServices] IDroneCommandService commandService) =>
{
    var joinRequest = request ?? new DroneJoinRequest { DroneId = droneId };
    if (string.IsNullOrWhiteSpace(joinRequest.DroneId))
    {
        joinRequest.DroneId = droneId;
    }
    
    var response = commandService.JoinDrone(hiveId, joinRequest);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
})
.WithName("JoinDroneToHive")
.WithTags("Hives", "Drones")
.WithSummary("Приєднати дрона до рою")
.WithDescription("Додає дрона до рою. Дрон повинен бути зареєстрований і не може бути в іншому рої одночасно")
.Accepts<DroneJoinRequest>("application/json")
.Produces<DroneJoinResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

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
})
.WithName("BatchJoinDronesToHive")
.WithTags("Hives", "Drones")
.WithSummary("Масове приєднання дронів до рою")
.WithDescription("Додає кілька дронів до рою одночасно. Повертає статистику: скільки приєднано, вже в рої, не вдалося")
.Accepts<BatchJoinDronesRequest>("application/json")
.Produces<BatchJoinDronesResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

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
})
.WithName("RemoveDroneFromHive")
.WithTags("Hives", "Drones")
.WithSummary("Видалити дрона з рою")
.WithDescription("Видаляє дрона з рою, дозволяючи йому приєднатися до іншого рою. Очищає чергу команд дрона")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

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
})
.WithName("BatchRemoveDronesFromHive")
.WithTags("Hives", "Drones")
.WithSummary("Масове видалення дронів з рою")
.WithDescription("Видаляє кілька дронів з рою одночасно. Повертає статистику: скільки видалено, не в рої, не вдалося")
.Accepts<BatchRemoveDronesRequest>("application/json")
.Produces<BatchRemoveDronesResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

groupBuilder.MapGet("hives/{hiveId}/drones/{droneId}/connected", (string hiveId, string droneId, [FromServices] IDroneCommandService commandService) =>
{
    var connectedDrones = commandService.GetConnectedDrones(hiveId, droneId);
    return Results.Ok(new { hiveId, droneId, connectedDrones, count = connectedDrones.Count });
})
.WithName("GetConnectedDrones")
.WithTags("Hives", "Drones")
.WithSummary("Отримати зв'язаних дронів")
.WithDescription("Повертає список дронів, які мають прямий зв'язок з вказаним дроном в межах того ж рою")
.Produces(StatusCodes.Status200OK);

// Topology management endpoints
groupBuilder.MapPost("hives/{hiveId}/topology/rebuild", (string hiveId, [FromBody] TopologyRebuildRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    request.HiveId = hiveId;

    var response = relayService.RebuildTopology(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
})
.WithName("RebuildTopology")
.WithTags("Topology", "Hives")
.WithSummary("Перебудувати топологію рою")
.WithDescription("Перебудовує топологію зв'язків між дронами в рої. Підтримує типи: mesh (повна мережа), star (зірка), dual_star (подвійна зірка)")
.Accepts<TopologyRebuildRequest>("application/json")
.Produces<TopologyRebuildResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

groupBuilder.MapPost("hives/{hiveId}/topology/connect-hivemind", (string hiveId, [FromBody] ConnectToHiveMindRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    request.HiveId = hiveId;

    var response = relayService.ConnectToHiveMind(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
})
.WithName("ConnectHiveToHiveMind")
.WithTags("Topology", "Hives")
.WithSummary("Підключити рій до HiveMind")
.WithDescription("Реєструє relay дрони як точки входу між роєм та HiveMind. Не змінює граф зв'язків, лише зберігає інформацію про entry relays")
.Accepts<ConnectToHiveMindRequest>("application/json")
.Produces<TopologyRebuildResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

groupBuilder.MapGet("hives/{hiveId}/topology/connectivity", (string hiveId, [FromServices] IDroneRelayService relayService) =>
{
    var response = relayService.AnalyzeSwarmConnectivity(hiveId);
    return Results.Ok(response);
})
.WithName("AnalyzeSwarmConnectivity")
.WithTags("Topology", "Hives")
.WithSummary("Аналіз зв'язності рою")
.WithDescription("Аналізує зв'язність рою: чи всі дрони з'єднані, скільки компонентів, ізольовані групи, статистика зв'язків")
.Produces<SwarmConnectivityResponse>(StatusCodes.Status200OK);

groupBuilder.MapPost("drones/connections/degrade", ([FromBody] DegradeConnectionRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = relayService.DegradeConnection(request);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
})
.WithName("DegradeConnection")
.WithTags("Connections", "Drones")
.WithSummary("Деградувати зв'язок між дронами")
.WithDescription("Змінює вагу зв'язку між двома дронами (0.0-1.0). Якщо вага 0 або менше, зв'язок видаляється. Оновлює обидва напрямки (бідирекціонально)")
.Accepts<DegradeConnectionRequest>("application/json")
.Produces<DegradeConnectionResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

groupBuilder.MapPost("drones/connections/batch-degrade", ([FromBody] BatchDegradeConnectionsRequest request, [FromServices] IDroneRelayService relayService) =>
{
    if (request == null)
    {
        return Results.BadRequest(new { message = "Request cannot be null" });
    }

    var response = relayService.BatchDegradeConnections(request);
    return Results.Ok(response);
})
.WithName("BatchDegradeConnections")
.WithTags("Connections", "Drones")
.WithSummary("Масове деградування зв'язків")
.WithDescription("Деградує кілька зв'язків між дронами одночасно. Повертає статистику успішних та невдалих операцій")
.Accepts<BatchDegradeConnectionsRequest>("application/json")
.Produces<BatchDegradeConnectionsResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

groupBuilder.MapPost("hives/{hiveId}/drones/{droneId}/commands/mesh", (string hiveId, string droneId, [FromBody] DroneCommand command, [FromQuery] double? minWeight, [FromServices] IDroneRelayService relayService) =>
{
    if (command == null)
    {
        return Results.BadRequest(new { message = "Command cannot be null" });
    }
    
    var hive = HiveInMemoryState.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found." });
    }
    
    var drone = HiveInMemoryState.GetDrone(droneId);
    if (drone == null)
    {
        return Results.NotFound(new { message = $"Drone {droneId} not found. Cannot send command to non-existent drone." });
    }
    
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
    
    if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
    {
        command.CommandPayload = null;
    }
    
    var meshResponse = relayService.SendCommandViaMesh(droneId, command, minWeight ?? 0.5);
    
    if (!meshResponse.Success)
    {
        return Results.BadRequest(meshResponse);
    }
    
    return Results.Ok(meshResponse);
})
.WithName("SendMeshCommand")
.WithTags("Commands", "Drones")
.WithSummary("Відправити команду дрону через mesh-мережу")
.WithDescription("Відправляє команду дрону через mesh-мережу з використанням relay дронів. Знаходить найкоротший маршрут з мінімальною вагою зв'язку")
.Accepts<DroneCommand>("application/json")
.Produces<MeshCommandResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

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
})
.WithName("GetDroneCommands")
.WithTags("Commands", "Drones")
.WithSummary("Отримати всі команди дрона")
.WithDescription("Повертає всі команди, призначені для конкретного дрона, з нумерацією та інформацією про рій")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status204NoContent);

groupBuilder.MapPost("drones/{droneId}/commands", (string droneId, [FromBody] DroneCommand command, [FromServices] IDroneCommandService commandService) =>
{
    if (command == null)
    {
        return Results.BadRequest(new { message = "Command cannot be null" });
    }
    
    var drone = HiveInMemoryState.GetDrone(droneId);
    if (drone == null)
    {
        return Results.NotFound(new { message = $"Drone {droneId} not found. Cannot send command to non-existent drone." });
    }
    
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
    
    command.TargetDroneId = droneId;
    
    if (command.Timestamp == default)
    {
        command.Timestamp = DateTime.UtcNow;
    }
    
    command.CommandId = Guid.NewGuid();
    
    if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
    {
        command.CommandPayload = null;
    }
    else if (command.CommandType == DroneCommandType.Move)
    {
        if (command.CommandPayload == null)
        {
            return Results.BadRequest(new { message = "CommandPayload is required for Move command" });
        }
        
        try
        {
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(command.CommandPayload);
            var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = payloadDoc.RootElement;
            
            var hasLat = root.TryGetProperty("lat", out var latElement) || root.TryGetProperty("Lat", out latElement);
            var hasLon = root.TryGetProperty("lon", out var lonElement) || root.TryGetProperty("Lon", out lonElement);
            var hasHeight = root.TryGetProperty("height", out var heightElement) || root.TryGetProperty("Height", out heightElement);
            var hasSpeed = root.TryGetProperty("speed", out var speedElement) || root.TryGetProperty("Speed", out speedElement);
            
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
            
            var movePayload = System.Text.Json.JsonSerializer.Deserialize<MoveDroneCommandPayload>(payloadJson);
            if (movePayload == null)
            {
                return Results.BadRequest(new { message = "Invalid Move command payload. Failed to deserialize payload." });
            }
            
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
})
.WithName("SendDroneCommand")
.WithTags("Commands", "Drones")
.WithSummary("Відправити команду дрону")
.WithDescription("Відправляє команду безпосередньо дрону (тільки для дронів, які не в рої). Для Move команди потрібні: lat, lon, height, speed")
.Accepts<DroneCommand>("application/json")
.Produces<DroneCommand>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

groupBuilder.MapPost("hives/{hiveId}/commands", (string hiveId, [FromBody] DroneCommand command, [FromServices] IDroneCommandService commandService) =>
{
    if (command == null)
    {
        return Results.BadRequest(new { message = "Command cannot be null" });
    }
    
    var hive = HiveInMemoryState.GetHive(hiveId);
    if (hive == null)
    {
        return Results.NotFound(new { message = $"Hive {hiveId} not found." });
    }
    
    if (command.Timestamp == default)
    {
        command.Timestamp = DateTime.UtcNow;
    }
    
    command.CommandId = Guid.NewGuid();
    
    if (command.CommandType == DroneCommandType.Stop || command.CommandType == DroneCommandType.GetTelemetry)
    {
        command.CommandPayload = null;
    }
    else if (command.CommandType == DroneCommandType.Move)
    {
        if (command.CommandPayload == null)
        {
            return Results.BadRequest(new { message = "CommandPayload is required for Move command" });
        }
        
        try
        {
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(command.CommandPayload);
            var payloadDoc = System.Text.Json.JsonDocument.Parse(payloadJson);
            var root = payloadDoc.RootElement;
            
            var hasLat = root.TryGetProperty("lat", out var latElement) || root.TryGetProperty("Lat", out latElement);
            var hasLon = root.TryGetProperty("lon", out var lonElement) || root.TryGetProperty("Lon", out lonElement);
            var hasHeight = root.TryGetProperty("height", out var heightElement) || root.TryGetProperty("Height", out heightElement);
            var hasSpeed = root.TryGetProperty("speed", out var speedElement) || root.TryGetProperty("Speed", out speedElement);
            
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
            
            var movePayload = System.Text.Json.JsonSerializer.Deserialize<MoveDroneCommandPayload>(payloadJson);
            if (movePayload == null)
            {
                return Results.BadRequest(new { message = "Invalid Move command payload. Failed to deserialize payload." });
            }
            
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
})
.WithName("SendHiveCommand")
.WithTags("Commands", "Hives")
.WithSummary("Відправити команду всьому рою")
.WithDescription("Відправляє команду всім дронам у рої. Індивідуальні команди кожного дрона очищаються та замінюються новою командою рою. Для Move команди потрібні: lat, lon, height, speed")
.Accepts<DroneCommand>("application/json")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound);

app.Run();
