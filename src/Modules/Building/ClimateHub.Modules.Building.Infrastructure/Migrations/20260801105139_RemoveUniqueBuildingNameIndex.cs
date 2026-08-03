using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Building.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueBuildingNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_buildings_Name",
                schema: "building",
                table: "buildings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_buildings_Name",
                schema: "building",
                table: "buildings",
                column: "Name",
                unique: true);
        }
    }
}
