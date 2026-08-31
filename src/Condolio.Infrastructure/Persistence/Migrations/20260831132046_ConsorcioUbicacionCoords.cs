using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsorcioUbicacionCoords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "Consorcios",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "Consorcios",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Consorcios");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Consorcios");
        }
    }
}
