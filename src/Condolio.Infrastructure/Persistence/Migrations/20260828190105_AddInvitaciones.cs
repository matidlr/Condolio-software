using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invitaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConsorcioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ExpiraUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    InvitadoPorUsuarioId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitaciones_Consorcios_ConsorcioId",
                        column: x => x.ConsorcioId,
                        principalTable: "Consorcios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_AdministradorId_ConsorcioId_Estado",
                table: "Invitaciones",
                columns: new[] { "AdministradorId", "ConsorcioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_ConsorcioId",
                table: "Invitaciones",
                column: "ConsorcioId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_Token",
                table: "Invitaciones",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitaciones");
        }
    }
}
