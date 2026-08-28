using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncidenciaComentariosYEscalar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscaladaUtc",
                table: "UnidadIncidencias",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IncidenciaComentarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IncidenciaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Texto = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutorUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidenciaComentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidenciaComentarios_UnidadIncidencias_IncidenciaId",
                        column: x => x.IncidenciaId,
                        principalTable: "UnidadIncidencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_IncidenciaComentarios_AdministradorId_IncidenciaId",
                table: "IncidenciaComentarios",
                columns: new[] { "AdministradorId", "IncidenciaId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidenciaComentarios_IncidenciaId",
                table: "IncidenciaComentarios",
                column: "IncidenciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidenciaComentarios");

            migrationBuilder.DropColumn(
                name: "EscaladaUtc",
                table: "UnidadIncidencias");
        }
    }
}
