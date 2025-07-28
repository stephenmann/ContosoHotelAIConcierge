using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ContosoHotels.Migrations
{
    public partial class AddRoomServiceAndHousekeeping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HousekeepingRequests",
                columns: table => new
                {
                    HousekeepingRequestId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(nullable: false),
                    RequestType = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    RequestDate = table.Column<DateTime>(nullable: false),
                    CompletionDate = table.Column<DateTime>(nullable: true),
                    Notes = table.Column<string>(maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousekeepingRequests", x => x.HousekeepingRequestId);
                    table.ForeignKey(
                        name: "FK_HousekeepingRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomServices",
                columns: table => new
                {
                    RoomServiceId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(nullable: false),
                    ItemName = table.Column<string>(maxLength: 200, nullable: false),
                    ItemDescription = table.Column<string>(maxLength: 1000, nullable: true),
                    ServiceType = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    RequestDate = table.Column<DateTime>(nullable: false),
                    DeliveryDate = table.Column<DateTime>(nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpecialInstructions = table.Column<string>(maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomServices", x => x.RoomServiceId);
                    table.ForeignKey(
                        name: "FK_RoomServices_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HousekeepingRequests_BookingId",
                table: "HousekeepingRequests",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomServices_BookingId",
                table: "RoomServices",
                column: "BookingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HousekeepingRequests");

            migrationBuilder.DropTable(
                name: "RoomServices");
        }
    }
}
