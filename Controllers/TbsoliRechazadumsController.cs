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
    [Authorize(Roles = "Administrador,RRHH")]
    public class TbsoliRechazadumsController : Controller
    {
        private readonly Farmacol1Context _context;

        public TbsoliRechazadumsController(Farmacol1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string busqueda, string cedula, string nombre, string tipoSolicitud)
        {
            var query = _context.TbsoliRechazada.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(r =>
                    (r.Nombre != null && r.Nombre.Contains(busqueda)) ||
                    (r.TipoSolicitud != null && r.TipoSolicitud.Contains(busqueda)) ||
                    (r.Motivo != null && r.Motivo.Contains(busqueda))
                );

            if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                query = query.Where(r => r.CC == cedNum);
            if (!string.IsNullOrEmpty(nombre))
                query = query.Where(r => r.Nombre != null && r.Nombre.Contains(nombre));
            if (!string.IsNullOrEmpty(tipoSolicitud))
                query = query.Where(r => r.TipoSolicitud != null && r.TipoSolicitud.Contains(tipoSolicitud));

            ViewBag.Busqueda = busqueda;
            ViewBag.Cedula = cedula;
            ViewBag.Nombre = nombre;
            ViewBag.TipoSolicitud = tipoSolicitud;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var r = await _context.TbsoliRechazada.FirstOrDefaultAsync(m => m.IdSolicitud == id);
            if (r == null) return NotFound();
            return View(r);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdSolicitud,Nombre,CC,TipoSolicitud,Motivo,Observaciones,Anexos,FechaSolicitud")] TbsoliRechazadum tbsoliRechazadum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tbsoliRechazadum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tbsoliRechazadum);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var r = await _context.TbsoliRechazada.FindAsync(id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdSolicitud,Nombre,CC,TipoSolicitud,Motivo,Observaciones,Anexos,FechaSolicitud")] TbsoliRechazadum tbsoliRechazadum)
        {
            if (id != tbsoliRechazadum.IdSolicitud) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbsoliRechazadum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbsoliRechazadumExists(tbsoliRechazadum.IdSolicitud)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tbsoliRechazadum);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var r = await _context.TbsoliRechazada.FirstOrDefaultAsync(m => m.IdSolicitud == id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var r = await _context.TbsoliRechazada.FindAsync(id);
            if (r != null) _context.TbsoliRechazada.Remove(r);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TbsoliRechazadumExists(int id) =>
            _context.TbsoliRechazada.Any(e => e.IdSolicitud == id);
    }
}