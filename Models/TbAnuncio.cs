using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farmacol.Models
{
    public class TbAnuncio
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Mensaje { get; set; } = string.Empty;

        [StringLength(50)]
        public string Tipo { get; set; } = "Info"; 

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaExpiracion { get; set; }

        public bool Activo { get; set; } = true;

        [StringLength(100)]
        public string CreadoPor { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Imagen { get; set; }

        // Dimensiones de la imagen (opcional, se guardan al subir)
        public int? Width { get; set; }

        public int? Height { get; set; }
    }
}
