using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH")]
public class PersonalRetiradoController : Controller
{
    private readonly Farmacol1Context _context;

    public PersonalRetiradoController(Farmacol1Context context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? busqueda)
    {
        var query = _context.TbpersonalRetirados.AsQueryable();

        if (!string.IsNullOrEmpty(busqueda))
            query = query.Where(r =>
                (r.NombreColaborador != null && r.NombreColaborador.Contains(busqueda)) ||
                (r.Cargo != null && r.Cargo.Contains(busqueda)) ||
                (r.Area != null && r.Area.Contains(busqueda)));

        ViewBag.Busqueda = busqueda;
        return View(await query.OrderByDescending(r => r.FechaRetiro).ToListAsync());
    }
}