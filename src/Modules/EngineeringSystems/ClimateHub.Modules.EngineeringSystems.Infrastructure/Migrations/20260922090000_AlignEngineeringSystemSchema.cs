using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.EngineeringSystems.Infrastructure.Migrations
{
    [DbContext(typeof(EngineeringSystemsDbContext))]
    [Migration("20260922090000_AlignEngineeringSystemSchema")]
    public partial class AlignEngineeringSystemSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("Lifecycle", "engineering_systems", "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active");
            migrationBuilder.AddColumn<string>("OperationalStatus", "engineering_systems", "engineering", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Unknown");
            migrationBuilder.AddColumn<DateTimeOffset>("CreatedAt", "engineering_systems", "engineering", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
            migrationBuilder.AddColumn<DateTimeOffset>("UpdatedAt", "engineering_systems", "engineering", type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("Lifecycle", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("OperationalStatus", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("CreatedAt", "engineering_systems", "engineering");
            migrationBuilder.DropColumn("UpdatedAt", "engineering_systems", "engineering");
        }
    }
}
