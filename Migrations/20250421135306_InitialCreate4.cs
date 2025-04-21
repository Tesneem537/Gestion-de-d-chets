using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteManagement3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollectorID",
                table: "WeeklyStats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WeeklyStats",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "WeeklyStats",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectorID",
                table: "WeeklyStats");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WeeklyStats");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "WeeklyStats");
        }
    }
}
