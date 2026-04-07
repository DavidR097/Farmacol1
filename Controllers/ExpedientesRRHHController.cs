using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH,Gerente,Jefe,Coordinador,Asistente")]
public class ExpedientesRRHHController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly IWebHostEnvironment _env;
    private readonly DocumentoService _docSvc;
    private readonly AuditService _audit;
    private readonly ExcelService _excel;

    public ExpedientesRRHHController(Farmacol1Context context,
        IWebHostEnvironment env, 
        DocumentoService docSvc,
        AuditService audit,
        ExcelService excel)
    {
        _context = context; 
        _env = env; 
        _docSvc = docSvc; 
        _audit = audit;
        _excel = excel;
    }

    public async Task<IActionResult> Index(string? busqueda, string? area)
    {
        var query = _context.Tbpersonals.AsQueryable();
        if (!string.IsNullOrEmpty(busqueda))
            query = query.Where(p =>
                (p.NombreColaborador != null && p.NombreColaborador.Contains(busqueda)) ||
                (p.Cargo != null && p.Cargo.Contains(busqueda)));
        if (!string.IsNullOrEmpty(area))
            query = query.Where(p => p.Area != null && p.Area == area);

        var personal = await query.OrderBy(p => p.NombreColaborador)
            .Select(p => new { p.CC, p.NombreColaborador, p.Cargo, p.Area }).ToListAsync();
        var ccs = personal.Select(p => p.CC).ToList();

        var conteo = (await _context.TbExpedientes
            .Where(d => ccs.Contains(d.CC) && d.Modulo == "RRHH").Select(d => d.CC).ToListAsync())
            .GroupBy(cc => cc).ToDictionary(g => g.Key, g => g.Count());

        var docsRecientes = (await _context.TbExpedientes
            .Where(d => ccs.Contains(d.CC) && d.Modulo == "RRHH")
            .OrderByDescending(d => d.FechaSubida).ToListAsync())
            .GroupBy(d => d.CC).ToDictionary(g => g.Key, g => g.Take(3).ToList());

        ViewBag.Busqueda = busqueda ?? ""; ViewBag.Area = area ?? "";
        ViewBag.Areas = await _context.Tbpersonals.Where(p => p.Area != null)
                               .Select(p => p.Area!).Distinct().OrderBy(a => a).ToListAsync();
        ViewBag.Conteo = conteo; ViewBag.Personal = personal; ViewBag.DocsRecientes = docsRecientes;
        ViewBag.Plantillas = await _context.TbPlantillas
                               .Where(p => p.Modulo == "RRHH" && p.Activa).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(int cc, string tipoDocumento)
    {
        var personal = await _context.Tbpersonals.FindAsync(cc);
        if (personal == null) return NotFound();
        var plantilla = await _context.TbPlantillas
            .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.Modulo == "RRHH" && p.Activa);
        if (plantilla == null) { TempData["Error"] = $"No hay plantilla activa para '{tipoDocumento}'."; return RedirectToAction(nameof(Index)); }

        var rutaFisica = Path.Combine(_env.WebRootPath,
            plantilla.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var docxBytes = await _docSvc.GenerarDocxAsync(rutaFisica, personal);
        var nombreBase = $"{tipoDocumento.Replace(" ", "_")}_{personal.CC}_{DateTime.Now:yyyyMMdd_HHmm}";
        return File(docxBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"{nombreBase}.docx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subir(int cc, IFormFile archivo,
        string? tipoDocumento, string? nombrePersonalizado)
    {
        if (archivo == null || Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
        { TempData["Error"] = "Solo PDF."; return RedirectToAction(nameof(Index)); }

        var ruta = await GuardarPdf(archivo, cc);
        var nombreDoc = string.IsNullOrWhiteSpace(nombrePersonalizado)
            ? Path.GetFileNameWithoutExtension(archivo.FileName) : nombrePersonalizado;

        _context.TbExpedientes.Add(new TbExpediente
        {
            CC = cc,
            Modulo = "RRHH",
            NombreArchivo = nombreDoc,
            TipoDocumento = tipoDocumento,
            RutaArchivo = ruta,
            FechaSubida = DateTime.Now,
            SubidoPor = User.Identity?.Name ?? ""
        });
        await _context.SaveChangesAsync();

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_EXPEDIENTES, AuditService.ACC_SUBIR,
            $"Documento '{nombreDoc}' subido al expediente CC:{cc} (RRHH)",
            cc.ToString());
        }
        catch { }

        TempData["Exito"] = "✅ Documento subido.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirMasivo(string ccs, IFormFile archivo,
        string? tipoDocumento, string? nombrePersonalizado)
    {
        if (archivo == null || Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
        { TempData["Error"] = "Solo PDF."; return RedirectToAction(nameof(Index)); }
        var lista = ccs.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out int n) ? (int?)n : null)
            .Where(n => n.HasValue).Select(n => n!.Value).ToList();
        if (!lista.Any()) { TempData["Error"] = "Sin empleados."; return RedirectToAction(nameof(Index)); }
        byte[] contenido;
        using (var msB = new MemoryStream()) { await archivo.CopyToAsync(msB); contenido = msB.ToArray(); }
        var nombreDoc = string.IsNullOrWhiteSpace(nombrePersonalizado)
            ? Path.GetFileNameWithoutExtension(archivo.FileName) : nombrePersonalizado;
        foreach (var cc in lista)
        {
            var ruta = await GuardarBytes(contenido, archivo.FileName, cc);
            _context.TbExpedientes.Add(new TbExpediente
            {
                CC = cc,
                Modulo = "RRHH",
                NombreArchivo = nombreDoc,
                TipoDocumento = tipoDocumento,
                RutaArchivo = ruta,
                FechaSubida = DateTime.Now,
                SubidoPor = User.Identity?.Name ?? ""
            });
        }
        await _context.SaveChangesAsync();
        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_EXPEDIENTES, AuditService.ACC_SUBIR,
            $"Subida masiva '{nombreDoc}' a {lista.Count} empleados (RRHH)");
        }
        catch { }
        TempData["Exito"] = $"✅ Subido a {lista.Count} empleado(s).";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Ver(int id)
    {
        var doc = await _context.TbExpedientes.FindAsync(id);
        if (doc == null) return NotFound();
        var full = Path.Combine(_env.WebRootPath,
            doc.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(full)) return NotFound();
        return PhysicalFile(full, "application/pdf");
    }

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
        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_EXPEDIENTES, AuditService.ACC_ELIMINAR,
            $"Documento '{doc.NombreArchivo}' eliminado del expediente CC:{cc} (RRHH)",
            id.ToString());
        }
        catch { }
        TempData["Exito"] = "Documento eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GuardarPdf(IFormFile archivo, int cc)
    {
        var carpeta = Path.Combine(_env.WebRootPath, "expedientes", "rrhh", cc.ToString());
        Directory.CreateDirectory(carpeta);
        var nombre = $"{DateTime.Now.Ticks}_{Path.GetFileName(archivo.FileName)}";
        using var s = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create);
        await archivo.CopyToAsync(s);
        return $"/expedientes/rrhh/{cc}/{nombre}";
    }

    private async Task<string> GuardarBytes(byte[] contenido, string fileName, int cc)
    {
        var carpeta = Path.Combine(_env.WebRootPath, "expedientes", "rrhh", cc.ToString());
        Directory.CreateDirectory(carpeta);
        var nombre = $"{DateTime.Now.Ticks}_{Path.GetFileName(fileName)}";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(carpeta, nombre), contenido);
        return $"/expedientes/rrhh/{cc}/{nombre}";
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(string? busqueda, string? area, bool todo = false)
    {
        IQueryable<TbExpediente> query = _context.TbExpedientes.Where(d => d.Modulo == "RRHH");
        if (!todo)
        {
            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(d => d.NombreArchivo.Contains(busqueda) ||
                    (d.TipoDocumento != null && d.TipoDocumento.Contains(busqueda)));
        }
        var datos = await query.OrderByDescending(d => d.FechaSubida).ToListAsync();
        var bytes = _excel.ExportarExpedientes(datos);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ExpedientesRRHH_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}
