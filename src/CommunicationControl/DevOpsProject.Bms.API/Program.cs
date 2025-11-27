using DevOpsProject.Bms.API.Background;
using DevOpsProject.Bms.Logic.Data;
using DevOpsProject.Bms.Logic.Options;
using DevOpsProject.Bms.Logic.Services;
using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Controllers, Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Battlefield Management System",
        Version = "v1"
    });
});

// DB (приклад для SQL Server, можна замінити на PostgreSQL)
builder.Services.AddDbContext<BmsDbContext>(option => option.UseNpgsql(
    builder.Configuration.GetConnectionString("BmsDb")));

// Redis
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
var redisOptions = builder.Configuration.GetSection("Redis").Get<RedisOptions>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisOptions.ConnectionString));

// BMS options
builder.Services.Configure<BmsMonitoringOptions>(builder.Configuration.GetSection("BmsMonitoring"));

// Logic services
builder.Services.AddScoped<ICurrentStatusService, CurrentStatusService>();
builder.Services.AddScoped<IEwZoneService, EwZoneService>();
builder.Services.AddScoped<ITelemetryProcessor, TelemetryProcessor>();

// Background listener (BMS-1)
builder.Services.AddHostedService<TelemetryListenerBackgroundService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BMS v1");
});

app.MapControllers();
ApplyMigration();

app.Run();

void ApplyMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BmsDbContext>();

        if (db.Database.GetPendingMigrations().Any())
        {
            db.Database.Migrate();
        }
    }
}