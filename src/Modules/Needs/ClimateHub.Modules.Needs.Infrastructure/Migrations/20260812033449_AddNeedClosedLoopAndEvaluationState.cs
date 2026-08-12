using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClimateHub.Modules.Needs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedClosedLoopAndEvaluationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveCommandPlanId",
                schema: "needs",
                table: "needs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommandAttemptCount",
                schema: "needs",
                table: "needs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<Guid>(
                name: "LastCommandId",
                schema: "needs",
                table: "needs",
                type: "uuid",
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

            migrationBuilder.AddColumn<string>(
                name: "SelectedEngineeringCapabilityCode",
                schema: "needs",
                table: "needs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedEngineeringSystemId",
                schema: "needs",
                table: "needs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ViolationSince",
                schema: "needs",
                table: "needs",
                type: "timestamp with time zone",
                nullable: true);

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
                    EnvironmentSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    PolicySnapshotJson = table.Column<string>(type: "text", nullable: true),
                    PreviousStatus = table.Column<string>(type: "text", nullable: true),
                    NewStatus = table.Column<string>(type: "text", nullable: true),
                    CalculationResultJson = table.Column<string>(type: "text", nullable: true),
                    CapabilityPlanJson = table.Column<string>(type: "text", nullable: true),
                    DeviceResolutionResultJson = table.Column<string>(type: "text", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "room_parameter_evaluation_states",
                schema: "needs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ViolationDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ViolationSince = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastValue = table.Column<double>(type: "double precision", nullable: true),
                    LastMeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastQuality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LastEvaluationAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_parameter_evaluation_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_needs_ActiveCommandId",
                schema: "needs",
                table: "needs",
                column: "ActiveCommandId");

            migrationBuilder.CreateIndex(
                name: "IX_needs_BuildingId_Status",
                schema: "needs",
                table: "needs",
                columns: new[] { "BuildingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_needs_RoomId_Type",
                schema: "needs",
                table: "needs",
                columns: new[] { "RoomId", "Type" },
                unique: true,
                filter: "\"Status\" IN ('Detected', 'Planning', 'Planned', 'Executing', 'WaitingForEffect', 'Blocked')");

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
                name: "IX_need_evaluations_EvaluatedAt",
                schema: "needs",
                table: "need_evaluations",
                column: "EvaluatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_need_evaluations_NeedId",
                schema: "needs",
                table: "need_evaluations",
                column: "NeedId");

            migrationBuilder.CreateIndex(
                name: "IX_room_parameter_evaluation_states_LastEvaluationAt",
                schema: "needs",
                table: "room_parameter_evaluation_states",
                column: "LastEvaluationAt");

            migrationBuilder.CreateIndex(
                name: "IX_room_parameter_evaluation_states_RoomId_ParameterCode",
                schema: "needs",
                table: "room_parameter_evaluation_states",
                columns: new[] { "RoomId", "ParameterCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_parameter_evaluation_states_ViolationSince",
                schema: "needs",
                table: "room_parameter_evaluation_states",
                column: "ViolationSince");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "need_evaluations",
                schema: "needs");

            migrationBuilder.DropTable(
                name: "room_parameter_evaluation_states",
                schema: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_ActiveCommandId",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_BuildingId_Status",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropIndex(
                name: "IX_needs_RoomId_Type",
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

            migrationBuilder.DropColumn(
                name: "ActiveCommandPlanId",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "CommandAttemptCount",
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
                name: "LastCommandId",
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
                name: "SelectedEngineeringCapabilityCode",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "SelectedEngineeringSystemId",
                schema: "needs",
                table: "needs");

            migrationBuilder.DropColumn(
                name: "ViolationSince",
                schema: "needs",
                table: "needs");
        }
    }
}
