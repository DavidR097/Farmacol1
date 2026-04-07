using System.Collections.Generic;

namespace Farmacol.Models
{
    public class TbresponsivaIndexViewModel
    {
        public string Mode { get; set; } = "equipo"; // "equipo" or "cc"
        public IEnumerable<Tbinventario> Equipos { get; set; } = new List<Tbinventario>();
        public IEnumerable<TbresponsivaGroup> Grupos { get; set; } = new List<TbresponsivaGroup>();
    }

    public class TbresponsivaGroup
    {
        public int CC { get; set; }
        public string? Nombre { get; set; }
        public int Count { get; set; }
        public string? PrimerEquipo { get; set; }
        public string? PrimeraMarca { get; set; }
    }
}
