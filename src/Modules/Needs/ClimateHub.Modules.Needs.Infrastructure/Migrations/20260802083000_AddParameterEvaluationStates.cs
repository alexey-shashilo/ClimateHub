using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClimateHub.Modules.Needs.Infrastructure.Migrations
{
    public partial class AddParameterEvaluationStates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_parameter_evaluation_states",
                schema: "needs",
                columns: table => new
                {
                    RoomParameterEvaluationStateId = table.Column<long>(type: "bigint", nullable: false)
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
                    table.PrimaryKey("PK_room_parameter_evaluation_states", x => x.RoomParameterEvaluationStateId);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_room_parameter_evaluation_states_LastEvaluationAt",
                schema: "needs",
                table: "room_parameter_evaluation_states",
                column: "LastEvaluationAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_parameter_evaluation_states",
                schema: "needs");
        }
    }
}
