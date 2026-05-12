using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class FixSolicitudes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Motivo",
                table: "TBSoliRechazada",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "TBSolicitudes",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirigidoA",
                table: "TBSolicitudes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaRetiro",
                table: "TBSolicitudes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncluirFunciones",
                table: "TBSolicitudes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncluirSueldo",
                table: "TBSolicitudes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoCesantias",
                table: "TBSolicitudes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCesantias",
                table: "TBSolicitudes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioCorporativo",
                table: "TBPersonal",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirigidoA",
                table: "TBSolicitudes");

            migrationBuilder.DropColumn(
                name: "FechaRetiro",
                table: "TBSolicitudes");

            migrationBuilder.DropColumn(
                name: "IncluirFunciones",
                table: "TBSolicitudes");

            migrationBuilder.DropColumn(
                name: "IncluirSueldo",
                table: "TBSolicitudes");

            migrationBuilder.DropColumn(
                name: "MontoCesantias",
                table: "TBSolicitudes");

            migrationBuilder.DropColumn(
                name: "MotivoCesantias",
                table: "TBSolicitudes");

            migrationBuilder.AlterColumn<string>(
                name: "Motivo",
                table: "TBSoliRechazada",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "TBSolicitudes",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioCorporativo",
                table: "TBPersonal",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
