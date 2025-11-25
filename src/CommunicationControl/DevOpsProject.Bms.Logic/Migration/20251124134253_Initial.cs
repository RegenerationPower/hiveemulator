using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DevOpsProject.Bms.Logic.Migration
{
    /// <inheritdoc />
    public partial class Initial : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EwZoneHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    CenterLatitude = table.Column<double>(type: "double precision", nullable: false),
                    CenterLongitude = table.Column<double>(type: "double precision", nullable: false),
                    RadiusKm = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EwZoneHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EwZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CenterLatitude = table.Column<double>(type: "double precision", nullable: false),
                    CenterLongitude = table.Column<double>(type: "double precision", nullable: false),
                    RadiusKm = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EwZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HiveRepositionSuggestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceHiveId = table.Column<string>(type: "text", nullable: true),
                    OtherHiveId = table.Column<string>(type: "text", nullable: true),
                    SourceLatitude = table.Column<float>(type: "real", nullable: false),
                    SourceLongitude = table.Column<float>(type: "real", nullable: false),
                    OtherLatitude = table.Column<float>(type: "real", nullable: false),
                    OtherLongitude = table.Column<float>(type: "real", nullable: false),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    SuggestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HiveRepositionSuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HiveStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HiveId = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<float>(type: "real", nullable: false),
                    Longitude = table.Column<float>(type: "real", nullable: false),
                    Height = table.Column<float>(type: "real", nullable: false),
                    Speed = table.Column<float>(type: "real", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    IsInEwZone = table.Column<bool>(type: "boolean", nullable: false),
                    LastTelemetryTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HiveStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HiveId = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<float>(type: "real", nullable: false),
                    Longitude = table.Column<float>(type: "real", nullable: false),
                    Height = table.Column<float>(type: "real", nullable: false),
                    Speed = table.Column<float>(type: "real", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    IsInEwZone = table.Column<bool>(type: "boolean", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EwZoneHistory_ZoneId",
                table: "EwZoneHistory",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_HiveRepositionSuggestions_SourceHiveId_OtherHiveId_IsConsum~",
                table: "HiveRepositionSuggestions",
                columns: new[] { "SourceHiveId", "OtherHiveId", "IsConsumed" });

            migrationBuilder.CreateIndex(
                name: "IX_HiveStatuses_HiveId",
                table: "HiveStatuses",
                column: "HiveId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryHistory_HiveId_TimestampUtc",
                table: "TelemetryHistory",
                columns: new[] { "HiveId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EwZoneHistory");

            migrationBuilder.DropTable(
                name: "EwZones");

            migrationBuilder.DropTable(
                name: "HiveRepositionSuggestions");

            migrationBuilder.DropTable(
                name: "HiveStatuses");

            migrationBuilder.DropTable(
                name: "TelemetryHistory");
        }
    }
}
