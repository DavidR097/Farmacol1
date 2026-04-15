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

    // INDEX
    public async Task<IActionResult> Index()
    {
        var requisiciones = await _context.TbRequisiciones
            .OrderByDescending(r => r.FechaSolicitud ?? DateOnly.MinValue)
            .ToListAsync();

        return View(requisiciones);
    }

    // CREATE GET - Corregido para evitar NullReference
    public async Task<IActionResult> Create()
    {
        // Generar No. Requisición automático
        var ultimo = await _context.TbRequisiciones
            .OrderByDescending(r => r.NoRequisicion)
            .FirstOrDefaultAsync();

        string nuevoNoRequisicion = "RP-00001";

        if (ultimo != null && !string.IsNullOrEmpty(ultimo.NoRequisicion))
        {
            var numeroActual = int.TryParse(ultimo.NoRequisicion.Replace("RP-", ""), out int num) ? num : 0;
            nuevoNoRequisicion = $"RP-{(numeroActual + 1):D5}";
        }

        ViewBag.NoRequisicionSugerido = nuevoNoRequisicion;

        // Fecha Solicitud = Hoy
        var model = new TbRequisicione
        {
            FechaSolicitud = DateOnly.FromDateTime(DateTime.Today)
        };

        return View(model);
    }

    // CREATE POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TbRequisicione tbRequisicione)
    {
        if (ModelState.IsValid)
        {
            // Asegurar No. Requisición si no vino
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

            _context.Add(tbRequisicione);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "✅ Requisición creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Si hay error de validación, volver a generar el No. Requisición para que no se pierda
        if (string.IsNullOrEmpty(tbRequisicione.NoRequisicion))
        {
            var ultimo = await _context.TbRequisiciones
                .OrderByDescending(r => r.NoRequisicion)
                .FirstOrDefaultAsync();

            string nuevo = "RP-00001";
            if (ultimo != null && !string.IsNullOrEmpty(ultimo.NoRequisicion))
            {
                var num = int.TryParse(ultimo.NoRequisicion.Replace("RP-", ""), out int n) ? n : 0;
                nuevo = $"RP-{(num + 1):D5}";
            }
            ViewBag.NoRequisicionSugerido = nuevo;
        }

        return View(tbRequisicione);
    }

    // DETAILS
    public async Task<IActionResult> Details(int id)
    {
        var requisicion = await _context.TbRequisiciones.FindAsync(id);
        if (requisicion == null) return NotFound();
        return View(requisicion);
    }

    // EDIT GET
    public async Task<IActionResult> Edit(int id)
    {
        var requisicion = await _context.TbRequisiciones.FindAsync(id);
        if (requisicion == null) return NotFound();
        return View(requisicion);
    }

    // EDIT POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TbRequisicione tbRequisicione)
    {
        if (id != tbRequisicione.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tbRequisicione);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "✅ Requisición actualizada correctamente.";
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