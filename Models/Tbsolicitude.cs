using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace Farmacol.Models;

public partial class Tbsolicitude
{
    public int IdSolicitud { get; set; }

    public int CC { get; set; }

    public string? Nombre { get; set; }

    public string? Cargo { get; set; }

    public string TipoSolicitud { get; set; } = null!;

    public TimeSpan? HoraInicio { get; set; }

    public TimeSpan? HoraFin { get; set; }

    public decimal? TotalHoras { get; set; }
    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string? JefeInmediato { get; set; }

    public string? CargoJinmediato { get; set; }

    public string? Motivo { get; set; }

    public DateOnly? FechaSolicitud { get; set; }

    public string? AprobJinmediato { get; set; }

    public string? AprobCh { get; set; }

    public string? Observaciones { get; set; }

    public string? Anexos { get; set; }

    public string? Estado { get; set; }

    public decimal? TotalDias { get; set; }

    public string? SubtipoPermiso { get; set; }

    public string? EtapaAprobacion { get; set; }

    public string? ObservacionJefe { get; set; }

    public string? ObservacionRRHH { get; set; }
    public string? Paso1Aprobador { get; set; }
    public string? Paso1Estado { get; set; }
    public string? Paso1Obs { get; set; }
    public string? Paso2Aprobador { get; set; }
    public string? Paso2Estado { get; set; }
    public string? Paso2Obs { get; set; }
    public string? Paso3Aprobador { get; set; }
    public string? Paso3Estado { get; set; }
    public string? Paso3Obs { get; set; }
    public int? TotalPasos { get; set; }
    public int? PasoActual { get; set; }
    public string? NivelSolicitante { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public string? TipoFlujo { get; set; }
    public string? DocumentoSolicitado { get; set; }
    public int? DiasEnDinero { get; set; }
    public DateOnly? FechaReposición { get; set; }
    //public int? DiasDisponibles { get; set; }

}
