using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize]
public class SalasController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly EmailService _email;
    private readonly AuditService _audit;

    public SalasController(Farmacol1Context context, EmailService email, AuditService audit)
    {
        _context = context;
        _email = email;
        _audit = audit;
    }

    private bool EsGestor() => User.IsInRole("Recepcionista") || User.IsInRole("Administrador");

    // ── INDEX (Calendario semanal) ─────────────────────────────────────
    public async Task<IActionResult> Index(DateOnly? fecha = null)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var lunes = fecha ?? hoy.AddDays(-(int)hoy.DayOfWeek); // Domingo = 0

        // Asegurar que siempre empiece en lunes
        if (lunes.DayOfWeek != DayOfWeek.Monday)
            lunes = lunes.AddDays(-(int)lunes.DayOfWeek + 1);

        var finSemana = lunes.AddDays(6);

        // Cargar salas activas
        var salas = await _context.TbSalas
            .Where(s => s.Activa)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        // Cargar reservas de la semana
        var reservas = await _context.TbReservasSalas
            .Include(r => r.Sala)
            .Where(r => r.Fecha >= lunes && r.Fecha <= finSemana)
            .ToListAsync();

        ViewBag.Semana = Enumerable.Range(0, 7)
            .Select(i => lunes.AddDays(i))
            .ToList();

        ViewBag.Salas = salas;
        ViewBag.Reservas = reservas;
        ViewBag.Hoy = hoy;
        ViewBag.Lunes = lunes;
        ViewBag.EsGestor = EsGestor();

        return View();
    }
    // ── SOLICITAR GET ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Solicitar(int salaId = 0, DateOnly? fecha = null)
    {
        // Cargar todas las salas activas
        var salas = await _context.TbSalas
            .Where(s => s.Activa == true)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        var personal = await ObtenerPersonalActual();
        if (personal == null)
            return Forbid();

        ViewBag.Salas = salas;
        ViewBag.SalaSeleccionada = salaId;
        ViewBag.Fecha = fecha ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        ViewBag.HoraInicio = "08:00";
        ViewBag.HoraFin = "09:00";

        // Debug temporal
        System.Diagnostics.Debug.WriteLine($"Solicitar GET - Salas cargadas: {salas.Count}");

        return View("Solicitar");
    }

    // ── SOLICITAR POST - Guardar reserva automáticamente ───────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(int salaId, DateOnly fecha,
        TimeOnly horaInicio, TimeOnly horaFin, string? motivo)
    {
        var personal = await ObtenerPersonalActual();
        if (personal == null) return Forbid();

        // Verificar conflicto con reservas ya aprobadas
        bool hayConflicto = await _context.TbReservasSalas
            .AnyAsync(r => r.SalaId == salaId &&
                           r.Fecha == fecha &&
                           r.Estado == "Aprobada" &&
                           r.HoraInicio < horaFin && r.HoraFin > horaInicio);

        if (hayConflicto)
        {
            TempData["Error"] = "La sala ya está reservada en ese horario.";
            return RedirectToAction(nameof(Index));
        }

        var reserva = new TbReservaSala
        {
            SalaId = salaId,
            CC = personal.CC,
            NombreSolicitante = personal.NombreColaborador ?? User.Identity?.Name ?? "Usuario",
            Cargo = personal.Cargo ?? "",
            Area = personal.Area ?? "",
            Fecha = fecha,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Motivo = motivo ?? "Reserva sin motivo especificado",
            Estado = "Aprobada",                    // ← Automático
            FechaSolicitud = DateTime.Now,
            AtendidaPor = "Sistema (auto-aprobada)"
        };

        _context.TbReservasSalas.Add(reserva);
        await _context.SaveChangesAsync();

        await NotificarReservacion(reserva);

        TempData["Exito"] = $"✅ Reserva confirmada para el {fecha:dd/MM/yyyy} de {horaInicio:HH:mm} a {horaFin:HH:mm}.";
        return RedirectToAction(nameof(Index));
    }

    // ── CANCELAR (propia reserva) ─────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var reserva = await _context.TbReservasSalas
            .Include(r => r.Sala)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva == null)
        {
            TempData["Error"] = "❌ Reserva no encontrada.";
            return RedirectToAction(nameof(MisReservas));
        }

        var personal = await ObtenerPersonalActual();
        if (personal == null || personal.CC != reserva.CC)
        {
            TempData["Error"] = "❌ No tienes permiso para cancelar esta reserva.";
            return RedirectToAction(nameof(MisReservas));
        }

        // Validar si ya está cancelada
        if (reserva.Estado == "Cancelada")
        {
            TempData["Error"] = "⚠️ Esta reserva ya fue cancelada anteriormente.";
            return RedirectToAction(nameof(MisReservas));
        }

        // Validar si ya pasó la hora de inicio (no la hora de fin)
        if (reserva.Fecha < DateOnly.FromDateTime(DateTime.Today) ||
            (reserva.Fecha == DateOnly.FromDateTime(DateTime.Today) && reserva.HoraInicio <= TimeOnly.FromDateTime(DateTime.Now)))
        {
            TempData["Error"] = "❌ No puedes cancelar una reserva que ya ha comenzado.";
            return RedirectToAction(nameof(MisReservas));
        }

        // Cancelar la reserva
        reserva.Estado = "Cancelada";
        reserva.AtendidaPor = $"Cancelada por {personal.NombreColaborador}";
        await _context.SaveChangesAsync();

        TempData["Exito"] = $"✅ Reserva de {reserva.Sala?.Nombre} para el {reserva.Fecha:dd/MM/yyyy} cancelada correctamente.";
        return RedirectToAction(nameof(MisReservas));
    }

    // ── MIS RESERVAS ──────────────────────────────────────────────────
    public async Task<IActionResult> MisReservas()
    {
        var personal = await ObtenerPersonalActual();
        if (personal == null) return View(new List<TbReservaSala>());

        var misReservas = await _context.TbReservasSalas
            .Include(r => r.Sala)
            .Where(r => r.CC == personal.CC)
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.HoraInicio)
            .ToListAsync();

        return View(misReservas);
    }

    // ── GESTIÓN (solo Recepcionista + Admin) ─────────────────────────
    [Authorize(Roles = "Recepcionista,Administrador")]
    public async Task<IActionResult> Gestion(string? estado)
    {
        var query = _context.TbReservasSalas.Include(r => r.Sala).AsQueryable();
        if (!string.IsNullOrEmpty(estado))
            query = query.Where(r => r.Estado == estado);

        var reservas = await query.OrderByDescending(r => r.Fecha).ToListAsync();
        ViewBag.Estado = estado;
        return View(reservas);
    }

    // Helpers privados
    private async Task<Tbpersonal?> ObtenerPersonalActual()
    {
        var userName = User.Identity?.Name ?? "";
        return await _context.Tbpersonals.FirstOrDefaultAsync(p =>
            p.UsuarioCorporativo == userName || p.CorreoCorporativo == userName);
    }

    private async Task NotificarReservacion(TbReservaSala reserva)
    {
        var receptores = await _context.Tbpersonals
            .Where(p => p.Cargo == "Recepcionista" || p.Cargo == "Administrador")
            .Select(p => p.CorreoCorporativo ?? p.UsuarioCorporativo)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToListAsync();

        foreach (var correo in receptores)
        {
            try
            {
                await _email.EnviarAsync(correo,
                    $"[Farmacol] Nueva reserva: {reserva.Sala?.Nombre}",
                    $"<p><strong>{reserva.NombreSolicitante}</strong> reservó <strong>{reserva.Sala?.Nombre}</strong> el {reserva.Fecha:dd/MM/yyyy} de {reserva.HoraInicio} a {reserva.HoraFin}.</p>");
            }
            catch { }
        }
    }
}