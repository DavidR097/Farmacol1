using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize]
public class MiExpedienteController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly IWebHostEnvironment _env;

    public MiExpedienteController(Farmacol1Context context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // ── INDEX: expediente propio del usuario ──────────────────────────────
    public async Task<IActionResult> Index()
    {
        var userName = User.Identity?.Name ?? "";

        var personal = await _context.Tbpersonals
            .FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == userName ||
                p.CorreoCorporativo == userName);

        if (personal == null)
        {
            ViewBag.Error = "No encontramos tu perfil de personal. Contacta a RRHH.";
            return View(new List<TbExpediente>());
        }

        // Solo documentos marcados como visibles para el usuario
        var docs = await _context.TbExpedientes
            .Where(d => d.CC == personal.CC && d.Visible)
            .OrderByDescending(d => d.FechaSubida)
            .ToListAsync();

        ViewBag.Personal = personal;
        return View(docs);
    }

    // ── VER: descarga/abre el PDF ─────────────────────────────────────────
    public async Task<IActionResult> Ver(int id)
    {
        var userName = User.Identity?.Name ?? "";
        var personal = await _context.Tbpersonals
            .FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == userName ||
                p.CorreoCorporativo == userName);

        if (personal == null) return Forbid();

        var doc = await _context.TbExpedientes
            .FirstOrDefaultAsync(d => d.Id == id && d.CC == personal.CC && d.Visible);

        if (doc == null) return NotFound();

        var full = Path.Combine(_env.WebRootPath,
            doc.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(full)) return NotFound();

        return PhysicalFile(full, "application/pdf", doc.NombreArchivo + ".pdf",
            enableRangeProcessing: true);
    }
}
