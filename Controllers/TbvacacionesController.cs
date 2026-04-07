using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador,RRHH,Usuario")]
    public class TbvacacionesController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly IWebHostEnvironment _env;

        public TbvacacionesController(Farmacol1Context context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string busqueda, string cedula, string nombre, string cargo, string fechaInicio, string fechaFin)
        {
            if (User.IsInRole("Usuario"))
                return RedirectToAction(nameof(Create));

            var query = _context.Tbvacaciones.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(v =>
                    v.Nombre.Contains(busqueda) ||
                    v.Cargo.Contains(busqueda) ||
                    v.CC.Contains(busqueda) ||
                    (v.Observaciones != null && v.Observaciones.Contains(busqueda))
                );

            if (!string.IsNullOrEmpty(cedula))
                query = query.Where(v => v.CC.Contains(cedula));
            if (!string.IsNullOrEmpty(nombre))
                query = query.Where(v => v.Nombre.Contains(nombre));
            if (!string.IsNullOrEmpty(cargo))
                query = query.Where(v => v.Cargo.Contains(cargo));
            if (!string.IsNullOrEmpty(fechaInicio) && DateOnly.TryParse(fechaInicio, out DateOnly fInicio))
                query = query.Where(v => v.FechaInicio >= fInicio);
            if (!string.IsNullOrEmpty(fechaFin) && DateOnly.TryParse(fechaFin, out DateOnly fFin))
                query = query.Where(v => v.FechaFin <= fFin);

            ViewBag.Busqueda = busqueda;
            ViewBag.Cedula = cedula;
            ViewBag.Nombre = nombre;
            ViewBag.Cargo = cargo;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var tbvacacione = await _context.Tbvacaciones.FirstOrDefaultAsync(m => m.IdVacación == id);
            if (tbvacacione == null) return NotFound();
            return View(tbvacacione);
        }

        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Usuario"))
            {
                var email = User.Identity?.Name ?? "";
                var personal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.CorreoCorporativo == email); ;

                if (personal != null)
                {
                    ViewBag.CedulaActual = personal.CC.ToString();
                    ViewBag.NombreActual = personal.Nombre;
                    ViewBag.CargoActual = personal.Cargo;
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IdVacación,Nombre,CC,Cargo,FechaInicio,FechaFin,TotalDías,FechaSolicitud,Observaciones")]
            Tbvacacione tbvacacione, IFormFile? archivoAnexo)
        {
            if (ModelState.IsValid)
            {
                if (archivoAnexo != null && archivoAnexo.Length > 0)
                {
                    var extension = Path.GetExtension(archivoAnexo.FileName).ToLower();
                    if (extension != ".pdf")
                    {
                        ModelState.AddModelError("", "Solo se permiten archivos PDF.");
                        return View(tbvacacione);
                    }

                    var nombreArchivo = $"vacacion_{DateTime.Now.Ticks}.pdf";
                    var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "anexos");
                    Directory.CreateDirectory(carpeta);
                    var ruta = Path.Combine(carpeta, nombreArchivo);

                    using var stream = new FileStream(ruta, FileMode.Create);
                    await archivoAnexo.CopyToAsync(stream);
                    tbvacacione.Anexos = $"/anexos/{nombreArchivo}";
                }

                _context.Add(tbvacacione);
                await _context.SaveChangesAsync();

                if (User.IsInRole("Usuario"))
                {
                    TempData["Exito"] = "Solicitud de vacaciones enviada correctamente.";
                    return RedirectToAction(nameof(Create));
                }

                return RedirectToAction(nameof(Index));
            }
            return View(tbvacacione);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tbvacacione = await _context.Tbvacaciones.FindAsync(id);
            if (tbvacacione == null) return NotFound();
            return View(tbvacacione);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdVacación,Nombre,CC,Cargo,FechaInicio,FechaFin,TotalDías,FechaSolicitud,Observaciones,Anexos")] Tbvacacione tbvacacione)
        {
            if (id != tbvacacione.IdVacación) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbvacacione);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbvacacioneExists(tbvacacione.IdVacación)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tbvacacione);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var tbvacacione = await _context.Tbvacaciones.FirstOrDefaultAsync(m => m.IdVacación == id);
            if (tbvacacione == null) return NotFound();
            return View(tbvacacione);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tbvacacione = await _context.Tbvacaciones.FindAsync(id);
            if (tbvacacione != null) _context.Tbvacaciones.Remove(tbvacacione);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DescargarAnexo(int id)
        {
            var tb = await _context.Tbvacaciones.FindAsync(id);
            if (tb == null || string.IsNullOrEmpty(tb.Anexos)) return NotFound();

            Tbpersonal? personal = null;
            if (int.TryParse(tb.CC, out int cc))
                personal = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == cc);

            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            bool esSolicitante = personal != null && (User.Identity?.Name == personal.CorreoCorporativo || User.Identity?.Name == personal.UsuarioCorporativo);

            if (!esAdmin && !esRRHH && !esSolicitante) return Forbid();

            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", tb.Anexos.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            return PhysicalFile(fullPath, "application/pdf", Path.GetFileName(fullPath));
        }

        private bool TbvacacioneExists(int id) =>
            _context.Tbvacaciones.Any(e => e.IdVacación == id);
    }
}