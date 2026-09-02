using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProveedorCamposDirectorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Proveedores",
                type: "varchar(240)",
                maxLength: 240,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "Proveedores",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Horario",
                table: "Proveedores",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Recomendado",
                table: "Proveedores",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SitioWeb",
                table: "Proveedores",
                type: "varchar(240)",
                maxLength: 240,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TelefonoAlt",
                table: "Proveedores",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Horario",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Recomendado",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "SitioWeb",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TelefonoAlt",
                table: "Proveedores");
        }
    }
}
