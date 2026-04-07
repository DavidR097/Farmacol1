using System.ComponentModel.DataAnnotations;

namespace Farmacol.Models;

public class TbpersonalRetirado
{
    [Key]
    public int CC { get; set; }
    public string? NombreColaborador { get; set; }
    public string? Cargo { get; set; }
    public string? Area { get; set; }
    public string? CorreoCorporativo { get; set; }
    public string? UsuarioCorporativo { get; set; }
    public DateTime FechaRetiro { get; set; } = DateTime.Now;
    public string? MotivoRetiro { get; set; }
}