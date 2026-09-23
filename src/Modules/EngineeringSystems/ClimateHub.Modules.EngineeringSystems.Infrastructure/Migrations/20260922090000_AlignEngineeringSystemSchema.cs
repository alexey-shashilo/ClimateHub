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
            migrationBuilder.DropColumn("Lifecycle", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("LegacyStatus", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("OperationalStatus", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("CreatedAt", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("UpdatedAt", "engineering_systems", "engineering");

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
