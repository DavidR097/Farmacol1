using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models;

public partial class Tbpersonal
{
    [Key]
    public int CC { get; set; }

    public string? ExpedicionCiudad { get; set; }
    public string? CiudadTrabajo { get; set; }
    public string? NombreColaborador { get; set; }
    public string? Cargo { get; set; }
    public string? CodCeco { get; set; }
    public string? NombreCentroCostos { get; set; }
    public string? Area { get; set; }
    public string? Gerencia { get; set; }
    public DateOnly? FechaIngreso { get; set; }
    public DateOnly? VencimientoPeriodoPrueba { get; set; }
    public int? AniosAntiguedad { get; set; }
    public int? MesesAntiguedad { get; set; }
    public decimal? SalarioEnero2020 { get; set; }
    public decimal? SalarioFeb2020 { get; set; }
    public decimal? SalarioFeb2021 { get; set; }
    public decimal? SalarioFeb2022 { get; set; }
    public decimal? SalarioFeb2023 { get; set; }
    public decimal? SalarioEneFeb2024 { get; set; }
    public decimal? SalarioMar2024 { get; set; }
    public decimal? SalarioFeb2025 { get; set; }
    public decimal? SalarioFeb2026 { get; set; }
    public decimal? AuxAlimentacion2020 { get; set; }
    public decimal? AuxAlimentacion2021 { get; set; }
    public decimal? AuxAlimentacion2022 { get; set; }
    public decimal? AuxAlimentacion2023 { get; set; }
    public decimal? AuxGasolina2021 { get; set; }
    public decimal? AuxGasolina20222023 { get; set; }
    public decimal? AuxRodamiento { get; set; }
    public decimal? AuxRodamiento20222023 { get; set; }
    public decimal? BaseIncentivo20222023 { get; set; }
    public decimal? MedicinaPrepagada { get; set; }
    public decimal? LlaveSnacBebidas { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public int? Edad { get; set; }
    public int? MesesEdad { get; set; }
    public string? MesNacimiento { get; set; }
    public string? Generacion { get; set; }
    public string? Genero { get; set; }
    public string? CiudadNacimiento { get; set; }
    public string? EstadoCivil { get; set; }
    public string? CorreoPersonal { get; set; }
    public string? Contacto { get; set; }
    public string? DireccionResidencia { get; set; }
    public string? Barrio { get; set; }
    public string? Rh { get; set; }
    public string? ContactoEmergencia { get; set; }
    public string? Parentesco { get; set; }
    public string? TelefonoEmergencia { get; set; }
    public string? TipoContrato { get; set; }
    public string? Eps { get; set; }
    public string? FondoPensiones { get; set; }
    public string? FondoCesantias { get; set; }
    public string? CajaCompensacion { get; set; }
    public string? Arl { get; set; }
    public string? TipoCuenta { get; set; }
    public string? NumeroCuenta { get; set; }
    public string? Banco { get; set; }
    public string? TallaCamisa { get; set; }
    public string? Grupo { get; set; }
    public string? Concepto { get; set; }

    public string? CorreoCorporativo { get; set; }
    public string? UsuarioCorporativo { get; set; }


    [NotMapped] public string? Nombre => NombreColaborador;
    [NotMapped] public string? NúmeroCel => Contacto;
    [NotMapped] public string? CelularEmergencia => TelefonoEmergencia;
    [NotMapped] public string? CajaCompensación => CajaCompensacion;
    [NotMapped] public string? Tpcuenta => TipoCuenta;
    [NotMapped] public string? NoCuenta => NumeroCuenta;
    [NotMapped] public string? ExpediciónCiudad => ExpedicionCiudad;
}