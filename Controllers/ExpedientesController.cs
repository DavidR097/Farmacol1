using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH,Gerente,Jefe,Coordinador,Asistente")]
public class ExpedientesController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] TiposPermitidos =
    {
        "Contrato", "Hoja de vida", "Responsiva TI", "Certificado",
        "Paz y salvo", "Carnet", "Otros"
    };

    public ExpedientesController(Farmacol1Context context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // ── INDEX ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? busqueda, string? area)
    {
        var query = _context.Tbpersonals.AsQueryable();

        if (!string.IsNullOrEmpty(busqueda))
            query = query.Where(p =>
                (p.NombreColaborador != null && p.NombreColaborador.Contains(busqueda)) ||
                (p.Cargo != null && p.Cargo.Contains(busqueda)));

        if (!string.IsNullOrEmpty(area))
            query = query.Where(p => p.Area != null && p.Area == area);

        var personal = await query
            .OrderBy(p => p.NombreColaborador)
            .Select(p => new { p.CC, p.NombreColaborador, p.Cargo, p.Area })
            .ToListAsync();

        var ccs = personal.Select(p => p.CC).ToList();

        // Reemplaza el bloque docsQ por esto:
        var todosLosDocsConteo = await _context.TbExpedientes
            .Where(d => ccs.Contains(d.CC))
            .Select(d => d.CC)
            .ToListAsync();

        var conteo = todosLosDocsConteo
            .GroupBy(cc => cc)
            .ToDictionary(g => g.Key, g => g.Count());

        // Traer últimos 3 docs por empleado para mostrar en acordeón
        var todosLosDocs = await _context.TbExpedientes
            .Where(d => ccs.Contains(d.CC))
            .OrderByDescending(d => d.FechaSubida)
            .ToListAsync();

        var docsRecientes = todosLosDocs
            .GroupBy(d => d.CC)
            .ToDictionary(g => g.Key, g => g.Take(3).ToList());

        ViewBag.Busqueda = busqueda ?? "";
        ViewBag.Area = area ?? "";
        ViewBag.Areas = await _context.Tbpersonals
            .Where(p => p.Area != null)
            .Select(p => p.Area!).Distinct().OrderBy(a => a).ToListAsync();
        ViewBag.Conteo = conteo;
        ViewBag.Personal = personal;
        ViewBag.DocsRecientes = docsRecientes;

        return View();
    }

    // ── EXPEDIENTE: carpeta completa del empleado ─────────────────────────
    public async Task<IActionResult> Expediente(int cc)
    {
        var personal = await _context.Tbpersonals.FindAsync(cc);
        if (personal == null) return NotFound();

        var docs = await _context.TbExpedientes
            .Where(d => d.CC == cc)
            .OrderByDescending(d => d.FechaSubida)
            .ToListAsync();

        ViewBag.Personal = personal;
        ViewBag.Tipos = TiposPermitidos;
        return View(docs);
    }

    // ── SUBIR: un archivo para un empleado ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subir(int cc, IFormFile archivo,
        string? tipoDocumento, string? nombrePersonalizado)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo PDF.";
            return RedirectToAction(nameof(Index));
        }
        if (Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
        {
            TempData["Error"] = "Solo se permiten archivos PDF.";
            return RedirectToAction(nameof(Index));
        }

        var ruta = await GuardarArchivo(archivo, cc);
        _context.TbExpedientes.Add(new TbExpediente
        {
            CC = cc,
            NombreArchivo = string.IsNullOrWhiteSpace(nombrePersonalizado)
                                ? Path.GetFileNameWithoutExtension(archivo.FileName)
                                : nombrePersonalizado,
            TipoDocumento = tipoDocumento,
            RutaArchivo = ruta,
            FechaSubida = DateTime.Now,
            SubidoPor = User.Identity?.Name ?? ""
        });
        await _context.SaveChangesAsync();
        TempData["Exito"] = "✅ Documento subido correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ── SUBIR MASIVO ──────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirMasivo(string ccs, IFormFile archivo,
        string? tipoDocumento, string? nombrePersonalizado)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo PDF.";
            return RedirectToAction(nameof(Index));
        }
        if (Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
        {
            TempData["Error"] = "Solo se permiten archivos PDF.";
            return RedirectToAction(nameof(Index));
        }

        var listaCCs = ccs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => int.TryParse(s.Trim(), out int n) ? (int?)n : null)
                          .Where(n => n.HasValue).Select(n => n!.Value).ToList();

        if (!listaCCs.Any())
        {
            TempData["Error"] = "No se seleccionaron empleados.";
            return RedirectToAction(nameof(Index));
        }

        // Leer el archivo una sola vez en memoria
        byte[] contenido;
        using (var ms = new MemoryStream())
        {
            await archivo.CopyToAsync(ms);
            contenido = ms.ToArray();
        }

        int subidos = 0;
        foreach (var cc in listaCCs)
        {
            var ruta = await GuardarBytes(contenido, archivo.FileName, cc);
            _context.TbExpedientes.Add(new TbExpediente
            {
                CC = cc,
                NombreArchivo = string.IsNullOrWhiteSpace(nombrePersonalizado)
                                    ? Path.GetFileNameWithoutExtension(archivo.FileName)
                                    : nombrePersonalizado,
                TipoDocumento = tipoDocumento,
                RutaArchivo = ruta,
                FechaSubida = DateTime.Now,
                SubidoPor = User.Identity?.Name ?? ""
            });
            subidos++;
        }

        await _context.SaveChangesAsync();
        TempData["Exito"] = $"✅ Documento subido a {subidos} empleado(s).";
        return RedirectToAction(nameof(Index));
    }

    // ── VER PDF ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Ver(int id)
    {
        var doc = await _context.TbExpedientes.FindAsync(id);
        if (doc == null) return NotFound();

        var fullPath = Path.Combine(_env.WebRootPath,
            doc.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        return PhysicalFile(fullPath, "application/pdf");
    }

    // ── ELIMINAR ──────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, string? returnUrl)
    {
        var doc = await _context.TbExpedientes.FindAsync(id);
        if (doc == null) return NotFound();

        var cc = doc.CC;
        var fullPath = Path.Combine(_env.WebRootPath,
            doc.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.TbExpedientes.Remove(doc);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Documento eliminado.";

        if (returnUrl == "expediente")
            return RedirectToAction(nameof(Expediente), new { cc });
        return RedirectToAction(nameof(Index));
    }

    // ── GENERAR DOCUMENTO ─────────────────────────────────────────────────
    public async Task<IActionResult> GenerarDocumento(int cc, string plantilla = "responsiva")
    {
        var personal = await _context.Tbpersonals.FindAsync(cc);
        if (personal == null) return NotFound();

        ViewBag.Personal = personal;
        ViewBag.Plantilla = plantilla;
        ViewBag.Fecha = DateTime.Today.ToString("dd 'de' MMMM 'de' yyyy",
                                new System.Globalization.CultureInfo("es-CO"));
        return View();
    }

    // ── HELPERS ───────────────────────────────────────────────────────────
    private async Task<string> GuardarArchivo(IFormFile archivo, int cc)
    {
        var carpeta = Path.Combine(_env.WebRootPath, "expedientes", cc.ToString());
        Directory.CreateDirectory(carpeta);
        var nombre = $"{DateTime.Now.Ticks}_{Path.GetFileName(archivo.FileName)}";
        var rutaFisica = Path.Combine(carpeta, nombre);
        using var stream = new FileStream(rutaFisica, FileMode.Create);
        await archivo.CopyToAsync(stream);
        return $"/expedientes/{cc}/{nombre}";
    }

    private async Task<string> GuardarBytes(byte[] contenido, string fileName, int cc)
    {
        var carpeta = Path.Combine(_env.WebRootPath, "expedientes", cc.ToString());
        Directory.CreateDirectory(carpeta);
        var nombre = $"{DateTime.Now.Ticks}_{Path.GetFileName(fileName)}";
        var rutaFisica = Path.Combine(carpeta, nombre);
        await System.IO.File.WriteAllBytesAsync(rutaFisica, contenido);
        return $"/expedientes/{cc}/{nombre}";
    }
}
