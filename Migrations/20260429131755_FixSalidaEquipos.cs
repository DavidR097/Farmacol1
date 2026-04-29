using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class FixSalidaEquipos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAprobacionCH",
                table: "TbSalidaEquipos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAprobacionGerencia",
                table: "TbSalidaEquipos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteCorreo",
                table: "TbSalidaEquipos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitanteUsuario",
                table: "TbSalidaEquipos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaAprobacionCH",
                table: "TbSalidaEquipos");

            migrationBuilder.DropColumn(
                name: "FechaAprobacionGerencia",
                table: "TbSalidaEquipos");

            migrationBuilder.DropColumn(
                name: "SolicitanteCorreo",
                table: "TbSalidaEquipos");

            migrationBuilder.DropColumn(
                name: "SolicitanteUsuario",
                table: "TbSalidaEquipos");
        }
    }
}
