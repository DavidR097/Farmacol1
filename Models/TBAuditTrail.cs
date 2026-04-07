namespace Farmacol.Models;

public class TbAuditTrail
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Usuario { get; set; } = null!;
    public string Modulo { get; set; } = null!;
    public string Accion { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? EntidadId { get; set; }
    public string? Ip { get; set; }
}
