using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class FixTerceros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FondoPension",
                table: "TbSolicitudTerceros",
                newName: "RequiereCursosEspeciales");

            migrationBuilder.RenameColumn(
                name: "EPS",
                table: "TbSolicitudTerceros",
                newName: "PlanillaDePago");

            migrationBuilder.RenameColumn(
                name: "ARL",
                table: "TbSolicitudTerceros",
                newName: "Identificacion");

            migrationBuilder.AddColumn<string>(
                name: "CursosEspeciales",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CursosEspeciales",
                table: "TbSolicitudTerceros");

            migrationBuilder.RenameColumn(
                name: "RequiereCursosEspeciales",
                table: "TbSolicitudTerceros",
                newName: "FondoPension");

            migrationBuilder.RenameColumn(
                name: "PlanillaDePago",
                table: "TbSolicitudTerceros",
                newName: "EPS");

            migrationBuilder.RenameColumn(
                name: "Identificacion",
                table: "TbSolicitudTerceros",
                newName: "ARL");
        }
    }
}
