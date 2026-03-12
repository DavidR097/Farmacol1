namespace Farmacol.Models
{
    public partial class Tbnotificacione
    {
        public int IdNotificacion { get; set; }
        public string UsuarioDestino { get; set; } = null!;
        public string Mensaje { get; set; } = null!;
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int? IdSolicitud { get; set; }
    }
}