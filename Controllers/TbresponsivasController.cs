using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador,TI")]
    public class TbresponsivasController : Controller
    {
        private readonly Farmacol1Context _context;

        public TbresponsivasController(Farmacol1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string busqueda, string cedula, string equipo, string marca, string serie, string estado)
        {
            var query = _context.Tbresponsivas.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(r =>
                    (r.Equipo != null && r.Equipo.Contains(busqueda)) ||
                    (r.Marca != null && r.Marca.Contains(busqueda)) ||
                    (r.Serie != null && r.Serie.Contains(busqueda)) ||
                    (r.Estado != null && r.Estado.Contains(busqueda))
                );

            if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                query = query.Where(r => r.CC == cedNum);
            if (!string.IsNullOrEmpty(equipo))
                query = query.Where(r => r.Equipo != null && r.Equipo.Contains(equipo));
            if (!string.IsNullOrEmpty(marca))
                query = query.Where(r => r.Marca != null && r.Marca.Contains(marca));
            if (!string.IsNullOrEmpty(serie))
                query = query.Where(r => r.Serie != null && r.Serie.Contains(serie));
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado != null && r.Estado.Contains(estado));

            ViewBag.Busqueda = busqueda;
            ViewBag.Cedula = cedula;
            ViewBag.Equipo = equipo;
            ViewBag.Marca = marca;
            ViewBag.Serie = serie;
            ViewBag.Estado = estado;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var tbresponsiva = await _context.Tbresponsivas.FirstOrDefaultAsync(m => m.CC == id);
            if (tbresponsiva == null) return NotFound();
            return View(tbresponsiva);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CC,Equipo,Marca,Serie,Observación,Estado")] Tbresponsiva tbresponsiva)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tbresponsiva);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tbresponsiva);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tbresponsiva = await _context.Tbresponsivas.FindAsync(id);
            if (tbresponsiva == null) return NotFound();
            return View(tbresponsiva);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CC,Equipo,Marca,Serie,Observación,Estado")] Tbresponsiva tbresponsiva)
        {
            if (id != tbresponsiva.CC) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbresponsiva);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbresponsivaExists(tbresponsiva.CC)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tbresponsiva);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var tbresponsiva = await _context.Tbresponsivas.FirstOrDefaultAsync(m => m.CC == id);
            if (tbresponsiva == null) return NotFound();
            return View(tbresponsiva);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tbresponsiva = await _context.Tbresponsivas.FindAsync(id);
            if (tbresponsiva != null) _context.Tbresponsivas.Remove(tbresponsiva);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TbresponsivaExists(int id) =>
            _context.Tbresponsivas.Any(e => e.CC == id);
    }
}