namespace Farmacol.Models;

public class TbPlantilla
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string TipoDocumento { get; set; } = null!;
    public string Modulo { get; set; } = null!;
    public string RutaArchivo { get; set; } = null!;
    public DateTime FechaSubida { get; set; } = DateTime.Now;
    public string? SubidaPor { get; set; }
    public bool Activa { get; set; } = true;
}
