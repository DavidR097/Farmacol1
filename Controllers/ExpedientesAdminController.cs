using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH,Gerente,Jefe,Coordinador,Asistente")]
public class ExpedientesAdminController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _email;

    public ExpedientesAdminController(Farmacol1Context context,
        IWebHostEnvironment env, EmailService email)
    {
        _context = context;
        _env = env;
        _email = email;
    }

    // ── DETALLE: todos los documentos de un empleado ──────────────────────
    public async Task<IActionResult> Detalle(int cc)
    {
        var personal = await _context.Tbpersonals.FindAsync(cc);
        if (personal == null) return NotFound();

        var docs = await _context.TbExpedientes
            .Where(d => d.CC == cc)
            .OrderByDescending(d => d.FechaSubida)
            .ToListAsync();

        // Equipos TI vinculados
        var equipos = await _context.Tbinventarios
            .Where(e => e.CC == cc).ToListAsync();

        ViewBag.Personal = personal;
        ViewBag.Equipos = equipos;
        return View(docs);
    }

    // ── SUBIR: agregar documento ──────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subir(int cc, IFormFile archivo,
        string? tipoDocumento, string? nombrePersonalizado, string modulo = "RRHH")
    {
        if (archivo == null || Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
        {
            TempData["Error"] = "Solo se permiten archivos PDF.";
            return RedirectToAction(nameof(Detalle), new { cc });
        }

        var carpeta = Path.Combine(_env.WebRootPath, "expedientes",
            modulo.ToLower(), cc.ToString());
        Directory.CreateDirectory(carpeta);
        var nombre = $"{DateTime.Now.Ticks}_{Path.GetFileName(archivo.FileName)}";
        using var s = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create);
        await archivo.CopyToAsync(s);

        var nombreDoc = string.IsNullOrWhiteSpace(nombrePersonalizado)
            ? Path.GetFileNameWithoutExtension(archivo.FileName) : nombrePersonalizado;

        _context.TbExpedientes.Add(new TbExpediente
        {
            CC = cc,
            Modulo = modulo,
            NombreArchivo = nombreDoc,
            TipoDocumento = tipoDocumento,
            RutaArchivo = $"/expedientes/{modulo.ToLower()}/{cc}/{nombre}",
            FechaSubida = DateTime.Now,
            SubidoPor = User.Identity?.Name ?? "",
            Visible = true
        });
        await _context.SaveChangesAsync();

        // Notificar al empleado por correo
        try { await _email.NotificarDocumentoSubidoAsync(cc, nombreDoc, modulo); } catch { }

        TempData["Exito"] = "✅ Documento subido correctamente.";
        return RedirectToAction(nameof(Detalle), new { cc });
    }

    // ── TOGGLE VISIBLE: mostrar/ocultar al usuario ────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisible(int id)
    {
        var doc = await _context.TbExpedientes.FindAsync(id);
        if (doc == null) return NotFound();
        doc.Visible = !doc.Visible;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Detalle), new { cc = doc.CC });
    }

    // ── ELIMINAR ──────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var doc = await _context.TbExpedientes.FindAsync(id);
        if (doc == null) return NotFound();
        var cc = doc.CC;
        var full = Path.Combine(_env.WebRootPath,
            doc.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        _context.TbExpedientes.Remove(doc);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Documento eliminado.";
        return RedirectToAction(nameof(Detalle), new { cc });
    }

    // ── VER PDF ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Ver(int id)
    {
        var doc = await _context.TbExpedientes.FindAsync(id);
        if (doc == null) return NotFound();
        var full = Path.Combine(_env.WebRootPath,
            doc.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(full)) return NotFound();
        return PhysicalFile(full, "application/pdf");
    }
}
