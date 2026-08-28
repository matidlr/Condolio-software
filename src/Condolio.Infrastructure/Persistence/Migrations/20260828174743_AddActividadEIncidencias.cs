using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActividadEIncidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnidadActividades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detalle = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadActividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadActividades_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UnidadIncidencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Titulo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Severidad = table.Column<int>(type: "int", nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Etiquetas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutorUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadIncidencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadIncidencias_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadActividades_AdministradorId_UnidadId",
                table: "UnidadActividades",
                columns: new[] { "AdministradorId", "UnidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadActividades_UnidadId",
                table: "UnidadActividades",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadIncidencias_AdministradorId_UnidadId",
                table: "UnidadIncidencias",
                columns: new[] { "AdministradorId", "UnidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadIncidencias_UnidadId",
                table: "UnidadIncidencias",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnidadActividades");

            migrationBuilder.DropTable(
                name: "UnidadIncidencias");
        }
    }
}
