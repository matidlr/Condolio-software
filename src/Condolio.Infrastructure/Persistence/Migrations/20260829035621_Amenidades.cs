using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Amenidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Amenidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConsorcioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImagenesIds = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reservable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IntervaloMinutos = table.Column<int>(type: "int", nullable: false),
                    LimiteMensual = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxReservasPorUnidad = table.Column<int>(type: "int", nullable: false),
                    TieneCosto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Tarifa = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    RequiereAprobacion = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReservableDesde = table.Column<DateOnly>(type: "date", nullable: true),
                    DiasBloqueados = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MensajeReserva = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenidades", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AmenidadHorarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AmenidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Dia = table.Column<int>(type: "int", nullable: false),
                    Cerrado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AbreMin = table.Column<int>(type: "int", nullable: false),
                    CierraMin = table.Column<int>(type: "int", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmenidadHorarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmenidadHorarios_Amenidades_AmenidadId",
                        column: x => x.AmenidadId,
                        principalTable: "Amenidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConsorcioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AmenidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SolicitanteUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SolicitanteNombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Inicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Fin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    Nota = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResueltaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Amenidades_AmenidadId",
                        column: x => x.AmenidadId,
                        principalTable: "Amenidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Amenidades_AdministradorId_ConsorcioId",
                table: "Amenidades",
                columns: new[] { "AdministradorId", "ConsorcioId" });

            migrationBuilder.CreateIndex(
                name: "IX_AmenidadHorarios_AdministradorId_AmenidadId",
                table: "AmenidadHorarios",
                columns: new[] { "AdministradorId", "AmenidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_AmenidadHorarios_AmenidadId",
                table: "AmenidadHorarios",
                column: "AmenidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_AdministradorId_ConsorcioId_Estado",
                table: "Reservas",
                columns: new[] { "AdministradorId", "ConsorcioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_AmenidadId_Inicio",
                table: "Reservas",
                columns: new[] { "AmenidadId", "Inicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AmenidadHorarios");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Amenidades");
        }
    }
}
