using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class AddImagenToTbAnuncio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Añadir columna Imagen sólo si no existe (protección contra bases de datos ya actualizadas)
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.TbAnuncios','Imagen') IS NULL
BEGIN
    ALTER TABLE dbo.TbAnuncios ADD Imagen nvarchar(200) NULL
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar sólo si existe
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.TbAnuncios','Imagen') IS NOT NULL
BEGIN
    ALTER TABLE dbo.TbAnuncios DROP COLUMN Imagen
END");
        }
    }
}
