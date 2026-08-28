using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadDetalles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AreaM2",
                table: "Unidades",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Seccion",
                table: "Unidades",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaM2",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "Seccion",
                table: "Unidades");
        }
    }
}
