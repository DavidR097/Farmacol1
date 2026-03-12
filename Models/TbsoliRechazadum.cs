using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models;

public partial class TbsoliRechazadum
{
    public int IdSolicitud { get; set; }
    public string Nombre { get; set; } = null!;

    [Column("Cédula")]
    public int CC { get; set; }

    public string TipoSolicitud { get; set; } = null!;
    public string Motivo { get; set; } = null!;
    public string? Observaciones { get; set; }
    public string? Anexos { get; set; }
    public DateOnly FechaSolicitud { get; set; }
}