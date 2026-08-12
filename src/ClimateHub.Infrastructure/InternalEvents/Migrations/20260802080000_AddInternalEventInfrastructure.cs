using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClimateHub.Infrastructure.InternalEvents.Migrations
{
    [DbContext(typeof(ClimateHub.Infrastructure.InternalEvents.InternalEventsDbContext))]
    [Migration("20260802080000_AddInternalEventInfrastructure")]
    public partial class AddInternalEventInfrastructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "platform");

            migrationBuilder.CreateTable(
                name: "internal_event_outbox",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    Headers = table.Column<string>(type: "jsonb", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastFailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastFailureAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_internal_event_outbox", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "internal_event_inbox",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsumerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_internal_event_inbox", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "sse_event_log",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_sse_event_log", x => x.Id); });

            // Indexes for internal_event_outbox
            migrationBuilder.CreateIndex(
                name: "IX_internal_event_outbox_EventId",
                schema: "platform",
                table: "internal_event_outbox",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_internal_event_outbox_Status_AvailableAt",
                schema: "platform",
                table: "internal_event_outbox",
                columns: new[] { "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_event_outbox_Status_ProcessingStartedAt",
                schema: "platform",
                table: "internal_event_outbox",
                columns: new[] { "Status", "ProcessingStartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_event_outbox_EventType_CreatedAt",
                schema: "platform",
                table: "internal_event_outbox",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_event_outbox_AggregateId_CreatedAt",
                schema: "platform",
                table: "internal_event_outbox",
                columns: new[] { "AggregateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_event_outbox_RoomId",
                schema: "platform",
                table: "internal_event_outbox",
                column: "RoomId");

            // Indexes for internal_event_inbox
            migrationBuilder.CreateIndex(
                name: "IX_internal_event_inbox_ConsumerName_EventId",
                schema: "platform",
                table: "internal_event_inbox",
                columns: new[] { "ConsumerName", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_internal_event_inbox_Status_ReceivedAt",
                schema: "platform",
                table: "internal_event_inbox",
                columns: new[] { "Status", "ReceivedAt" });

            // Indexes for sse_event_log
            migrationBuilder.CreateIndex(
                name: "IX_sse_event_log_EventId",
                schema: "platform",
                table: "sse_event_log",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sse_event_log_OccurredAt",
                schema: "platform",
                table: "sse_event_log",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_sse_event_log_BuildingId_OccurredAt",
                schema: "platform",
                table: "sse_event_log",
                columns: new[] { "BuildingId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sse_event_log_RoomId_OccurredAt",
                schema: "platform",
                table: "sse_event_log",
                columns: new[] { "RoomId", "OccurredAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "internal_event_outbox", schema: "platform");
            migrationBuilder.DropTable(name: "internal_event_inbox", schema: "platform");
            migrationBuilder.DropTable(name: "sse_event_log", schema: "platform");
        }
    }
}
