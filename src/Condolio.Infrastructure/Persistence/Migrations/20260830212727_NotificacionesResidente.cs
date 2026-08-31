using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesResidente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Categoria",
                table: "Notificaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DestinatarioUsuarioId",
                table: "Notificaciones",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PreferenciasNotificacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeguridadApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SeguridadMail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FinanzasApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FinanzasMail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComunidadApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComunidadMail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EventosApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EventosMail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EdificioApp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EdificioMail = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasNotificacion", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_DestinatarioUsuarioId_LeidaUtc",
                table: "Notificaciones",
                columns: new[] { "DestinatarioUsuarioId", "LeidaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasNotificacion_UsuarioId",
                table: "PreferenciasNotificacion",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenciasNotificacion");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_DestinatarioUsuarioId_LeidaUtc",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "DestinatarioUsuarioId",
                table: "Notificaciones");
        }
    }
}
