using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClimateHub.Modules.Needs.Infrastructure.Migrations
{
    public partial class AddNeedClosedLoopFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new fields to needs table
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ViolationSince",
                schema: "needs",
                table: "needs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectEvaluationDueAt",
                schema: "needs",
                table: "needs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCommandCreatedAt",
                schema: "needs",
                table: "needs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastMeaningfulImprovementAt",
                schema: "needs",
                table: "needs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanningAttemptCount",
                schema: "needs",
                table: "needs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CommandAttemptCount",
                schema: "needs",
                table: "needs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LastCommandId",
                schema: "needs",
                table: "needs",
                type: "uuid",
                nullable: true);

            // Create need_evaluations table
            migrationBuilder.CreateTable(
                name: "need_evaluations",
                schema: "needs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EnvironmentSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    PolicySnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    PreviousStatus = table.Column<string>(type: "text", nullable: true),
                    NewStatus = table.Column<string>(type: "text", nullable: true),
                    CalculationResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    CapabilityPlanJson = table.Column<string>(type: "jsonb", nullable: true),
                    DeviceResolutionResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_need_evaluations", x => x.Id);
                });

            // Create indexes for need_evaluations
            migrationBuilder.CreateIndex(
                name: "IX_need_evaluations_NeedId",
                schema: "needs",
                table: "need_evaluations",
                column: "NeedId");

            migrationBuilder.CreateIndex(
                name: "IX_need_evaluations_EvaluatedAt",
                schema: "needs",
                table: "need_evaluations",
                column: "EvaluatedAt");

            // Drop old index and create new ones for needs
            migrationBuilder.DropIndex(
                name: "IX_needs_RoomId_Status",
                schema: "needs",
                table: "needs");

            // Partial unique index for active needs by RoomId + Type
            migrationBuilder.CreateIndex(
                name: "IX_needs_RoomId_Type_Active",
                schema: "needs",
                table: "needs",
                columns: new[] { "RoomId", "Type" },
                filter: "Status IN ('Detected', 'Planning', 'Planned', 'Executing', 'WaitingForEffect', 'Blocked')",
                unique: true);

            // Performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_needs_Status_CooldownUntil",
                schema: "needs",
                table: "needs",
                columns: new[] { "Status", "CooldownUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_needs_Status_EffectEvaluationDueAt",
                schema: "needs",
                table: "needs",
                columns: new[] { "Status", "EffectEvaluationDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_needs_Status_LastEvaluationAt",
                schema: "needs",
                table: "needs",
                columns: new[] { "Status", "LastEvaluationAt" });

            migrationBuilder.CreateIndex(
                name: "IX_needs_ActiveCommandId",
                schema: "needs",
                table: "needs",
                column: "ActiveCommandId");

            migrationBuilder.CreateIndex(
                name: "IX_needs_RoomId_Status",
                schema: "needs",
                table: "needs",
                columns: new[] { "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_needs_BuildingId_Status",
                schema: "needs",
                table: "needs",
                columns: new[] { "BuildingId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "need_evaluations",
                schema: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_RoomId_Type_Active",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_Status_CooldownUntil",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_Status_EffectEvaluationDueAt",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_Status_LastEvaluationAt",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_ActiveCommandId",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_BuildingId_Status",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "ViolationSince",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "EffectEvaluationDueAt",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "LastCommandCreatedAt",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "LastMeaningfulImprovementAt",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "PlanningAttemptCount",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "CommandAttemptCount",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "LastCommandId",
                schema: "needs",
                table: "needs");

            migrationBuilder.CreateIndex(
                name: "IX_needs_RoomId_Status",
                schema: "needs",
                table: "needs",
                columns: new[] { "RoomId", "Status" });
        }
    }
}