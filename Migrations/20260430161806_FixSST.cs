using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class FixSST : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TbSolicitudTerceros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaRegistro = table.Column<DateOnly>(type: "date", nullable: true),
                    Solicitante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombresTerceros = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoDocumento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentoTerceros = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Empresa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactoEmpresa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoVisita = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaDirigida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquiposIngreso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IngresoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentacionSST = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EPS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ARL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FondoPension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiereEPP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ElementoEPP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AprobadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaAprobacionSST = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRechazo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RechazadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaDevolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DevueltoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservacionEstado = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbSolicitudTerceros", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TbSolicitudTerceros");
        }
    }
}
