using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DevOpsProject.Bms.Logic.Data;

public class BmsDbContext : DbContext
{
    public DbSet<HiveStatus>       HiveStatuses     { get; set; }
    public DbSet<EwZone>           EwZones          { get; set; }
    public DbSet<EwZoneHistory>    EwZoneHistory    { get; set; }
    public DbSet<TelemetryHistory> TelemetryHistory { get; set; }
    public DbSet<HiveRepositionSuggestion> HiveRepositionSuggestions { get; set; }

    public BmsDbContext(DbContextOptions<BmsDbContext> options): base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HiveStatus>()
            .HasIndex(x => x.HiveId)
            .IsUnique();
        
        modelBuilder.Entity<EwZone>()
            .HasKey(x => x.Id);
        
        modelBuilder.Entity<EwZoneHistory>()
            .HasIndex(x => x.ZoneId);
        
        modelBuilder.Entity<TelemetryHistory>()
            .HasIndex(x => new { x.HiveId, x.TimestampUtc });
        
        modelBuilder.Entity<HiveRepositionSuggestion>()
            .HasIndex(x => new { x.SourceHiveId, x.OtherHiveId, x.IsConsumed });

    }
}