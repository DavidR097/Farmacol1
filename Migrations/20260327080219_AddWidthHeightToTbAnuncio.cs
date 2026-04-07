using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class AddWidthHeightToTbAnuncio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "TbAnuncios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "TbAnuncios",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "TbAnuncios");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "TbAnuncios");
        }
    }
}
