using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class FixSolicitudTerceros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConductorCedulaUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConductorFormacionVialUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConductorLicenciaUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConductorNombre",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConductorPlanillaSeguridadSocialUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaHabilitacionMTUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaReportePESVUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoSolicitud",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoInspeccionesUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoLicenciaTransitoUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoPlanMantenimientoUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoPolizasUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoRTMUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiculoSOATUrl",
                table: "TbSolicitudTerceros",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConductorCedulaUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "ConductorFormacionVialUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "ConductorLicenciaUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "ConductorNombre",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "ConductorPlanillaSeguridadSocialUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "EmpresaHabilitacionMTUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "EmpresaReportePESVUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "TipoSolicitud",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "VehiculoInspeccionesUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "VehiculoLicenciaTransitoUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "VehiculoPlanMantenimientoUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "VehiculoPolizasUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "VehiculoRTMUrl",
                table: "TbSolicitudTerceros");

            migrationBuilder.DropColumn(
                name: "VehiculoSOATUrl",
                table: "TbSolicitudTerceros");
        }
    }
}
