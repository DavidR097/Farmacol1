using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador")]
public class AuditTrailController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly ExcelService _excel;

    public AuditTrailController(Farmacol1Context context,
        ExcelService excel)
    {
        _context = context;
        _excel = excel;
    }

    public async Task<IActionResult> Index(
        string? usuario, string? modulo, string? accion,
        string? desde, string? hasta, int pagina = 1)
    {
        var query = _context.TbAuditTrails.AsQueryable();

        if (!string.IsNullOrEmpty(usuario))
            query = query.Where(a => a.Usuario.Contains(usuario));

        if (!string.IsNullOrEmpty(modulo))
            query = query.Where(a => a.Modulo == modulo);

        if (!string.IsNullOrEmpty(accion))
            query = query.Where(a => a.Accion == accion);

        if (DateTime.TryParse(desde, out var fechaDesde))
            query = query.Where(a => a.Fecha >= fechaDesde);

        if (DateTime.TryParse(hasta, out var fechaHasta))
            query = query.Where(a => a.Fecha <= fechaHasta.AddDays(1));

        const int porPagina = 50;
        var total = await query.CountAsync();
        var registros = await query
            .OrderByDescending(a => a.Fecha)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .ToListAsync();

        ViewBag.Usuario = usuario;
        ViewBag.Modulo = modulo;
        ViewBag.Accion = accion;
        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;
        ViewBag.Pagina = pagina;
        ViewBag.Total = total;
        ViewBag.PorPagina = porPagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / porPagina);

        // Listas para los filtros
        ViewBag.Modulos = await _context.TbAuditTrails
            .Select(a => a.Modulo).Distinct().OrderBy(m => m).ToListAsync();
        ViewBag.Acciones = await _context.TbAuditTrails
            .Select(a => a.Accion).Distinct().OrderBy(a => a).ToListAsync();

        return View(registros);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(
    string? usuario, string? modulo, string? accion,
    string? desde, string? hasta, bool todo = false)
    {
        IQueryable<TbAuditTrail> query = _context.TbAuditTrails;
        if (!todo)
        {
            if (!string.IsNullOrEmpty(usuario))
                query = query.Where(a => a.Usuario.Contains(usuario));
            if (!string.IsNullOrEmpty(modulo))
                query = query.Where(a => a.Modulo == modulo);
            if (!string.IsNullOrEmpty(accion))
                query = query.Where(a => a.Accion == accion);
            if (DateTime.TryParse(desde, out var fechaDesde))
                query = query.Where(a => a.Fecha >= fechaDesde);
            if (DateTime.TryParse(hasta, out var fechaHasta))
                query = query.Where(a => a.Fecha <= fechaHasta.AddDays(1));
        }
        var datos = await query.OrderByDescending(a => a.Fecha).ToListAsync();
        var bytes = _excel.ExportarAuditTrail(datos);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"AuditTrail_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}