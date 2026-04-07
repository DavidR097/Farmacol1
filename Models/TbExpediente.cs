namespace Farmacol.Models;

public class TbExpediente
{
    public int Id { get; set; }
    public int CC { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string? TipoDocumento { get; set; }
    public string RutaArchivo { get; set; } = null!;
    public string? Modulo { get; set; }   // "RRHH" o "TI"
    public bool Visible { get; set; } = true;  // ← el usuario puede verlo
    public DateTime FechaSubida { get; set; } = DateTime.Now;
    public string? SubidoPor { get; set; }
}
