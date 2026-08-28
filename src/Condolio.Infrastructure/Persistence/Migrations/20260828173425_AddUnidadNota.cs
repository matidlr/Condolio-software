using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnidadNotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Texto = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutorUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadNotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadNotas_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadNotas_AdministradorId_UnidadId",
                table: "UnidadNotas",
                columns: new[] { "AdministradorId", "UnidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadNotas_UnidadId",
                table: "UnidadNotas",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnidadNotas");
        }
    }
}
