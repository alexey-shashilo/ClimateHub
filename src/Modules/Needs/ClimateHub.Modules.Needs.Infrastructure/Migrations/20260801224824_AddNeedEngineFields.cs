using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Needs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedEngineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "needs");

            migrationBuilder.CreateTable(
                name: "needs",
                schema: "needs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DesiredMin = table.Column<double>(type: "double precision", nullable: false),
                    DesiredMax = table.Column<double>(type: "double precision", nullable: false),
                    DesiredPreferred = table.Column<double>(type: "double precision", nullable: false),
                    CurrentValue = table.Column<double>(type: "double precision", nullable: true),
                    Deviation = table.Column<double>(type: "double precision", nullable: false),
                    SourceParameterCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceMeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveCommandId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedCapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlanningFailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEvaluationAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CooldownUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StableSince = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GeneratedByPolicyVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_needs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_needs_RoomId_Status",
                schema: "needs",
                table: "needs",
                columns: new[] { "RoomId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "needs",
                schema: "needs");
        }
    }
}
