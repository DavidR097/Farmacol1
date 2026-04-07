using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Farmacol.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TbAnuncios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TbAnuncios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TBAreas",
                columns: table => new
                {
                    ID_Area = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBAreas", x => x.ID_Area);
                });

            migrationBuilder.CreateTable(
                name: "TBAuditTrail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Usuario = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EntidadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBAuditTrail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TBDelegaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CC = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreadaPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AprobadorOriginal = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBDelegaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TBExpedientes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CC = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    SubidoPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBExpedientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TBInventario",
                columns: table => new
                {
                    ID_Equipo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ubicación = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Ubicación2 = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Dispositivo = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Modelo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Serie = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    IMEI = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Marca = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Observación = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    Planta = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Cédula = table.Column<int>(type: "int", nullable: true),
                    Anexo = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBInvent__AB5A5EA976A52A69", x => x.ID_Equipo);
                });

            migrationBuilder.CreateTable(
                name: "TBNotificaciones",
                columns: table => new
                {
                    ID_Notificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioDestino = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Mensaje = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: false),
                    Leida = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ID_Solicitud = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBNotifi", x => x.ID_Notificacion);
                });

            migrationBuilder.CreateTable(
                name: "TBPersonal",
                columns: table => new
                {
                    CC = table.Column<int>(type: "int", nullable: false),
                    ExpedicionCiudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CiudadTrabajo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreColaborador = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodCeco = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreCentroCostos = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Gerencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FechaIngreso = table.Column<DateOnly>(type: "date", nullable: true),
                    VencimientoPeriodoPrueba = table.Column<DateOnly>(type: "date", nullable: true),
                    AniosAntiguedad = table.Column<int>(type: "int", nullable: true),
                    MesesAntiguedad = table.Column<int>(type: "int", nullable: true),
                    SalarioEnero2020 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioFeb2020 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioFeb2021 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioFeb2022 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioFeb2023 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioEneFeb2024 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioMar2024 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioFeb2025 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalarioFeb2026 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxAlimentacion2020 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxAlimentacion2021 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxAlimentacion2022 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxAlimentacion2023 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxGasolina2021 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxGasolina20222023 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxRodamiento = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AuxRodamiento20222023 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseIncentivo20222023 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MedicinaPrepagada = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LlaveSnacBebidas = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Edad = table.Column<int>(type: "int", nullable: true),
                    MesesEdad = table.Column<int>(type: "int", nullable: true),
                    MesNacimiento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Generacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Genero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CiudadNacimiento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstadoCivil = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CorreoPersonal = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Contacto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DireccionResidencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Barrio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Rh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ContactoEmergencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Parentesco = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TelefonoEmergencia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TipoContrato = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Eps = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FondoPensiones = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FondoCesantias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CajaCompensacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Arl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoCuenta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NumeroCuenta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TallaCamisa = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Grupo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Concepto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CorreoCorporativo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UsuarioCorporativo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FotoPerfil = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBPersonal", x => x.CC);
                });

            migrationBuilder.CreateTable(
                name: "TBPersonalRetirado",
                columns: table => new
                {
                    CC = table.Column<int>(type: "int", nullable: false),
                    NombreColaborador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorreoCorporativo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioCorporativo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRetiro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MotivoRetiro = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBPersonalRetirado", x => x.CC);
                });

            migrationBuilder.CreateTable(
                name: "TBPlantillas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Modulo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    SubidaPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBPlantillas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TBResponsivas",
                columns: table => new
                {
                    Cédula = table.Column<int>(type: "int", nullable: false),
                    Equipo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Marca = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Serie = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Observación = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBRespon__F12AB28853CB5A26", x => x.Cédula);
                });

            migrationBuilder.CreateTable(
                name: "TBSalas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBSalas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TBSolicitudes",
                columns: table => new
                {
                    ID_Solicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CC = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Cargo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Tipo_Solicitud = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Hora_Inicio = table.Column<int>(type: "int", nullable: true),
                    Hora_Fin = table.Column<int>(type: "int", nullable: true),
                    Total_Horas = table.Column<int>(type: "int", nullable: true),
                    Fecha_Inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    Fecha_Fin = table.Column<DateOnly>(type: "date", nullable: true),
                    Jefe_Inmediato = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Cargo_JInmediato = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Motivo = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Fecha_Solicitud = table.Column<DateOnly>(type: "date", nullable: true),
                    Aprob_JInmediato = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Aprob_CH = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Observaciones = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Anexos = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Estado = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Total_Dias = table.Column<int>(type: "int", nullable: true),
                    SubtipoPermiso = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    EtapaAprobacion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ObservacionJefe = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    ObservacionRRHH = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    Paso1Aprobador = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Paso1Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Paso1Obs = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    Paso2Aprobador = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Paso2Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Paso2Obs = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    Paso3Aprobador = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Paso3Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Paso3Obs = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    TotalPasos = table.Column<int>(type: "int", nullable: true),
                    PasoActual = table.Column<int>(type: "int", nullable: true),
                    NivelSolicitante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaDevolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TipoFlujo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentoSolicitado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBSolici__ED71123A33D0234D", x => x.ID_Solicitud);
                });

            migrationBuilder.CreateTable(
                name: "TBSoliRechazada",
                columns: table => new
                {
                    ID_Solicitud = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Cédula = table.Column<int>(type: "int", nullable: false),
                    Tipo_Solicitud = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Motivo = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Anexos = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Fecha_Solicitud = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBSoliRe__ED71123A21CDC8AB", x => x.ID_Solicitud);
                });

            migrationBuilder.CreateTable(
                name: "TBSubtiposPermiso",
                columns: table => new
                {
                    ID_Subtipo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBSubtip__730213EB2F928969", x => x.ID_Subtipo);
                });

            migrationBuilder.CreateTable(
                name: "TBTiposSolicitud",
                columns: table => new
                {
                    ID_Tipo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBTiposS__D34E66199410A618", x => x.ID_Tipo);
                });

            migrationBuilder.CreateTable(
                name: "TBVacaciones",
                columns: table => new
                {
                    ID_Vacación = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CC = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Fecha_Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fecha_Fin = table.Column<DateOnly>(type: "date", nullable: false),
                    Total_Días = table.Column<int>(type: "int", nullable: false),
                    Fecha_Solicitud = table.Column<DateOnly>(type: "date", nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Anexos = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TBVacaci__C39CCDF16ADBF4C9", x => x.ID_Vacación);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TBReservasSalas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaId = table.Column<int>(type: "int", nullable: false),
                    CC = table.Column<int>(type: "int", nullable: false),
                    NombreSolicitante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pendiente"),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    AtendidaPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBReservasSalas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TBReservasSalas_TBSalas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "TBSalas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TBReservasSalas_SalaId",
                table: "TBReservasSalas",
                column: "SalaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "TbAnuncios");

            migrationBuilder.DropTable(
                name: "TBAreas");

            migrationBuilder.DropTable(
                name: "TBAuditTrail");

            migrationBuilder.DropTable(
                name: "TBDelegaciones");

            migrationBuilder.DropTable(
                name: "TBExpedientes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "TBInventario");

            migrationBuilder.DropTable(
                name: "TBNotificaciones");

            migrationBuilder.DropTable(
                name: "TBPersonal");

            migrationBuilder.DropTable(
                name: "TBPersonalRetirado");

            migrationBuilder.DropTable(
                name: "TBPlantillas");

            migrationBuilder.DropTable(
                name: "TBReservasSalas");

            migrationBuilder.DropTable(
                name: "TBResponsivas");

            migrationBuilder.DropTable(
                name: "TBSolicitudes");

            migrationBuilder.DropTable(
                name: "TBSoliRechazada");

            migrationBuilder.DropTable(
                name: "TBSubtiposPermiso");

            migrationBuilder.DropTable(
                name: "TBTiposSolicitud");

            migrationBuilder.DropTable(
                name: "TBVacaciones");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "TBSalas");
        }
    }
}
