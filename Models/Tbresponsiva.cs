using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models;

public partial class Tbresponsiva
{
    [Key]
    [Column("Cédula")]
    public int CC { get; set; }

    public string? Equipo { get; set; }
    public string? Marca { get; set; }
    public string? Serie { get; set; }
    public string? Observación { get; set; }
    public string? Estado { get; set; }
}