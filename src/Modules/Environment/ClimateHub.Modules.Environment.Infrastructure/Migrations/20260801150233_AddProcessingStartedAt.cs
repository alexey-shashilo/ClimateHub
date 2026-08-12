using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimateHub.Modules.Environment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStartedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedAt",
                schema: "environment",
                table: "telemetry_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IlluminanceMax",
                schema: "environment",
                table: "room_policies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IlluminanceMin",
                schema: "environment",
                table: "room_policies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "IlluminancePreferred",
                schema: "environment",
                table: "room_policies",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                schema: "environment",
                table: "telemetry_outbox");

            migrationBuilder.DropColumn(
                name: "IlluminanceMax",
                schema: "environment",
                table: "room_policies");

            migrationBuilder.DropColumn(
                name: "IlluminanceMin",
                schema: "environment",
                table: "room_policies");

            migrationBuilder.DropColumn(
                name: "IlluminancePreferred",
                schema: "environment",
                table: "room_policies");
        }
    }
}
