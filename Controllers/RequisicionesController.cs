using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize(Roles = "Administrador,RRHH,Gerente")]
public class RequisicionesController : Controller
{
    private readonly Farmacol1Context _context;

    public RequisicionesController(Farmacol1Context context)
    {
        _context = context;
    }

    // LISTADO
    public async Task<IActionResult> Index()
    {
        var requisiciones = await _context.TbRequisiciones
            .OrderByDescending(r => r.FechaSolicitud)
            .ToListAsync();

        return View(requisiciones);
    }

    // CREAR GET
    public IActionResult Create()
    {
        return View();
    }

    // CREAR POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TbRequisiciones tbRequisicione)
    {
        if (ModelState.IsValid)
        {
            tbRequisicione.FechaCreacion = DateTime.Now;
            tbRequisicione.CreadoPor = User.Identity?.Name ?? "Sistema";

            _context.Add(tbRequisicione);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "✅ Requisición creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        return View(tbRequisicione);
    }

    // DETALLE
    public async Task<IActionResult> Details(int id)
    {
        var requisicion = await _context.TbRequisiciones.FindAsync(id);
        if (requisicion == null) return NotFound();

        return View(requisicion);
    }

    // EDITAR GET
    public async Task<IActionResult> Edit(int id)
    {
        var requisicion = await _context.TbRequisiciones.FindAsync(id);
        if (requisicion == null) return NotFound();

        return View(requisicion);
    }

    // EDITAR POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TbRequisiciones tbRequisicione)
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