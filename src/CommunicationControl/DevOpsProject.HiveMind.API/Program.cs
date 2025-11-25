using Asp.Versioning;
using Asp.Versioning.Builder;
using DevOpsProject.HiveMind.API.DI;
using DevOpsProject.HiveMind.API.Middleware;
using DevOpsProject.HiveMind.Logic.Patterns.Factory.Interfaces;
using DevOpsProject.HiveMind.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using DevOpsProject.Shared.Models;
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

groupBuilder.MapGet("ping", (IOptionsSnapshot<HiveCommunicationConfig> config) =>
{
    return Results.Ok(new
    {
        Timestamp = DateTime.Now,
        ID = config.Value.HiveID
    });
});

groupBuilder.MapPost("command", async (HiveMindCommand command, [FromServices]ICommandHandlerFactory factory) =>
{
    var handler = factory.GetHandler(command);
    await handler.HandleAsync(command);
    return Results.Ok();
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

groupBuilder.MapDelete("drones/{droneId:guid}", (Guid droneId, [FromServices] IDroneRelayService relayService) =>
{
    var removed = relayService.RemoveDrone(droneId);
    return removed ? Results.NoContent() : Results.NotFound();
});

groupBuilder.MapGet("drones/{droneId:guid}/analysis", (Guid droneId, [FromQuery] double? minWeight, [FromServices] IDroneRelayService relayService) =>
{
    var analysis = relayService.AnalyzeConnection(droneId, minWeight ?? 0.5);
    return Results.Ok(analysis);
});

app.Run();
