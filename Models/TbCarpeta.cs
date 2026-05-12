using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models;

public class TbCarpeta
{
    [Key]
    public int Id { get; set; }

    public int CC { get; set; }

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public string Modulo { get; set; } = "RRHH";

    public int? CarpetaPadreId { get; set; }

    [ForeignKey(nameof(CarpetaPadreId))]
    public TbCarpeta? CarpetaPadre { get; set; }

    public ICollection<TbCarpeta> SubCarpetas { get; set; } = new List<TbCarpeta>();

    public ICollection<TbExpediente> Expedientes { get; set; } = new List<TbExpediente>();

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public string CreadoPor { get; set; } = string.Empty;
}
