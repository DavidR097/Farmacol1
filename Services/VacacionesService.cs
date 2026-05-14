using Farmacol.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Farmacol.Services
{
    public class VacacionesService
    {
        private readonly Farmacol1Context _context;

        public VacacionesService(Farmacol1Context context)
        {
            _context = context;
        }

        public async Task<VacacionesViewModel> CalcularVacacionesAsync(int cc)
        {
            var personal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CC == cc);

            if (personal?.FechaIngreso == null)
                return new VacacionesViewModel
                {
                    CC = cc,
                    DiasDisponibles = 0,
                    Mensaje = "Fecha de ingreso no registrada"
                };

            var fechaIngreso = personal.FechaIngreso.Value;
            var hoy = DateOnly.FromDateTime(DateTime.Now);

            int totalDias = hoy.DayNumber - fechaIngreso.DayNumber;
            if (totalDias < 0) totalDias = 0;

            const decimal tasaDiaria = 1.25m / 30m;  
            decimal diasAcumulados = totalDias * tasaDiaria;

            decimal diasDisfrutados = await _context.Tbsolicitudes
                .Where(s => s.CC == cc
                        && s.TipoSolicitud == "Vacaciones"
                        && s.Estado == "Aprobada")
                .SumAsync(s => s.TotalDias ?? 0m);

            decimal diasDisponibles = diasAcumulados - diasDisfrutados;

            bool esPlanta = personal.Area?.Trim().Equals("Planta", StringComparison.OrdinalIgnoreCase) ?? false;

            Console.WriteLine($"[DEBUG] CC={cc}, FechaIngreso={fechaIngreso}, Hoy={hoy}, TotalDias={totalDias}, " +
                            $"TasaDiaria={tasaDiaria}, DiasAcumulados={diasAcumulados}, DiasDisfrutados={diasDisfrutados}, " +
                            $"DiasDisponibles={diasDisponibles}");

            return new VacacionesViewModel
            {
                CC = cc,
                Nombre = personal.NombreColaborador ?? "",
                FechaIngreso = fechaIngreso,
                MesesTrabajados = (int)Math.Floor(totalDias / 30.0), 
                DiasAcumulados = Math.Round(diasAcumulados, 2),
                DiasDisfrutados = Math.Round(diasDisfrutados, 2),
                DiasDisponibles = Math.Max(0, Math.Round(diasDisponibles, 2)),
                EsPlanta = esPlanta,
                Mensaje = esPlanta
                    ? "Se incluyen sábados como días vacacionales (jornada Planta)"
                    : "Solo se cuentan días hábiles (lunes-viernes)"
            };
        }   
}
}