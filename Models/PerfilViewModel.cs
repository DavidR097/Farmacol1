namespace Farmacol.Models
{
    public class PerfilViewModel
    {
        public Tbpersonal Personal { get; set; } = null!;
        public VacacionesViewModel Vacaciones { get; set; } = null!;
        public string? FotoPerfil { get; set; }
    }
}
