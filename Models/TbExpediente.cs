using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models;

public class TbExpediente
{
    public int Id { get; set; }
    public int CC { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string? TipoDocumento { get; set; }
    public string RutaArchivo { get; set; } = null!;
    public string? Modulo { get; set; }   
    public bool Visible { get; set; } = true;  
    public DateTime FechaSubida { get; set; } = DateTime.Now;
    public string? SubidoPor { get; set; }
    public int? CarpetaId { get; set; }

    [ForeignKey(nameof(CarpetaId))]
    public TbCarpeta? Carpeta { get; set; }
}
