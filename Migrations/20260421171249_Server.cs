using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class Server : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AniosAntiguedad",
                table: "TBPersonal",
                newName: "AñosAntiguedad");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaReposición",
                table: "TBSolicitudes",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaReposición",
                table: "TBSolicitudes");

            migrationBuilder.RenameColumn(
                name: "AñosAntiguedad",
                table: "TBPersonal",
                newName: "AniosAntiguedad");
        }
    }
}
