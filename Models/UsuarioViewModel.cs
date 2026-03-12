namespace Farmacol.Models
{
    public class UsuarioViewModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Rol { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Cargo { get; set; } = "";
        public int Cedula { get; set; }
    }
}