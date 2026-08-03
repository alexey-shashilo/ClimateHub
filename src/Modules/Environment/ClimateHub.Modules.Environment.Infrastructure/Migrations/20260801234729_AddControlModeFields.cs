using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Environment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlModeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Co2Mode",
                schema: "environment",
                table: "room_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HumidityMode",
                schema: "environment",
                table: "room_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IlluminanceMode",
                schema: "environment",
                table: "room_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TemperatureMode",
                schema: "environment",
                table: "room_policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "environment",
                table: "room_policies",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Co2Mode",
                schema: "environment",
                table: "room_policies");

            migrationBuilder.DropColumn(
                name: "HumidityMode",
                schema: "environment",
                table: "room_policies");

            migrationBuilder.DropColumn(
                name: "IlluminanceMode",
                schema: "environment",
                table: "room_policies");

            migrationBuilder.DropColumn(
                name: "TemperatureMode",
                schema: "environment",
                table: "room_policies");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "environment",
                table: "room_policies");
        }
    }
}
