using Microsoft.EntityFrameworkCore.Migrations;

namespace ContosoHotels.Migrations
{
    public partial class UpdateRoomDefaultCities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set default cities for existing rooms
            var cities = new[] { 
                "New York", "Miami", "Los Angeles", "San Francisco", "Chicago", 
                "Boston", "Seattle", "Denver", "Austin", "San Diego"
            };

            for (int i = 0; i < cities.Length; i++)
            {
                var startRange = i * 20 + 1;
                var endRange = (i + 1) * 20;
                
                migrationBuilder.Sql(
                    $"UPDATE Rooms SET City = '{cities[i]}' WHERE RoomId BETWEEN {startRange} AND {endRange}");
            }

            // Set default cities for any remaining rooms
            migrationBuilder.Sql(
                "UPDATE Rooms SET City = 'New York' WHERE City = ''");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration needed
        }
    }
}
