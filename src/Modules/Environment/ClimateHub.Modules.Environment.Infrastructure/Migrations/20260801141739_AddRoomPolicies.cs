using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Environment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_policies",
                schema: "environment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemperatureMin = table.Column<double>(type: "double precision", nullable: true),
                    TemperatureMax = table.Column<double>(type: "double precision", nullable: true),
                    TemperaturePreferred = table.Column<double>(type: "double precision", nullable: true),
                    HumidityMin = table.Column<double>(type: "double precision", nullable: true),
                    HumidityMax = table.Column<double>(type: "double precision", nullable: true),
                    HumidityPreferred = table.Column<double>(type: "double precision", nullable: true),
                    Co2Min = table.Column<double>(type: "double precision", nullable: true),
                    Co2Max = table.Column<double>(type: "double precision", nullable: true),
                    Co2Preferred = table.Column<double>(type: "double precision", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_policies", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_policies",
                schema: "environment");
        }
    }
}
