using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Building.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingXminRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "building",
                table: "buildings",
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
                schema: "building",
                table: "buildings");
        }
    }
}
