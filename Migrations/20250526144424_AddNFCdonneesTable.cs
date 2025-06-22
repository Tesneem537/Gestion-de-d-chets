using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteManagement3.Migrations
{
    /// <inheritdoc />
    public partial class AddNFCdonneesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HotelName",
                table: "WeeklyStats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeek",
                table: "WeeklyStats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyStats_WeekNumber_Year",
                table: "WeeklyStats",
                columns: new[] { "WeekNumber", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollection_EntryTime",
                table: "WasteCollection",
                column: "EntryTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyStats_WeekNumber_Year",
                table: "WeeklyStats");

            migrationBuilder.DropIndex(
                name: "IX_WasteCollection_EntryTime",
                table: "WasteCollection");

            migrationBuilder.AlterColumn<string>(
                name: "HotelName",
                table: "WeeklyStats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeek",
                table: "WeeklyStats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
