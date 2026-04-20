namespace Farmacol.Models
{
    public class VacacionesViewModel
    {
        public int CC { get; set; }
        public string Nombre { get; set; } = "";
        public DateOnly FechaIngreso { get; set; }
        public int MesesTrabajados { get; set; }
        public decimal DiasAcumulados { get; set; }
        public decimal DiasDisfrutados { get; set; }
        public decimal DiasDisponibles { get; set; }
        public bool EsPlanta { get; set; }
        public string Mensaje { get; set; } = "";
    }
}
