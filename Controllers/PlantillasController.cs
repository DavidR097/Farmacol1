using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador")]
public class PlantillasController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly IWebHostEnvironment _env;
    private readonly AuditService _audit;

    private static readonly string[] TiposRRHH = {
        "Constancia laboral",
        "Paz y salvo",
        "Carta de retiro",
        "Certificado de ingresos",
        "Vacaciones",          // ← nuevo
        "Permisos",            // ← nuevo
        "Vacaciones/Permisos"  // ← genérica (aplica para ambos)
    };
    private static readonly string[] TiposTI = {
        "Responsiva TI", "Entrega de equipo", "Devolución de equipo"
    };

    public PlantillasController(Farmacol1Context context,
        IWebHostEnvironment env, AuditService audit)
    {
        _context = context; _env = env; _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var lista = await _context.TbPlantillas
            .OrderByDescending(p => p.FechaSubida).ToListAsync();
        return View(lista);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        ViewBag.TiposRRHH = TiposRRHH;
        ViewBag.TiposTI = TiposTI;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string nombre, string tipoDocumento,
        string modulo, IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        { TempData["Error"] = "Selecciona un archivo .docx."; return RedirectToAction(nameof(Crear)); }
        if (Path.GetExtension(archivo.FileName).ToLower() != ".docx")
        { TempData["Error"] = "Solo archivos Word (.docx)."; return RedirectToAction(nameof(Crear)); }

        var carpeta = Path.Combine(_env.WebRootPath, "plantillas");
        Directory.CreateDirectory(carpeta);
        var nombreArchivo = $"{DateTime.Now.Ticks}_{Path.GetFileName(archivo.FileName)}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        using var stream = new FileStream(rutaFisica, FileMode.Create);
        await archivo.CopyToAsync(stream);

        var anteriores = await _context.TbPlantillas
            .Where(p => p.TipoDocumento == tipoDocumento && p.Modulo == modulo && p.Activa)
            .ToListAsync();
        anteriores.ForEach(p => p.Activa = false);

        _context.TbPlantillas.Add(new TbPlantilla
        {
            Nombre = nombre,
            TipoDocumento = tipoDocumento,
            Modulo = modulo,
            RutaArchivo = $"/plantillas/{nombreArchivo}",
            FechaSubida = DateTime.Now,
            SubidaPor = User.Identity?.Name ?? "",
            Activa = true
        });
        await _context.SaveChangesAsync();

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_PLANTILLAS, AuditService.ACC_SUBIR,
            $"Plantilla '{nombre}' subida para {tipoDocumento} ({modulo})");
        }
        catch { }

        TempData["Exito"] = $"✅ Plantilla '{nombre}' subida.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var p = await _context.TbPlantillas.FindAsync(id);
        if (p == null) return NotFound();
        var fullPath = Path.Combine(_env.WebRootPath,
            p.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        _context.TbPlantillas.Remove(p);
        await _context.SaveChangesAsync();

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_PLANTILLAS, AuditService.ACC_ELIMINAR,
            $"Plantilla '{p.Nombre}' eliminada", id.ToString());
        }
        catch { }

        TempData["Exito"] = "Plantilla eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
