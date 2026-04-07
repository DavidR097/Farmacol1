namespace Farmacol.Models;

public class TbReservaSala
{
    public int Id { get; set; }
    public int SalaId { get; set; }
    public TbSala? Sala { get; set; }
    public int CC { get; set; }
    public string NombreSolicitante { get; set; } = null!;
    public string? Cargo { get; set; }
    public string? Area { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Motivo { get; set; } = null!;
    public string Estado { get; set; } = "Pendiente";
    public string? Observacion { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;
    public string? AtendidaPor { get; set; }
}
