using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure.Migrations
{
    public partial class AddHumidificationPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Humidification System Configurations
            migrationBuilder.CreateTable(
                name: "humidification_configurations",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DesignCapacityKgH = table.Column<double>(type: "double precision", nullable: false),
                    MaximumCapacityKgH = table.Column<double>(type: "double precision", nullable: false),
                    MinimumCapacityKgH = table.Column<double>(type: "double precision", nullable: false),
                    TargetRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    MaximumRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    MinimumSupplyAirTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSupplyAirTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    HasWaterTreatment = table.Column<bool>(type: "boolean", nullable: false),
                    HasSteamGenerator = table.Column<bool>(type: "boolean", nullable: false),
                    HasUVSterilization = table.Column<bool>(type: "boolean", nullable: false),
                    CondensationProtectionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaximumSurfaceDewPointDeltaC = table.Column<double>(type: "double precision", nullable: false),
                    SanitaryCycleEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FlushIntervalHours = table.Column<int>(type: "integer", nullable: false),
                    DrainIntervalHours = table.Column<int>(type: "integer", nullable: false),
                    SterilizationIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    StandingWaterTimeoutMinutes = table.Column<double>(type: "double precision", nullable: false),
                    LegionellaProtectionTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_humidification_configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_humidification_configurations_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Humidification Zones
            migrationBuilder.CreateTable(
                name: "humidification_zones",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DesignCapacityKgH = table.Column<double>(type: "double precision", nullable: false),
                    MinimumRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    MaximumRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_humidification_zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_humidification_zones_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Humidification Zone Rooms
            migrationBuilder.CreateTable(
                name: "humidification_zone_rooms",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    DesignCapacityKgH = table.Column<double>(type: "double precision", nullable: false),
                    MinimumRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    MaximumRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_humidification_zone_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_humidification_zone_rooms_humidification_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalSchema: "engineering",
                        principalTable: "humidification_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Humidification Demands
            migrationBuilder.CreateTable(
                name: "humidification_demands",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClimatePlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    TargetRhPercent = table.Column<double>(type: "double precision", nullable: false),
                    DewPointC = table.Column<double>(type: "double precision", nullable: false),
                    RequiredCapacityKgH = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_humidification_demands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_humidification_demands_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex("IX_humidification_configurations_EngineeringSystemId", "humidification_configurations", "EngineeringSystemId", schema: "engineering", unique: true);
            migrationBuilder.CreateIndex("IX_humidification_zones_EngineeringSystemId", "humidification_zones", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_humidification_zone_rooms_RoomId", "humidification_zone_rooms", "RoomId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_humidification_demands_EngineeringSystemId", "humidification_demands", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_humidification_demands_RoomId_Status", "humidification_demands", new[] { "RoomId", "Status" }, schema: "engineering");

            // Add Lifecycle, CreatedAt, UpdatedAt columns to engineering_systems (if missing from initial migration)
            // Note: These were added in a prior migration if not present; this is a no-op if columns exist.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "humidification_demands", schema: "engineering");
            migrationBuilder.DropTable(name: "humidification_zone_rooms", schema: "engineering");
            migrationBuilder.DropTable(name: "humidification_configurations", schema: "engineering");
            migrationBuilder.DropTable(name: "humidification_zones", schema: "engineering");
        }
    }
}
