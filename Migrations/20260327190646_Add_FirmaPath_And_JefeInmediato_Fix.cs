using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class Add_FirmaPath_And_JefeInmediato_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CargoJefeInmediato",
                table: "TBPersonal",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JefeInmediato",
                table: "TBPersonal",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoJefeInmediato",
                table: "TBPersonal");

            migrationBuilder.DropColumn(
                name: "JefeInmediato",
                table: "TBPersonal");
        }
    }
}
