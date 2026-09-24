using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure.Migrations
{
    [DbContext(typeof(EngineeringSystemsDbContext))]
    [Migration("20260922090000_AlignEngineeringSystemSchema")]
    public partial class AlignEngineeringSystemSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Lifecycle", table: "engineering_systems", schema: "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active");
            migrationBuilder.AddColumn<string>(name: "LegacyStatus", table: "engineering_systems", schema: "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active");
            migrationBuilder.AddColumn<string>(name: "OperationalStatus", table: "engineering_systems", schema: "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Unknown");
            migrationBuilder.AddColumn<DateTimeOffset>(name: "CreatedAt", table: "engineering_systems", schema: "engineering", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
            migrationBuilder.AddColumn<DateTimeOffset>(name: "UpdatedAt", table: "engineering_systems", schema: "engineering", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
            migrationBuilder.AddColumn<long>(name: "Version", table: "engineering_capabilities", schema: "engineering", type: "bigint", nullable: false, defaultValue: 1L);
            migrationBuilder.AddColumn<long>(name: "Version", table: "engineering_resources", schema: "engineering", type: "bigint", nullable: false, defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "ventilation_configurations",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DesignSupplyAirflow = table.Column<double>(type: "double precision", nullable: false),
                    DesignExhaustAirflow = table.Column<double>(type: "double precision", nullable: false),
                    MinimumSupplyAirflow = table.Column<double>(type: "double precision", nullable: false),
                    MinimumExhaustAirflow = table.Column<double>(type: "double precision", nullable: false),
                    MaximumImbalancePct = table.Column<double>(type: "double precision", nullable: false),
                    HasHeatRecovery = table.Column<bool>(type: "boolean", nullable: false),
                    HasSupplyHeater = table.Column<bool>(type: "boolean", nullable: false),
                    HasSupplyAirTemperatureSensor = table.Column<bool>(type: "boolean", nullable: false),
                    HasOutdoorTemperatureSensor = table.Column<bool>(type: "boolean", nullable: false),
                    FrostProtectionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FrostProtectionTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MinimumSupplyAirTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSupplyAirTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DefaultHeatRecoveryEfficiency = table.Column<double>(type: "double precision", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventilation_configurations", x => x.Id);
                    table.ForeignKey(name: "FK_ventilation_configurations_engineering_systems_EngineeringSystemId", column: x => x.EngineeringSystemId, principalSchema: "engineering", principalTable: "engineering_systems", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thermal_configurations",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignSupplyTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DesignReturnTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MinimumSupplyTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MaximumSupplyTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DesignHeatLoad = table.Column<double>(type: "double precision", nullable: false),
                    SystemThermalMass = table.Column<double>(type: "double precision", nullable: false),
                    WeatherCompensationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WeatherCompensationMinOutdoor = table.Column<double>(type: "double precision", nullable: false),
                    WeatherCompensationMaxOutdoor = table.Column<double>(type: "double precision", nullable: false),
                    WeatherCompensationMinSupply = table.Column<double>(type: "double precision", nullable: false),
                    WeatherCompensationMaxSupply = table.Column<double>(type: "double precision", nullable: false),
                    WeatherCompensationSlope = table.Column<double>(type: "double precision", nullable: false),
                    WeatherCompensationParallelShift = table.Column<double>(type: "double precision", nullable: false),
                    FreezeProtectionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FreezeProtectionTemperature = table.Column<double>(type: "double precision", nullable: false),
                    FreezeProtectionSupplyTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MaximumReturnTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MaximumTempRiseRate = table.Column<double>(type: "double precision", nullable: false),
                    HasBufferTank = table.Column<bool>(type: "boolean", nullable: false),
                    BufferTankVolume = table.Column<double>(type: "double precision", nullable: false),
                    HasDHW = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_thermal_configurations", x => x.Id);
                    table.ForeignKey(name: "FK_thermal_configurations_engineering_systems_EngineeringSystemId", column: x => x.EngineeringSystemId, principalSchema: "engineering", principalTable: "engineering_systems", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_ventilation_configurations_EngineeringSystemId", table: "ventilation_configurations", schema: "engineering", column: "EngineeringSystemId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_thermal_configurations_EngineeringSystemId", table: "thermal_configurations", schema: "engineering", column: "EngineeringSystemId", unique: true);

            migrationBuilder.AddColumn<Guid>(name: "StrategyStrategyId", table: "command_plans", schema: "engineering", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "RequestedEffect", table: "command_plans", schema: "engineering", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "ResourceAllocationId", table: "command_plans", schema: "engineering", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NeedId", table: "command_plans", schema: "engineering", type: "character varying(50)", maxLength: 50, nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "BuildingId", table: "command_plans", schema: "engineering", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "RoomId", table: "command_plans", schema: "engineering", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CorrelationId", table: "command_plans", schema: "engineering", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CausationId", table: "command_plans", schema: "engineering", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "IdempotencyKey", table: "command_plans", schema: "engineering", type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "PlannedAt", table: "command_plans", schema: "engineering", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "StartedAt", table: "command_plans", schema: "engineering", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "FailedAt", table: "command_plans", schema: "engineering", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "CancelledAt", table: "command_plans", schema: "engineering", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "ExpiresAt", table: "command_plans", schema: "engineering", type: "timestamp with time zone", nullable: true);

            migrationBuilder.RenameColumn(name: "Order", table: "command_plan_steps", schema: "engineering", newName: "Sequence");
            migrationBuilder.AddColumn<string>(name: "DeviceRole", table: "command_plan_steps", schema: "engineering", type: "character varying(50)", maxLength: 50, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ExecutionMode", table: "command_plan_steps", schema: "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Sequential");
            migrationBuilder.AddColumn<bool>(name: "Required", table: "command_plan_steps", schema: "engineering", type: "boolean", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "Status", table: "command_plan_steps", schema: "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending");
            migrationBuilder.AddColumn<DateTimeOffset>(name: "StartedAt", table: "command_plan_steps", schema: "engineering", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "CompletedAt", table: "command_plan_steps", schema: "engineering", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "FailureCode", table: "command_plan_steps", schema: "engineering", type: "character varying(100)", maxLength: 100, nullable: true);

            migrationBuilder.CreateIndex(name: "IX_command_plans_NeedId", table: "command_plans", schema: "engineering", column: "NeedId");
            migrationBuilder.CreateIndex(name: "IX_command_plans_IdempotencyKey", table: "command_plans", schema: "engineering", column: "IdempotencyKey", unique: true, filter: "\"IdempotencyKey\" IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_command_plan_steps_DeviceRole", table: "command_plan_steps", schema: "engineering", column: "DeviceRole");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("thermal_configurations", "engineering");
            migrationBuilder.DropTable("ventilation_configurations", "engineering");

            migrationBuilder.DropColumn("Lifecycle", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("LegacyStatus", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("OperationalStatus", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("CreatedAt", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("UpdatedAt", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("Version", "engineering_capabilities", "engineering");
            migrationBuilder.DropColumn("Version", "engineering_resources", "engineering");

            migrationBuilder.DropIndex("IX_command_plans_NeedId", "command_plans", "engineering");
            migrationBuilder.DropIndex("IX_command_plans_IdempotencyKey", "command_plans", "engineering");
            migrationBuilder.DropIndex("IX_command_plan_steps_DeviceRole", "command_plan_steps", "engineering");

            migrationBuilder.DropColumn("StrategyStrategyId", "command_plans", "engineering");
            migrationBuilder.DropColumn("RequestedEffect", "command_plans", "engineering");
            migrationBuilder.DropColumn("ResourceAllocationId", "command_plans", "engineering");
            migrationBuilder.DropColumn("NeedId", "command_plans", "engineering");
            migrationBuilder.DropColumn("BuildingId", "command_plans", "engineering");
            migrationBuilder.DropColumn("RoomId", "command_plans", "engineering");
            migrationBuilder.DropColumn("CorrelationId", "command_plans", "engineering");
            migrationBuilder.DropColumn("CausationId", "command_plans", "engineering");
            migrationBuilder.DropColumn("IdempotencyKey", "command_plans", "engineering");
            migrationBuilder.DropColumn("PlannedAt", "command_plans", "engineering");
            migrationBuilder.DropColumn("StartedAt", "command_plans", "engineering");
            migrationBuilder.DropColumn("FailedAt", "command_plans", "engineering");
            migrationBuilder.DropColumn("CancelledAt", "command_plans", "engineering");
            migrationBuilder.DropColumn("ExpiresAt", "command_plans", "engineering");

            migrationBuilder.DropColumn("DeviceRole", "command_plan_steps", "engineering");
            migrationBuilder.DropColumn("ExecutionMode", "command_plan_steps", "engineering");
            migrationBuilder.DropColumn("Required", "command_plan_steps", "engineering");
            migrationBuilder.DropColumn("Status", "command_plan_steps", "engineering");
            migrationBuilder.DropColumn("StartedAt", "command_plan_steps", "engineering");
            migrationBuilder.DropColumn("CompletedAt", "command_plan_steps", "engineering");
            migrationBuilder.DropColumn("FailureCode", "command_plan_steps", "engineering");
            migrationBuilder.RenameColumn(name: "Sequence", table: "command_plan_steps", schema: "engineering", newName: "Order");
        }
    }
}
