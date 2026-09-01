using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreferenciasConsorcio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreferenciasConsorcio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConsorcioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResidentesPublican = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComentariosHabilitados = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AnunciosSiemprePorCorreo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasConsorcio", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasConsorcio_ConsorcioId",
                table: "PreferenciasConsorcio",
                column: "ConsorcioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenciasConsorcio");
        }
    }
}
