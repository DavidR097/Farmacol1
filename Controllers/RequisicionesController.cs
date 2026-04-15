using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH,Gerente")]
public class RequisicionesController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly EmailService _email;
    private readonly NotificacionService _notif;

    public RequisicionesController(
        Farmacol1Context context,
        EmailService email,
        NotificacionService notif)
    {
        _context = context;
        _email = email;
        _notif = notif;
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

        string nuevoNo = "RP-00001";
        if (ultimo != null && !string.IsNullOrEmpty(ultimo.NoRequisicion))
        {
            var num = int.TryParse(ultimo.NoRequisicion.Replace("RP-", ""), out int n) ? n : 0;
            nuevoNo = $"RP-{(num + 1):D5}";
        }

        ViewBag.NoRequisicionSugerido = nuevoNo;

        var model = new TbRequisicione
        {
            FechaSolicitud = DateOnly.FromDateTime(DateTime.Today),
            Estado = "En proceso"
        };

        return View(model);
    }

    // CREATE POST - Crea + Inicia flujo + Notificaciones
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TbRequisicione tbRequisicione)
    {
        if (ModelState.IsValid)
        {
            // Número de requisición
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

            tbRequisicione.AprobGerenciaGen = "Gerencia General";   
            tbRequisicione.AprobCH = "RRHH";
            tbRequisicione.AprobCHMex = "Directivo";          

            _context.Add(tbRequisicione);
            await _context.SaveChangesAsync();

            // ====================== NOTIFICACIONES ======================

            await EnviarNotificacionFlujo(tbRequisicione, "Gerencia General", "Directivo");

            await EnviarNotificacionFlujo(tbRequisicione, "RRHH", "RRHH");

            await EnviarNotificacionFlujo(tbRequisicione, "CH MXN", "Directivo");

            TempData["Exito"] = $"✅ Requisición {tbRequisicione.NoRequisicion} creada y enviada a Gerencia General.";
            return RedirectToAction(nameof(Index));
        }

        return View(tbRequisicione);
    }

    // Helper para enviar notificaciones (interna + correo)
    private async Task EnviarNotificacionFlujo(TbRequisicione req, string paso, string rol)
    {
        string mensaje = $"Nueva requisición de personal #{req.NoRequisicion} requiere tu aprobación como {paso}.";

        // Notificación interna (a todos los usuarios con ese rol)
        var usuarios = await _context.Tbpersonals
            .Where(p => p.Cargo != null && p.Cargo.Contains(rol, StringComparison.OrdinalIgnoreCase))
            .ToListAsync();

        foreach (var u in usuarios)
        {
            var destino = u.UsuarioCorporativo ?? u.CorreoCorporativo ?? "";
            if (!string.IsNullOrEmpty(destino))
            {
                await _notif.CrearNotificacion(destino, mensaje, req.Id);
            }
        }

        // Correo a RRHH / Gerente CH (ajusta según necesites)
        if (rol == "RRHH" || rol == "Directivo")
        {
            try
            {
                await _email.EnviarAsync(
                    destinatario: "seochoa@chinoin.com",
                    asunto: $"Nueva Requisición - {req.NoRequisicion}",
                    cuerpoHtml: $"<p><strong>Nueva requisición requiere aprobación:</strong></p>" +
                                $"<p><strong>No.:</strong> {req.NoRequisicion}</p>" +
                                $"<p><strong>Posición:</strong> {req.PosicionRequerida}</p>" +
                                $"<p><strong>Solicitante:</strong> {req.NombreSolicitante}</p>" +
                                $"<p>Paso actual: {paso}</p>"
                );
            }
            catch { }
        }
    }

    // Resto de métodos (Details, Edit, etc.)
    public async Task<IActionResult> Details(int id)
    {
        var requisicion = await _context.TbRequisiciones.FindAsync(id);
        if (requisicion == null) return NotFound();
        return View(requisicion);
    }

    // ... mantén tus métodos Edit si los necesitas
}