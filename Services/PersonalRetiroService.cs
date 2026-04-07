using Farmacol.Models;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Services;

public class PersonalRetiroService
{
    private readonly Farmacol1Context _context;

    public PersonalRetiroService(Farmacol1Context context)
    {
        _context = context;
    }

    /// <summary>
    /// Mueve el personal a TBPersonalRetirado y redirige sus solicitudes
    /// pendientes al Gerente General si era Jefe o Gerente.
    /// </summary>
    public async Task RetirarPersonal(Tbpersonal personal, string? motivo = null)
    {
        // 1. Mover a TBPersonalRetirado
        var yaExiste = await _context.TbpersonalRetirados
            .AnyAsync(r => r.CC == personal.CC);

        if (!yaExiste)
        {
            _context.TbpersonalRetirados.Add(new TbpersonalRetirado
            {
                CC = personal.CC,
                NombreColaborador = personal.NombreColaborador,
                Cargo = personal.Cargo,
                Area = personal.Area,
                CorreoCorporativo = personal.CorreoCorporativo,
                UsuarioCorporativo = personal.UsuarioCorporativo,
                FechaRetiro = DateTime.Now,
                MotivoRetiro = motivo ?? "Retiro desde sistema"
            });
            await _context.SaveChangesAsync();
        }

        // 2. Redirigir solicitudes si era Jefe o Gerente
        var cargo = (personal.Cargo ?? "").ToLower();
        if (cargo.StartsWith("jefe") || cargo.StartsWith("gerente"))
            await RedirigirSolicitudes(personal.Area ?? "");

        // 3. Eliminar — desacoplar el tracker antes de eliminar
        _context.ChangeTracker.Clear();
        var personalParaEliminar = await _context.Tbpersonals
            .FirstOrDefaultAsync(p => p.CC == personal.CC);

        if (personalParaEliminar != null)
        {
            _context.Tbpersonals.Remove(personalParaEliminar);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Redirige solicitudes "En proceso" que estaban esperando aprobación
    /// de un Jefe/Gerente del área al Gerente General.
    /// </summary>
    private async Task RedirigirSolicitudes(string area)
    {
        if (string.IsNullOrEmpty(area)) return;

        // Buscar Gerente General activo
        var gerenteGeneral = await _context.Tbpersonals
            .FirstOrDefaultAsync(p =>
                p.Cargo != null &&
                p.Cargo.ToLower() == "gerente general");

        var destinoFallback = "Gerente General";

        // Solicitudes En proceso donde el paso actual corresponde al área retirada
        var solicitudesPendientes = await _context.Tbsolicitudes
                .Where(s => s.Estado == "En proceso")
                .ToListAsync(); 


        foreach (var sol in solicitudesPendientes)
        {
            bool modificada = false;

            // Revisar cada paso para ver si alguno apunta al área del retirado
            if (sol.PasoActual == 1 &&
                sol.Paso1Estado == "Pendiente" &&
                ApuntaAlArea(sol.Paso1Aprobador, area))
            {
                sol.Paso1Aprobador = destinoFallback;
                modificada = true;
            }
            else if (sol.PasoActual == 2 &&
                     sol.Paso2Estado == "Pendiente" &&
                     ApuntaAlArea(sol.Paso2Aprobador, area))
            {
                sol.Paso2Aprobador = destinoFallback;
                modificada = true;
            }
            else if (sol.PasoActual == 3 &&
                     sol.Paso3Estado == "Pendiente" &&
                     ApuntaAlArea(sol.Paso3Aprobador, area))
            {
                sol.Paso3Aprobador = destinoFallback;
                modificada = true;
            }

            if (modificada)
            {
                sol.EtapaAprobacion =
                    $"Redirigida a Gerente General (aprobador original área {area} retirado)";
                _context.Update(sol);
            }
        }

        await _context.SaveChangesAsync();
    }

    private static bool ApuntaAlArea(string? aprobador, string area)
    {
        if (string.IsNullOrEmpty(aprobador)) return false;
        var ap = aprobador.ToLower();
        var ar = area.ToLower();
        // Ej: "Jefe Contraloría", "Gerente Planta" — contiene el área
        return ap.Contains(ar) ||
               (ap.StartsWith("jefe") && ar.Contains(ap.Replace("jefe ", ""))) ||
               (ap.StartsWith("gerente") && ar.Contains(ap.Replace("gerente ", "")));
    }
}