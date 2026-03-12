using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models;

public partial class Tbinventario
{
    public int IdEquipo { get; set; }
    public string? Ubicación { get; set; }
    public string? Ubicación2 { get; set; }
    public string? Dispositivo { get; set; }
    public string? Modelo { get; set; }
    public string? Serie { get; set; }
    public string? Imei { get; set; }
    public string? Marca { get; set; }
    public string? Observación { get; set; }
    public string? Planta { get; set; }

    [Column("Cédula")]
    public int? CC { get; set; }

    public string? Anexo { get; set; }
}