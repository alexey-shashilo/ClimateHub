using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClimateHub.Modules.Environment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialEnvironmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "environment");

            migrationBuilder.CreateTable(
                name: "message_inbox",
                schema: "environment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    device_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    boot_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_inbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "room_parameters",
                schema: "environment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Parameter = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Quality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceDeviceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_parameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "telemetry_outbox",
                schema: "environment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    RelativeHumidityPct = table.Column<double>(type: "double precision", nullable: true),
                    Co2Ppm = table.Column<double>(type: "double precision", nullable: true),
                    Quality = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_outbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_message_inbox_Source_message_id",
                schema: "environment",
                table: "message_inbox",
                columns: new[] { "Source", "message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_parameters_RoomId_Parameter",
                schema: "environment",
                table: "room_parameters",
                columns: new[] { "RoomId", "Parameter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_outbox_Status",
                schema: "environment",
                table: "telemetry_outbox",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "message_inbox",
                schema: "environment");

            migrationBuilder.DropTable(
                name: "room_parameters",
                schema: "environment");

            migrationBuilder.DropTable(
                name: "telemetry_outbox",
                schema: "environment");
        }
    }
}
