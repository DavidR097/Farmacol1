using System.ComponentModel.DataAnnotations;

namespace Farmacol.Models;

public class TbDelegacion
{
    public int Id { get; set; }
    public int CC { get; set; }
    public string? Nombre { get; set; }
    public string? Cargo { get; set; }
    public string? Area { get; set; }
    public string? Motivo { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public bool Activa { get; set; } = true;
    public string? CreadaPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public string? AprobadorOriginal { get; set; }
}