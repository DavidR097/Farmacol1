using System.Collections.Generic;

namespace Farmacol.Models
{
    public class TbresponsivaDetailsViewModel
    {
        public Tbresponsiva Responsiva { get; set; } = null!;
        public List<Tbinventario> Equipos { get; set; } = new List<Tbinventario>();
    }
}
