using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure.Migrations
{
    public partial class AddThermalPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thermal Zones
            migrationBuilder.CreateTable(
                name: "thermal_zones",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    DesignHeatLoadKw = table.Column<double>(type: "double precision", nullable: false),
                    ThermalMassKjPerK = table.Column<double>(type: "double precision", nullable: false),
                    HeatLossCoefficientWPerK = table.Column<double>(type: "double precision", nullable: false),
                    TargetTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    DemandStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActiveThermalPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thermal_zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_thermal_zones_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Thermal Zone Rooms
            migrationBuilder.CreateTable(
                name: "thermal_zone_rooms",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    DesignHeatLoadKw = table.Column<double>(type: "double precision", nullable: false),
                    MinimumTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaximumTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thermal_zone_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_thermal_zone_rooms_thermal_zones_ZoneId",
                        column: x => x.ZoneId,
                        principalSchema: "engineering",
                        principalTable: "thermal_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Heat Sources
            migrationBuilder.CreateTable(
                name: "heat_sources",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LifecycleStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    MinimumPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    MaximumPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    MinimumRuntimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinimumOffTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartupDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ShutdownDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Efficiency = table.Column<double>(type: "double precision", nullable: false),
                    NominalCop = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    CurrentOutputKw = table.Column<double>(type: "double precision", nullable: true),
                    CurrentOutputPct = table.Column<double>(type: "double precision", nullable: true),
                    RuntimeState = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastStoppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CooldownUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DefrostState = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_heat_sources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_heat_sources_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Hydraulic Circuits
            migrationBuilder.CreateTable(
                name: "hydraulic_circuits",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CircuitType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LifecycleStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    DesignFlowM3h = table.Column<double>(type: "double precision", nullable: false),
                    MinimumFlowM3h = table.Column<double>(type: "double precision", nullable: false),
                    MaximumFlowM3h = table.Column<double>(type: "double precision", nullable: false),
                    MinimumSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    CurrentSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    CurrentReturnTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    CurrentFlowM3h = table.Column<double>(type: "double precision", nullable: true),
                    TargetSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    PumpBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MixingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hydraulic_circuits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hydraulic_circuits_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Mixing Units
            migrationBuilder.CreateTable(
                name: "mixing_units",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CircuitId = table.Column<Guid>(type: "uuid", nullable: true),
                    LifecycleStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValveBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplySensorBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnSensorBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MinimumValvePositionPct = table.Column<double>(type: "double precision", nullable: false),
                    MaximumValvePositionPct = table.Column<double>(type: "double precision", nullable: false),
                    TargetSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    CurrentSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    CurrentValvePositionPct = table.Column<double>(type: "double precision", nullable: true),
                    ControlMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mixing_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mixing_units_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Circulation Pumps
            migrationBuilder.CreateTable(
                name: "circulation_pumps",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CircuitId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinimumSpeedPct = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSpeedPct = table.Column<double>(type: "double precision", nullable: false),
                    CurrentSpeedPct = table.Column<double>(type: "double precision", nullable: false),
                    TargetSpeedPct = table.Column<double>(type: "double precision", nullable: false),
                    CurrentFlowM3h = table.Column<double>(type: "double precision", nullable: true),
                    DryRunProtectionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastStoppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RuntimeHours = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_circulation_pumps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_circulation_pumps_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Buffer Tanks
            migrationBuilder.CreateTable(
                name: "buffer_tanks",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VolumeLiters = table.Column<double>(type: "double precision", nullable: false),
                    MinimumTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaximumTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    TargetTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    CurrentTopTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    CurrentBottomTemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    EstimatedStoredEnergyKwh = table.Column<double>(type: "double precision", nullable: false),
                    StateOfChargePct = table.Column<double>(type: "double precision", nullable: false),
                    ChargeStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SensorQuality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buffer_tanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buffer_tanks_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Domestic Hot Water Systems
            migrationBuilder.CreateTable(
                name: "domestic_hot_water_systems",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageVolumeLiters = table.Column<double>(type: "double precision", nullable: false),
                    TargetTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MinimumTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaximumTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    CurrentTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    LegionellaProtectionTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    LegionellaCycleEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HeatingCircuitId = table.Column<Guid>(type: "uuid", nullable: true),
                    CirculationPumpBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastSatisfiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domestic_hot_water_systems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_domestic_hot_water_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Weather Compensation Curves
            migrationBuilder.CreateTable(
                name: "weather_compensation_curves",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slope = table.Column<double>(type: "double precision", nullable: false),
                    ParallelShiftC = table.Column<double>(type: "double precision", nullable: false),
                    ReferenceOutdoorTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MinimumSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSupplyTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    RoomInfluenceFactor = table.Column<double>(type: "double precision", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_compensation_curves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weather_compensation_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Thermal Demands
            migrationBuilder.CreateTable(
                name: "thermal_demands",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClimatePlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EngineeringSubPlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ThermalZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    TargetTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    DeviationC = table.Column<double>(type: "double precision", nullable: false),
                    RequiredHeatingPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thermal_demands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_thermal_demands_thermal_zones_ThermalZoneId",
                        column: x => x.ThermalZoneId,
                        principalSchema: "engineering",
                        principalTable: "thermal_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex("IX_thermal_zones_EngineeringSystemId", "thermal_zones", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_thermal_zones_BuildingId", "thermal_zones", "BuildingId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_thermal_zone_rooms_ZoneId", "thermal_zone_rooms", "ZoneId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_thermal_zone_rooms_RoomId", "thermal_zone_rooms", "RoomId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_heat_sources_EngineeringSystemId", "heat_sources", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_hydraulic_circuits_EngineeringSystemId", "hydraulic_circuits", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_hydraulic_circuits_CircuitType", "hydraulic_circuits", "CircuitType", schema: "engineering");
            migrationBuilder.CreateIndex("IX_mixing_units_CircuitId", "mixing_units", "CircuitId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_circulation_pumps_CircuitId", "circulation_pumps", "CircuitId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_buffer_tanks_EngineeringSystemId", "buffer_tanks", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_domestic_hot_water_EngineeringSystemId", "domestic_hot_water_systems", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_weather_compensation_EngineeringSystemId", "weather_compensation_curves", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_thermal_demands_ThermalZoneId", "thermal_demands", "ThermalZoneId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_thermal_demands_RoomId", "thermal_demands", "RoomId", schema: "engineering");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "thermal_demands", schema: "engineering");
            migrationBuilder.DropTable(name: "weather_compensation_curves", schema: "engineering");
            migrationBuilder.DropTable(name: "domestic_hot_water_systems", schema: "engineering");
            migrationBuilder.DropTable(name: "buffer_tanks", schema: "engineering");
            migrationBuilder.DropTable(name: "circulation_pumps", schema: "engineering");
            migrationBuilder.DropTable(name: "mixing_units", schema: "engineering");
            migrationBuilder.DropTable(name: "hydraulic_circuits", schema: "engineering");
            migrationBuilder.DropTable(name: "heat_sources", schema: "engineering");
            migrationBuilder.DropTable(name: "thermal_zone_rooms", schema: "engineering");
            migrationBuilder.DropTable(name: "thermal_zones", schema: "engineering");
        }
    }
}
