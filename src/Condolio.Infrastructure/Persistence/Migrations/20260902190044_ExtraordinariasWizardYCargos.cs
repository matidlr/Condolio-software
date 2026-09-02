using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Condolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtraordinariasWizardYCargos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadCuotas",
                table: "Extraordinarias");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "Extraordinarias");

            migrationBuilder.RenameColumn(
                name: "PeriodoInicioMes",
                table: "Extraordinarias",
                newName: "MetodoReparto");

            migrationBuilder.RenameColumn(
                name: "PeriodoInicioAnio",
                table: "Extraordinarias",
                newName: "MesesEmitidos");

            migrationBuilder.RenameColumn(
                name: "FechaAprobacion",
                table: "Extraordinarias",
                newName: "FechaVencimiento");

            migrationBuilder.RenameColumn(
                name: "CuotasEmitidas",
                table: "Extraordinarias",
                newName: "Categoria");

            migrationBuilder.RenameColumn(
                name: "CriterioDistribucion",
                table: "Extraordinarias",
                newName: "CantidadMeses");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Extraordinarias",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(240)",
                oldMaxLength: 240)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaInicio",
                table: "Extraordinarias",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "Extraordinarias",
                type: "varchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CargosUnidad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConsorcioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadNombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Origen = table.Column<int>(type: "int", nullable: false),
                    ExtraordinariaId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Concepto = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Monto = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    MontoPagado = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: true),
                    Cuota = table.Column<int>(type: "int", nullable: false),
                    TotalCuotas = table.Column<int>(type: "int", nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargosUnidad", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExtraordinariaUnidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AdministradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConsorcioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExtraordinariaId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadNombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MontoAsignado = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CreadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtraordinariaUnidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtraordinariaUnidades_Extraordinarias_ExtraordinariaId",
                        column: x => x.ExtraordinariaId,
                        principalTable: "Extraordinarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CargosUnidad_ConsorcioId_UnidadId_Estado",
                table: "CargosUnidad",
                columns: new[] { "ConsorcioId", "UnidadId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_CargosUnidad_ExtraordinariaId",
                table: "CargosUnidad",
                column: "ExtraordinariaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtraordinariaUnidades_ConsorcioId_UnidadId",
                table: "ExtraordinariaUnidades",
                columns: new[] { "ConsorcioId", "UnidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtraordinariaUnidades_ExtraordinariaId",
                table: "ExtraordinariaUnidades",
                column: "ExtraordinariaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CargosUnidad");

            migrationBuilder.DropTable(
                name: "ExtraordinariaUnidades");

            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "Extraordinarias");

            migrationBuilder.DropColumn(
                name: "Titulo",
                table: "Extraordinarias");

            migrationBuilder.RenameColumn(
                name: "MetodoReparto",
                table: "Extraordinarias",
                newName: "PeriodoInicioMes");

            migrationBuilder.RenameColumn(
                name: "MesesEmitidos",
                table: "Extraordinarias",
                newName: "PeriodoInicioAnio");

            migrationBuilder.RenameColumn(
                name: "FechaVencimiento",
                table: "Extraordinarias",
                newName: "FechaAprobacion");

            migrationBuilder.RenameColumn(
                name: "Categoria",
                table: "Extraordinarias",
                newName: "CuotasEmitidas");

            migrationBuilder.RenameColumn(
                name: "CantidadMeses",
                table: "Extraordinarias",
                newName: "CriterioDistribucion");

            migrationBuilder.UpdateData(
                table: "Extraordinarias",
                keyColumn: "Descripcion",
                keyValue: null,
                column: "Descripcion",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "Extraordinarias",
                type: "varchar(240)",
                maxLength: 240,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CantidadCuotas",
                table: "Extraordinarias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "Extraordinarias",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
