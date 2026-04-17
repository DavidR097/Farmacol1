using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class TbCarpeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarpetaId",
                schema: "dbo",
                table: "TBExpedientes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TbCarpetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CC = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarpetaPadreId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbCarpetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TbCarpetas_TbCarpetas_CarpetaPadreId",
                        column: x => x.CarpetaPadreId,
                        principalTable: "TbCarpetas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBExpedientes_CarpetaId",
                schema: "dbo",
                table: "TBExpedientes",
                column: "CarpetaId");

            migrationBuilder.CreateIndex(
                name: "IX_TbCarpetas_CarpetaPadreId",
                table: "TbCarpetas",
                column: "CarpetaPadreId");

            migrationBuilder.AddForeignKey(
                name: "FK_TBExpedientes_TbCarpetas_CarpetaId",
                schema: "dbo",
                table: "TBExpedientes",
                column: "CarpetaId",
                principalTable: "TbCarpetas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBExpedientes_TbCarpetas_CarpetaId",
                schema: "dbo",
                table: "TBExpedientes");

            migrationBuilder.DropTable(
                name: "TbCarpetas");

            migrationBuilder.DropIndex(
                name: "IX_TBExpedientes_CarpetaId",
                schema: "dbo",
                table: "TBExpedientes");

            migrationBuilder.DropColumn(
                name: "CarpetaId",
                schema: "dbo",
                table: "TBExpedientes");
        }
    }
}
