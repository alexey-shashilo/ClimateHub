using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Climate.Infrastructure.Migrations;

[DbContext(typeof(ClimateHub.Modules.Climate.Infrastructure.ClimateDbContext))]
[Migration("20260802170000_AddClimateSchema")]
public partial class AddClimateSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("climate");

        migrationBuilder.CreateTable("climate_goals",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ActiveProfile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                TargetTemperature = table.Column<double>(type: "double precision", nullable: false),
                TargetTemperatureMin = table.Column<double>(type: "double precision", nullable: false),
                TargetTemperatureMax = table.Column<double>(type: "double precision", nullable: false),
                TargetHumidity = table.Column<double>(type: "double precision", nullable: false),
                TargetHumidityMin = table.Column<double>(type: "double precision", nullable: false),
                TargetHumidityMax = table.Column<double>(type: "double precision", nullable: false),
                TargetCo2 = table.Column<double>(type: "double precision", nullable: false),
                TargetCo2Max = table.Column<double>(type: "double precision", nullable: false),
                TargetIlluminance = table.Column<double>(type: "double precision", nullable: false),
                CurrentTemperature = table.Column<double>(type: "double precision", nullable: true),
                CurrentHumidity = table.Column<double>(type: "double precision", nullable: true),
                CurrentCo2 = table.Column<double>(type: "double precision", nullable: true),
                CurrentIlluminance = table.Column<double>(type: "double precision", nullable: true),
                SatisfactionPct = table.Column<double>(type: "double precision", nullable: false),
                ActiveClimatePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                PolicyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                EnvironmentStateVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                LastEvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SatisfiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                BlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_climate_goals", x => x.Id));

        migrationBuilder.CreateTable("climate_plans",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                ActiveProfile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                PlanningReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                EnvironmentSnapshotVersion = table.Column<string>(type: "text", nullable: true),
                PolicyVersion = table.Column<string>(type: "text", nullable: true),
                FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                PlannedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                WaitingForEffectAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_climate_plans", x => x.Id));

        migrationBuilder.CreateTable("engineering_sub_plans",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClimatePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                EngineeringCapabilityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RequestedEffect = table.Column<double>(type: "double precision", nullable: false),
                PriorityCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                PriorityValue = table.Column<int>(type: "integer", nullable: false),
                ExecutionOrder = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                EngineeringSystemId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                EngineeringCommandPlanId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Required = table.Column<bool>(type: "boolean", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_engineering_sub_plans", x => x.Id);
                table.ForeignKey("FK_engineering_sub_plans_climate_plans_ClimatePlanId", x => x.ClimatePlanId, principalSchema: "climate", principalTable: "climate_plans", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable("climate_plan_dependencies",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClimatePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                PredecessorSubPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                SuccessorSubPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Required = table.Column<bool>(type: "boolean", nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_climate_plan_dependencies", x => x.Id);
                table.ForeignKey("FK_climate_plan_dependencies_climate_plans_ClimatePlanId", x => x.ClimatePlanId, principalSchema: "climate", principalTable: "climate_plans", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable("climate_conflicts",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClimatePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                ConflictType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                FirstCapabilityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SecondCapabilityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                WinnerCapabilityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                LoserCapabilityCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Resolution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_climate_conflicts", x => x.Id);
                table.ForeignKey("FK_climate_conflicts_climate_plans_ClimatePlanId", x => x.ClimatePlanId, principalSchema: "climate", principalTable: "climate_plans", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable("climate_resources",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                ResourceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                MaximumCapacity = table.Column<double>(type: "double precision", nullable: false),
                AvailableCapacity = table.Column<double>(type: "double precision", nullable: false),
                ReservedCapacity = table.Column<double>(type: "double precision", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_climate_resources", x => x.Id));

        migrationBuilder.CreateTable("climate_resource_reservations",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClimateResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                ClimatePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                EngineeringSubPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                RequestedAmount = table.Column<double>(type: "double precision", nullable: false),
                ReservedAmount = table.Column<double>(type: "double precision", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ReservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_climate_resource_reservations", x => x.Id);
                table.ForeignKey("FK_climate_resource_reservations_climate_plans_ClimatePlanId", x => x.ClimatePlanId, principalSchema: "climate", principalTable: "climate_plans", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable("event_inbox",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConsumerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AggregateId = table.Column<string>(type: "text", nullable: true),
                BuildingId = table.Column<string>(type: "text", nullable: true),
                RoomId = table.Column<string>(type: "text", nullable: true),
                ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                PayloadHash = table.Column<string>(type: "text", nullable: true),
                AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_event_inbox", x => x.Id));

        migrationBuilder.CreateTable("climate_evaluations",
            schema: "climate",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClimateGoalId = table.Column<Guid>(type: "uuid", nullable: true),
                ClimatePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                BuildingId = table.Column<string>(type: "text", nullable: false),
                RoomId = table.Column<string>(type: "text", nullable: false),
                Trigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                EnvironmentSnapshot = table.Column<string>(type: "text", nullable: true),
                PolicySnapshot = table.Column<string>(type: "text", nullable: true),
                NeedSnapshot = table.Column<string>(type: "text", nullable: true),
                GoalBefore = table.Column<string>(type: "text", nullable: true),
                GoalAfter = table.Column<string>(type: "text", nullable: true),
                PlanningResult = table.Column<string>(type: "text", nullable: true),
                ConflictResolution = table.Column<string>(type: "text", nullable: true),
                DependencyGraph = table.Column<string>(type: "text", nullable: true),
                ResourceResult = table.Column<string>(type: "text", nullable: true),
                Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CorrelationId = table.Column<string>(type: "text", nullable: true),
                CausationId = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_climate_evaluations", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "climate_evaluations", schema: "climate");
        migrationBuilder.DropTable(name: "event_inbox", schema: "climate");
        migrationBuilder.DropTable(name: "climate_resource_reservations", schema: "climate");
        migrationBuilder.DropTable(name: "climate_conflicts", schema: "climate");
        migrationBuilder.DropTable(name: "climate_plan_dependencies", schema: "climate");
        migrationBuilder.DropTable(name: "engineering_sub_plans", schema: "climate");
        migrationBuilder.DropTable(name: "climate_resources", schema: "climate");
        migrationBuilder.DropTable(name: "climate_plans", schema: "climate");
        migrationBuilder.DropTable(name: "climate_goals", schema: "climate");
    }
}
