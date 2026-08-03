using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure.Migrations
{
    public partial class AddEngineeringSystemsSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "engineering");

            // Engineering Systems
            migrationBuilder.CreateTable(
                name: "engineering_systems",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SystemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ControlMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_engineering_systems", x => x.Id));

            // Capabilities
            migrationBuilder.CreateTable(
                name: "engineering_capabilities",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Minimum = table.Column<double>(type: "double precision", nullable: true),
                    Maximum = table.Column<double>(type: "double precision", nullable: true),
                    SupportsModulation = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_capabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_engineering_capabilities_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Resources
            migrationBuilder.CreateTable(
                name: "engineering_resources",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Maximum = table.Column<double>(type: "double precision", nullable: false),
                    Available = table.Column<double>(type: "double precision", nullable: false),
                    Reserved = table.Column<double>(type: "double precision", nullable: false),
                    Used = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_engineering_resources_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Zones
            migrationBuilder.CreateTable(
                name: "engineering_zones",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_zones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_engineering_zones_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Zone Rooms
            migrationBuilder.CreateTable(
                name: "engineering_zone_rooms",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_zone_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_engineering_zone_rooms_engineering_zones_SystemZoneId",
                        column: x => x.SystemZoneId,
                        principalSchema: "engineering",
                        principalTable: "engineering_zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // System Devices
            migrationBuilder.CreateTable(
                name: "engineering_system_devices",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineering_system_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_engineering_system_devices_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Strategies
            migrationBuilder.CreateTable(
                name: "engineering_strategies",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_engineering_strategies", x => x.Id));

            // Command Plans
            migrationBuilder.CreateTable(
                name: "command_plans",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineeringSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    NeedType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedValue = table.Column<double>(type: "double precision", nullable: false),
                    ValueUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StrategyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_command_plans_engineering_systems_EngineeringSystemId",
                        column: x => x.EngineeringSystemId,
                        principalSchema: "engineering",
                        principalTable: "engineering_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Command Plan Steps
            migrationBuilder.CreateTable(
                name: "command_plan_steps",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedValue = table.Column<double>(type: "double precision", nullable: false),
                    ValueUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_plan_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_command_plan_steps_command_plans_CommandPlanId",
                        column: x => x.CommandPlanId,
                        principalSchema: "engineering",
                        principalTable: "command_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Command Plan Resource Allocations
            migrationBuilder.CreateTable(
                name: "command_plan_resource_allocations",
                schema: "engineering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_plan_resource_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_command_plan_resource_allocations_command_plans_CommandPlanId",
                        column: x => x.CommandPlanId,
                        principalSchema: "engineering",
                        principalTable: "command_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex("IX_engineering_systems_BuildingId", "engineering_systems", "BuildingId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_systems_SystemType", "engineering_systems", "SystemType", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_capabilities_Code", "engineering_capabilities", "Code", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_capabilities_EngineeringSystemId", "engineering_capabilities", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_resources_Code", "engineering_resources", "Code", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_resources_EngineeringSystemId", "engineering_resources", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_zones_EngineeringSystemId", "engineering_zones", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_zone_rooms_SystemZoneId", "engineering_zone_rooms", "SystemZoneId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_system_devices_EngineeringSystemId", "engineering_system_devices", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_engineering_strategies_Type", "engineering_strategies", "Type", schema: "engineering");
            migrationBuilder.CreateIndex("IX_command_plans_EngineeringSystemId", "command_plans", "EngineeringSystemId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_command_plans_Status", "command_plans", "Status", schema: "engineering");
            migrationBuilder.CreateIndex("IX_command_plan_steps_CommandPlanId", "command_plan_steps", "CommandPlanId", schema: "engineering");
            migrationBuilder.CreateIndex("IX_command_plan_resource_allocations_CommandPlanId", "command_plan_resource_allocations", "CommandPlanId", schema: "engineering");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "command_plan_resource_allocations", schema: "engineering");
            migrationBuilder.DropTable(name: "command_plan_steps", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_capabilities", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_resources", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_zone_rooms", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_system_devices", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_strategies", schema: "engineering");
            migrationBuilder.DropTable(name: "command_plans", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_zones", schema: "engineering");
            migrationBuilder.DropTable(name: "engineering_systems", schema: "engineering");
        }
    }
}