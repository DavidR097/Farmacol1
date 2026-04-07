using Farmacol.Models;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Services;

public class DelegacionService
{
    private readonly Farmacol1Context _context;

    public DelegacionService(Farmacol1Context context)
    {
        _context = context;
    }

    // ── Verificaciones ────────────────────────────────────────────────────

    public async Task<bool> EstaInhabilitado(string cargo, string area)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return await _context.TbDelegaciones.AnyAsync(d =>
            d.Activa &&
            d.FechaInicio <= hoy &&
            d.FechaFin >= hoy &&
            d.Cargo != null && d.Cargo.ToLower() == cargo.ToLower() &&
            d.Area != null && d.Area.ToLower() == area.ToLower());
    }

    public async Task<bool> EstaInhabilitadoPorCC(int cc)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        return await _context.TbDelegaciones.AnyAsync(d =>
            d.Activa &&
            d.CC == cc &&
            d.FechaInicio <= hoy &&
            d.FechaFin >= hoy);
    }

    // ── Crear delegación ──────────────────────────────────────────────────

    public async Task CrearDelegacion(int cc, string nombre, string cargo,
        string area, string motivo, DateOnly inicio, DateOnly fin, string creadaPor)
    {
        // Desactivar delegaciones anteriores del mismo CC
        var anteriores = await _context.TbDelegaciones
            .Where(d => d.CC == cc && d.Activa)
            .ToListAsync();
        anteriores.ForEach(d => d.Activa = false);

        // Calcular el aprobador original que usará el flujo para este cargo/área
        // Ej: cargo="Gerente Ventas", area="Ventas" → aprobadorOriginal="Gerente Ventas"
        var aprobadorOriginal = cargo.Trim().StartsWith("Jefe", StringComparison.OrdinalIgnoreCase)
            ? $"Jefe {area}"
            : $"Gerente {area}";

        _context.TbDelegaciones.Add(new TbDelegacion
        {
            CC = cc,
            Nombre = nombre,
            Cargo = cargo,
            Area = area,
            Motivo = motivo,
            FechaInicio = inicio,
            FechaFin = fin,
            Activa = true,
            CreadaPor = creadaPor,
            FechaCreacion = DateTime.Now,
            AprobadorOriginal = aprobadorOriginal   // ← guardamos para restaurar
        });

        await _context.SaveChangesAsync();
        await RedirigirSolicitudesPendientes(cargo, area, aprobadorOriginal);
    }

    // ── Redirigir pendientes al Gerente General ───────────────────────────

    private async Task RedirigirSolicitudesPendientes(string cargo, string area, string aprobadorOriginal)
    {
        var solicitudes = await _context.Tbsolicitudes
            .Where(s => s.Estado == "En proceso")
            .ToListAsync();

        foreach (var sol in solicitudes)
        {
            var paso = sol.PasoActual ?? 1;
            bool modificada = false;

            if (ApuntaA(sol.Paso1Aprobador, cargo, area)
                && sol.Paso1Estado != "Aprobado" && sol.Paso1Estado != "Rechazado")
            {
                sol.Paso1Aprobador = "Gerente General";
                if (paso == 1)
                    sol.EtapaAprobacion = $"Pendiente: Gerente General (delegación de {aprobadorOriginal})";
                modificada = true;
            }

            if (ApuntaA(sol.Paso2Aprobador, cargo, area)
                && sol.Paso2Estado != "Aprobado" && sol.Paso2Estado != "Rechazado")
            {
                sol.Paso2Aprobador = "Gerente General";
                if (paso == 2)
                    sol.EtapaAprobacion = $"Pendiente: Gerente General (delegación de {aprobadorOriginal})";
                modificada = true;
            }

            if (ApuntaA(sol.Paso3Aprobador, cargo, area)
                && sol.Paso3Estado != "Aprobado" && sol.Paso3Estado != "Rechazado")
            {
                sol.Paso3Aprobador = "Gerente General";
                if (paso == 3)
                    sol.EtapaAprobacion = $"Pendiente: Gerente General (delegación de {aprobadorOriginal})";
                modificada = true;
            }

            if (modificada) _context.Update(sol);
        }

        await _context.SaveChangesAsync();
    }

    // ── Cancelar delegación manualmente (Admin/RRHH) ──────────────────────

    public async Task CancelarDelegacion(int id)
    {
        var delegacion = await _context.TbDelegaciones.FindAsync(id);
        if (delegacion == null) return;

        delegacion.Activa = false;
        await _context.SaveChangesAsync();

        // Restaurar solicitudes si tiene aprobador original guardado
        if (!string.IsNullOrEmpty(delegacion.AprobadorOriginal))
            await RestaurarSolicitudes(delegacion.AprobadorOriginal);
    }

    // ── Desactivar delegaciones vencidas y restaurar flujo ────────────────

    public async Task DesactivarVencidas()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var vencidas = await _context.TbDelegaciones
            .Where(d => d.Activa && d.FechaFin < hoy)
            .ToListAsync();

        foreach (var d in vencidas)
        {
            d.Activa = false;

            var aprobadorOriginal = d.AprobadorOriginal
                ?? (d.Cargo!.StartsWith("Jefe", StringComparison.OrdinalIgnoreCase)
                    ? $"Jefe {d.Area}"
                    : $"Gerente {d.Area}");

            await RestaurarSolicitudes(aprobadorOriginal);
        }

        await _context.SaveChangesAsync();
    }

    // ── Restaurar solicitudes al aprobador original ───────────────────────

    private async Task RestaurarSolicitudes(string aprobadorOriginal)
    {
        var solicitudes = await _context.Tbsolicitudes
            .Where(s => s.Estado == "En proceso")
            .ToListAsync();

        foreach (var sol in solicitudes)
        {
            var paso = sol.PasoActual ?? 1;
            bool modificada = false;

            // Restaurar cualquier paso que haya sido redirigido a GG por delegación
            // y que corresponda a este aprobador original
            if (sol.Paso1Aprobador == "Gerente General"
                && sol.Paso1Estado != "Aprobado" && sol.Paso1Estado != "Rechazado"
                && sol.EtapaAprobacion != null
                && sol.EtapaAprobacion.Contains($"delegación de {aprobadorOriginal}"))
            {
                sol.Paso1Aprobador = aprobadorOriginal;
                if (paso == 1)
                    sol.EtapaAprobacion = $"Pendiente: {aprobadorOriginal}";
                modificada = true;
            }

            if (sol.Paso2Aprobador == "Gerente General"
                && sol.Paso2Estado != "Aprobado" && sol.Paso2Estado != "Rechazado"
                && sol.EtapaAprobacion != null
                && sol.EtapaAprobacion.Contains($"delegación de {aprobadorOriginal}"))
            {
                sol.Paso2Aprobador = aprobadorOriginal;
                if (paso == 2)
                    sol.EtapaAprobacion = $"Pendiente: {aprobadorOriginal}";
                modificada = true;
            }

            if (sol.Paso3Aprobador == "Gerente General"
                && sol.Paso3Estado != "Aprobado" && sol.Paso3Estado != "Rechazado"
                && sol.EtapaAprobacion != null
                && sol.EtapaAprobacion.Contains($"delegación de {aprobadorOriginal}"))
            {
                sol.Paso3Aprobador = aprobadorOriginal;
                if (paso == 3)
                    sol.EtapaAprobacion = $"Pendiente: {aprobadorOriginal}";
                modificada = true;
            }

            if (modificada) _context.Update(sol);
        }

        await _context.SaveChangesAsync();
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static bool ApuntaA(string? aprobador, string cargo, string area)
    {
        if (string.IsNullOrEmpty(aprobador)) return false;
        var ap = aprobador.Trim().ToLower();
        var cg = cargo.Trim().ToLower();
        var ar = area.Trim().ToLower();

        if (ap == cg) return true;

        var partes = ap.Split(' ', 2);
        if (partes.Length == 2)
        {
            var nivelCg = cg.Split(' ')[0];
            return string.Equals(partes[0], nivelCg, StringComparison.OrdinalIgnoreCase)
                && string.Equals(partes[1], ar, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
