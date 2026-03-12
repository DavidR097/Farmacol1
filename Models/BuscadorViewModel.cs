namespace Farmacol.Models
{
    public class BuscadorViewModel
    {
        public string? Termino { get; set; }
        public List<Tbinventario> Inventario { get; set; } = new();
        public List<Tbpersonal> Personal { get; set; } = new();
        public List<Tbresponsiva> Responsivas { get; set; } = new();
        public List<Tbvacacione> Vacaciones { get; set; } = new();
        public List<Tbsolicitude> Solicitudes { get; set; } = new();
        public List<TbsoliRechazadum> SolicitudesRechazadas { get; set; } = new();
    }
}