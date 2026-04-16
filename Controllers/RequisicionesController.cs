using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH,Gerente,Jefe,Gerente Capital Humano,Directivo")]
public class RequisicionesController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly EmailService _email;
    private readonly UserManager<IdentityUser> _userManager;

    public RequisicionesController(Farmacol1Context context, 
                                    EmailService email, 
                                    UserManager<IdentityUser> userManager)
    {
        _context = context;
        _email = email;
        _userManager = userManager;
    }

    // INDEX
    public async Task<IActionResult> Index()
    {
        var requisiciones = await _context.TbRequisiciones
            .OrderByDescending(r => r.FechaSolicitud ?? DateOnly.MinValue)
            .ToListAsync();

        return View(requisiciones);
    }

    // CREATE GET
    public async Task<IActionResult> Create()
    {
        var ultimo = await _context.TbRequisiciones
            .OrderByDescending(r => r.NoRequisicion)
            .FirstOrDefaultAsync();

        string nuevoNoRequisicion = "RP-00001";
        if (ultimo != null && !string.IsNullOrEmpty(ultimo.NoRequisicion))
        {
            var num = int.TryParse(ultimo.NoRequisicion.Replace("RP-", ""), out int n) ? n : 0;
            nuevoNoRequisicion = $"RP-{(num + 1):D5}";
        }

        ViewBag.NoRequisicionSugerido = nuevoNoRequisicion;

        var model = new TbRequisicione
        {
            FechaSolicitud = DateOnly.FromDateTime(DateTime.Today)
        };

        return View(model);
    }

    // CREATE POST - Corregido con notificaciones dinámicas
    // CREATE POST - Notificaciones dinámicas por Cargo y Rol
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TbRequisicione tbRequisicione)
    {
        if (ModelState.IsValid)
        {
            // Generar No. Requisición
            if (string.IsNullOrEmpty(tbRequisicione.NoRequisicion))
            {
                var ultimo = await _context.TbRequisiciones
                    .OrderByDescending(r => r.NoRequisicion)
                    .FirstOrDefaultAsync();

                int siguiente = 1;
                if (ultimo != null && !string.IsNullOrEmpty(ultimo.NoRequisicion))
                {
                    var num = int.TryParse(ultimo.NoRequisicion.Replace("RP-", ""), out int n) ? n : 0;
                    siguiente = n + 1;
                }
                tbRequisicione.NoRequisicion = $"RP-{siguiente:D5}";
            }

            tbRequisicione.FechaCreacion = DateTime.Now;
            tbRequisicione.CreadoPor = User.Identity?.Name ?? "Sistema";
            tbRequisicione.Estado = "En proceso";

            _context.Add(tbRequisicione);
            await _context.SaveChangesAsync();

            // =============================================
            // NOTIFICACIONES POR CORREO - BÚSQUEDA ROBUSTA
            // =============================================
            try
            {
                // 1. Búsqueda por Cargo (más específico)
                var cargosClave = new[]
                {
                "Gerente Capital Humano",
                "Gerente General",
                "Gerente Compensaciones Y Beneficios",
            };

                var correosPorCargo = await _context.Tbpersonals
                    .Where(p => p.CorreoCorporativo != null &&
                                p.Cargo != null &&
                                cargosClave.Any(c => p.Cargo.Contains(c, StringComparison.OrdinalIgnoreCase)))
                    .Select(p => p.CorreoCorporativo!)
                    .Distinct()
                    .ToListAsync();

                // Combinar ambos (evitando duplicados)
                var correosANotificar = correosPorCargo.ToList();

                if (correosANotificar.Any())
                {
                    var asunto = $"Nueva Requisición de Personal - {tbRequisicione.NoRequisicion}";
                    var cuerpo = $@"
                    <p>Se ha creado una nueva requisición de personal en el sistema:</p>
                    <p><strong>No. Requisición:</strong> {tbRequisicione.NoRequisicion}</p>
                    <p><strong>Posición Requerida:</strong> {tbRequisicione.PosicionRequerida}</p>
                    <p><strong>Solicitante:</strong> {tbRequisicione.NombreSolicitante}</p>
                    <p><strong>Gerencia:</strong> {tbRequisicione.GerenciaSolicitante}</p>
                    <br>
                    <p>Por favor ingrese al sistema para revisar y dar seguimiento.</p>";

                    foreach (var correo in correosANotificar)
                    {
                        if (!string.IsNullOrWhiteSpace(correo))
                        {
                            await _email.EnviarAsync(
                                destinatario: correo,
                                asunto: asunto,
                                cuerpoHtml: cuerpo
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Solo logueamos el error, no interrumpimos el flujo
                Console.WriteLine($"Error enviando notificaciones de requisición: {ex.Message}");
            }

            TempData["Exito"] = $"✅ Requisición {tbRequisicione.NoRequisicion} creada correctamente.";
            return RedirectToAction("Gestionar", new { id = tbRequisicione.Id });
        }

        return View(tbRequisicione);
    }

    // GESTIONAR / VER REQUISICIÓN (Versión simplificada - solo datos + estado)
    public async Task<IActionResult> Gestionar(int id)
    {
        var req = await _context.TbRequisiciones.FindAsync(id);
        if (req == null) return NotFound();

        return View(req);
    }

    // DETAILS (opcional, puedes usarlo o redirigir a Gestionar)
    public async Task<IActionResult> Details(int id)
    {
        return RedirectToAction("Gestionar", new { id });
    }

    // EDIT (mantener si lo necesitas)
    public async Task<IActionResult> Edit(int id)
    {
        var requisicion = await _context.TbRequisiciones.FindAsync(id);
        if (requisicion == null) return NotFound();
        return View(requisicion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TbRequisicione tbRequisicione)
    {
        if (id != tbRequisicione.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbRequisicione);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Requisición actualizada.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TbRequisicioneExists(tbRequisicione.Id)) return NotFound();
                throw;
            }
        }
        return View(tbRequisicione);
    }

    private bool TbRequisicioneExists(int id)
    {
        return _context.TbRequisiciones.Any(e => e.Id == id);
    }
}