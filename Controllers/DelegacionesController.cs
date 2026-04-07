using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH")]
public class DelegacionesController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly DelegacionService _delegacion;
    private readonly AuditService _audit;
    private readonly ExcelService _excel;

    public DelegacionesController(Farmacol1Context context,
        DelegacionService delegacion, 
        AuditService audit,
        ExcelService excel)
    {
        _context = context;
        _delegacion = delegacion;
        _audit = audit;
        _excel = excel;
    }

    public async Task<IActionResult> Index()
    {
        var delegaciones = await _context.TbDelegaciones
            .Where(d => d.Activa)
            .OrderByDescending(d => d.FechaCreacion)
            .ToListAsync();

        return View(delegaciones);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        ViewBag.Personal = await _context.Tbpersonals
            .Where(p => p.Cargo != null &&
                (p.Cargo.StartsWith("Jefe") || p.Cargo.StartsWith("Gerente")))
            .OrderBy(p => p.NombreColaborador).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(int cc, string motivo,
        DateOnly fechaInicio, DateOnly fechaFin)
    {
        var personal = await _context.Tbpersonals.FindAsync(cc);
        if (personal == null) { TempData["Error"] = "No se encontró el empleado."; return RedirectToAction(nameof(Crear)); }
        if (fechaFin < fechaInicio) { TempData["Error"] = "Fecha fin no puede ser anterior."; return RedirectToAction(nameof(Crear)); }

        await _delegacion.CrearDelegacion(personal.CC, personal.NombreColaborador ?? "",
            personal.Cargo ?? "", personal.Area ?? "", motivo, fechaInicio, fechaFin,
            User.Identity?.Name ?? "");

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_INHABILITACION, AuditService.ACC_CREAR,
            $"{personal.NombreColaborador} inhabilitado del {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}",
            cc.ToString());
        }
        catch { }

        TempData["Exito"] = $"✅ {personal.NombreColaborador} inhabilitado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var d = await _context.TbDelegaciones.FindAsync(id);
        if (d == null || !d.Activa) return NotFound();
        return View(d);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, string motivo,
        DateOnly fechaInicio, DateOnly fechaFin)
    {
        var d = await _context.TbDelegaciones.FindAsync(id);
        if (d == null || !d.Activa) return NotFound();
        if (fechaFin < fechaInicio) { TempData["Error"] = "Fecha fin no puede ser anterior."; return RedirectToAction(nameof(Editar), new { id }); }
        d.Motivo = motivo; d.FechaInicio = fechaInicio; d.FechaFin = fechaFin;
        await _context.SaveChangesAsync();
        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_INHABILITACION, AuditService.ACC_EDITAR,
            $"Inhabilitación #{id} editada", id.ToString());
        }
        catch { }
        TempData["Exito"] = "✅ Inhabilitación actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        await _delegacion.CancelarDelegacion(id);
        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_INHABILITACION, AuditService.ACC_CANCELAR,
            $"Inhabilitación #{id} cancelada por {User.Identity?.Name}", id.ToString());
        }
        catch { }
        TempData["Exito"] = "✅ Inhabilitación cancelada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(bool todo = false)
    {
        var datos = await _context.TbDelegaciones
            .OrderByDescending(d => d.FechaCreacion).ToListAsync();
        var bytes = _excel.ExportarInhabilitaciones(datos);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Inhabilitaciones_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}
