using System;
using System.Collections.Generic;

namespace Farmacol.Models;

public partial class Tbvacacione
{
    public int IdVacación { get; set; }

    public string Nombre { get; set; } = null!;

    public string CC { get; set; } = null!;

    public string Cargo { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public int TotalDías { get; set; }

    public DateOnly FechaSolicitud { get; set; }

    public string? Observaciones { get; set; }

    public string? Anexos { get; set; }
}
