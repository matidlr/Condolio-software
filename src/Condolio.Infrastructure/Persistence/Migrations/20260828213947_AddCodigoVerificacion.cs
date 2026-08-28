using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoVerificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoVerificacion",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoVerificacionExpiraUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoVerificacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CodigoVerificacionExpiraUtc",
                table: "AspNetUsers");
        }
    }
}
