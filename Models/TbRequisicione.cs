using System.ComponentModel.DataAnnotations;

namespace Farmacol.Models;

public partial class TbRequisicione
{
    [Key]
    public int Id { get; set; }                     

    public string NoRequisicion { get; set; } = null!;
    public DateOnly? FechaSolicitud { get; set; }
    public string? PosicionRequerida { get; set; }
    public string? GerenciaSolicitante { get; set; }
    public string? NombreSolicitante { get; set; }
    public string? CargoSolicitante { get; set; }
    public string? Firma { get; set; }
    public string? TipoContrato { get; set; }
    public string? DedicacionLaboral { get; set; }
    public string? MotivoVacante { get; set; }
    public string? ReemplazaA { get; set; }
    public string? FormacionAcademica { get; set; }
    public string? OtrosEstudios { get; set; }
    public string? IdiomaExtranjero { get; set; }
    public bool OfimaticaBasica { get; set; } = false;
    public bool OfimaticaIntermedia { get; set; } = false;
    public bool OfimaticaAvanzada { get; set; } = false;
    public bool SAP { get; set; } = false;
    public string? OtroConocimiento { get; set; }   
    public string? AprobGerenciaGen { get; set; }
    public string? AprobCH { get; set; }
    public string? AprobCHMex { get; set; }
    public string? Observaciones { get; set; }
    public DateOnly? FechaIngreso { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public string? CreadoPor { get; set; }
}