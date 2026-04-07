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

        public async Task<IActionResult> Index(string mode = "equipo", string busqueda = null, string cedula = null, string equipo = null, string marca = null, string serie = null)
        {
            // mode: "equipo" = vista por equipo (por defecto), "cc" = vista agrupada por CC
            if (string.Equals(mode, "cc", StringComparison.OrdinalIgnoreCase))
            {
                // Agrupar por CC desde Tbinventarios
                var groups = await (from t in _context.Tbinventarios
                                    where t.CC != null
                                    group t by t.CC into g
                                    join p in _context.Tbpersonals on g.Key equals p.CC into pg
                                    from p in pg.DefaultIfEmpty()
                                    select new Farmacol.Models.TbresponsivaGroup
                                    {
                                        CC = g.Key!.Value,
                                        Nombre = p != null ? p.NombreColaborador : null,
                                        Count = g.Count(),
                                        PrimerEquipo = g.OrderBy(ti => ti.IdEquipo).Select(ti => ti.Dispositivo).FirstOrDefault(),
                                        PrimeraMarca = g.OrderBy(ti => ti.IdEquipo).Select(ti => ti.Marca).FirstOrDefault()
                                    }).ToListAsync();

                var vm = new Farmacol.Models.TbresponsivaIndexViewModel
                {
                    Mode = "cc",
                    Grupos = groups
                };
                return View(vm);
            }

            // Vista por equipo (default)
            var query = _context.Tbinventarios.AsQueryable().Where(t => t.CC != null);
            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(i =>
                    (i.Dispositivo != null && i.Dispositivo.Contains(busqueda)) ||
                    (i.Marca != null && i.Marca.Contains(busqueda)) ||
                    (i.Serie != null && i.Serie.Contains(busqueda)) ||
                    (i.Observación != null && i.Observación.Contains(busqueda))
                );

            if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                query = query.Where(i => i.CC == cedNum);
            if (!string.IsNullOrEmpty(equipo))
                query = query.Where(i => i.Dispositivo != null && i.Dispositivo.Contains(equipo));
            if (!string.IsNullOrEmpty(marca))
                query = query.Where(i => i.Marca != null && i.Marca.Contains(marca));
            if (!string.IsNullOrEmpty(serie))
                query = query.Where(i => i.Serie != null && i.Serie.Contains(serie));

            var vmEquipo = new Farmacol.Models.TbresponsivaIndexViewModel
            {
                Mode = "equipo",
                Equipos = await query.ToListAsync()
            };

            return View(vmEquipo);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var tbresponsiva = await _context.Tbresponsivas.FirstOrDefaultAsync(m => m.CC == id);
            if (tbresponsiva == null)
            {
                // Si no existe una entrada en Tbresponsivas para este CC, crear una vista mínima con equipos
                var equiposSolo = await _context.Tbinventarios.Where(t => t.CC == id).ToListAsync();
                if (!equiposSolo.Any()) return NotFound();

                var vmEmpty = new Farmacol.Models.TbresponsivaDetailsViewModel
                {
                    Responsiva = new Tbresponsiva { CC = id ?? 0, Equipo = equiposSolo.FirstOrDefault()?.Dispositivo, Marca = equiposSolo.FirstOrDefault()?.Marca, Serie = equiposSolo.FirstOrDefault()?.Serie, Observación = "(Generada automáticamente)" },
                    Equipos = equiposSolo
                };
                return View(vmEmpty);
            }

            // Obtener todos los equipos asignados a este CC desde Tbinventario
            var equipos = await _context.Tbinventarios.Where(t => t.CC == id).ToListAsync();

            var vm = new Farmacol.Models.TbresponsivaDetailsViewModel
            {
                Responsiva = tbresponsiva,
                Equipos = equipos
            };

            return View(vm);
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