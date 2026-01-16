using Microsoft.EntityFrameworkCore.Migrations;

namespace APPDEV_PROJECT.Migrations
{
    public partial class AddWorkerLocationCoordinates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Workers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Workers",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Workers");
        }
    }
}
