using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Devices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceXminRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "device",
                table: "devices",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "device",
                table: "devices");
        }
    }
}
