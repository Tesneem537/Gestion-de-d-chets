using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteManagement3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Collector",
                columns: table => new
                {
                    CollectorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectorName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collector", x => x.CollectorID);
                });

            migrationBuilder.CreateTable(
                name: "Hotel",
                columns: table => new
                {
                    HotelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HotelName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hotel", x => x.HotelID);
                });

            migrationBuilder.CreateTable(
                name: "Truck",
                columns: table => new
                {
                    TruckID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TruckName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Truck", x => x.TruckID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "WasteType",
                columns: table => new
                {
                    WasteTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WasteTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteType", x => x.WasteTypeID);
                });

            migrationBuilder.CreateTable(
                name: "GarageCheckin",
                columns: table => new
                {
                    CheckinID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectorID = table.Column<int>(type: "int", nullable: false),
                    TruckID = table.Column<int>(type: "int", nullable: false),
                    CheckinTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarageCheckin", x => x.CheckinID);
                    table.ForeignKey(
                        name: "FK_GarageCheckin_Truck_TruckID",
                        column: x => x.TruckID,
                        principalTable: "Truck",
                        principalColumn: "TruckID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GarageCheckin_Users_CollectorID",
                        column: x => x.CollectorID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WasteCollection",
                columns: table => new
                {
                    WasteCollectionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollectorID = table.Column<int>(type: "int", nullable: false),
                    HotelID = table.Column<int>(type: "int", nullable: false),
                    TruckName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TruckID = table.Column<int>(type: "int", nullable: false),
                    WasteTypeID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Photo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CollectorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HotelName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteCollection", x => x.WasteCollectionID);
                    table.ForeignKey(
                        name: "FK_WasteCollection_Collector_CollectorID",
                        column: x => x.CollectorID,
                        principalTable: "Collector",
                        principalColumn: "CollectorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WasteCollection_Hotel_HotelID",
                        column: x => x.HotelID,
                        principalTable: "Hotel",
                        principalColumn: "HotelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WasteCollection_Truck_TruckID",
                        column: x => x.TruckID,
                        principalTable: "Truck",
                        principalColumn: "TruckID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WasteCollection_WasteType_WasteTypeID",
                        column: x => x.WasteTypeID,
                        principalTable: "WasteType",
                        principalColumn: "WasteTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarageCheckin_CollectorID",
                table: "GarageCheckin",
                column: "CollectorID");

            migrationBuilder.CreateIndex(
                name: "IX_GarageCheckin_TruckID",
                table: "GarageCheckin",
                column: "TruckID");

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollection_CollectorID",
                table: "WasteCollection",
                column: "CollectorID");

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollection_HotelID",
                table: "WasteCollection",
                column: "HotelID");

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollection_TruckID",
                table: "WasteCollection",
                column: "TruckID");

            migrationBuilder.CreateIndex(
                name: "IX_WasteCollection_WasteTypeID",
                table: "WasteCollection",
                column: "WasteTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GarageCheckin");

            migrationBuilder.DropTable(
                name: "WasteCollection");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Collector");

            migrationBuilder.DropTable(
                name: "Hotel");

            migrationBuilder.DropTable(
                name: "Truck");

            migrationBuilder.DropTable(
                name: "WasteType");
        }
    }
}
