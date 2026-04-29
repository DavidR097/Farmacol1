using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class FixEquipos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TbSalidaEquipos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaRegistro = table.Column<DateOnly>(type: "date", nullable: true),
                    Solicitante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Elemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CódigoSerie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivoSalida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DebeRegresar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaSalida = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaRegreso = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservacionEstado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    EstadoConsulta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EtapaAprobacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AprobacionGerencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AprobacionCH = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbSalidaEquipos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TbSalidaEquipos");
        }
    }
}
