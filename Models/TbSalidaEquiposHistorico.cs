using System;

namespace Farmacol.Models
{
    public class TbSalidaEquiposHistorico
    {
        public int Id { get; set; }
        public string SalidaId { get; set; } = null!;
        public string Accion { get; set; } = null!; // Creada, Aprobada, Rechazada, Finalizada
        public string Usuario { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public string? Observacion { get; set; }
    }
}
