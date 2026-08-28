using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjuntos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adjuntos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OwnerTipo = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NombreArchivo = table.Column<string>(type: "varchar(260)", maxLength: 260, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    RutaRelativa = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubidoPorUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjuntos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Adjuntos_AdministradorId_OwnerTipo_OwnerId",
                table: "Adjuntos",
                columns: new[] { "AdministradorId", "OwnerTipo", "OwnerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adjuntos");
        }
    }
}
