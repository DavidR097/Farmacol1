using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador,TI")]
    public class TbinventariosController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly ExcelService _excel;

        public TbinventariosController(Farmacol1Context context,
            ExcelService excel)
        {
            _context = context;
            _excel = excel;
        }

        public async Task<IActionResult> Index(
            string? busqueda, string? ubicacion, string? dispositivo,
            string? marca, string? planta, string? serie, string? cedula)
        {
            var query = _context.Tbinventarios.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(i =>
                    (i.Ubicación != null && i.Ubicación.Contains(busqueda)) ||
                    (i.Dispositivo != null && i.Dispositivo.Contains(busqueda)) ||
                    (i.Marca != null && i.Marca.Contains(busqueda)) ||
                    (i.Planta != null && i.Planta.Contains(busqueda)) ||
                    (i.Serie != null && i.Serie.Contains(busqueda)) ||
                    (i.Modelo != null && i.Modelo.Contains(busqueda)) ||
                    (i.Observación != null && i.Observación.Contains(busqueda)));

            if (!string.IsNullOrEmpty(ubicacion))
                query = query.Where(i => i.Ubicación != null &&
                                         i.Ubicación.Contains(ubicacion));
            if (!string.IsNullOrEmpty(dispositivo))
                query = query.Where(i => i.Dispositivo != null &&
                                         i.Dispositivo.Contains(dispositivo));
            if (!string.IsNullOrEmpty(marca))
                query = query.Where(i => i.Marca != null && i.Marca.Contains(marca));
            if (!string.IsNullOrEmpty(planta))
                query = query.Where(i => i.Planta != null && i.Planta.Contains(planta));
            if (!string.IsNullOrEmpty(serie))
                query = query.Where(i => i.Serie != null && i.Serie.Contains(serie));
            if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                query = query.Where(i => i.CC == cedNum);

            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.Ubicacion = ubicacion ?? "";
            ViewBag.Dispositivo = dispositivo ?? "";
            ViewBag.Marca = marca ?? "";
            ViewBag.Planta = planta ?? "";
            ViewBag.Serie = serie ?? "";
            ViewBag.Cedula = cedula ?? "";

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Tbinventarios.FirstOrDefaultAsync(m => m.IdEquipo == id);
            if (item == null) return NotFound();
            return View(item);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IdEquipo,Ubicación,Ubicación2,Dispositivo,Modelo,Serie,Imei,Marca,Observación,Planta,CC")]
            Tbinventario tbinventario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tbinventario);

                if (tbinventario.CC != null && tbinventario.CC != 0)
                    await SincronizarResponsiva(tbinventario, null);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tbinventario);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Tbinventarios.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("IdEquipo,Ubicación,Ubicación2,Dispositivo,Modelo,Serie,Imei,Marca,Observación,Planta,CC,Anexo")]
            Tbinventario tbinventario, IFormFile? archivoAnexo)
        {
            if (id != tbinventario.IdEquipo) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Manejar archivo anexo
                    if (archivoAnexo != null && archivoAnexo.Length > 0)
                    {
                        var ext = Path.GetExtension(archivoAnexo.FileName).ToLower();
                        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                        {
                            ModelState.AddModelError("", "Solo se permiten .jpg o .png");
                            return View(tbinventario);
                        }
                        var carpeta = Path.Combine(
                            Directory.GetCurrentDirectory(), "wwwroot", "anexos");
                        Directory.CreateDirectory(carpeta);
                        var nombreArchivo = $"equipo_{tbinventario.IdEquipo}_{DateTime.Now.Ticks}{ext}";
                        using var stream = new FileStream(
                            Path.Combine(carpeta, nombreArchivo), FileMode.Create);
                        await archivoAnexo.CopyToAsync(stream);
                        tbinventario.Anexo = $"/anexos/{nombreArchivo}";
                    }
                    else
                    {
                        var anterior = await _context.Tbinventarios
                            .AsNoTracking()
                            .FirstOrDefaultAsync(i => i.IdEquipo == id);
                        tbinventario.Anexo = anterior?.Anexo;
                    }

                    // Sincronizar responsiva
                    var previo = await _context.Tbinventarios
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.IdEquipo == id);
                    await SincronizarResponsiva(tbinventario, previo?.CC);

                    _context.Update(tbinventario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbinventarioExists(tbinventario.IdEquipo)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tbinventario);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.Tbinventarios.FirstOrDefaultAsync(m => m.IdEquipo == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Tbinventarios.FindAsync(id);
            if (item != null) _context.Tbinventarios.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── IMPORTAR EXCEL ────────────────────────────────────────────────
        public IActionResult ImportarExcel() => View();

        [HttpPost]
        public async Task<IActionResult> ImportarExcel(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.Error = "Selecciona un archivo Excel válido.";
                return View();
            }

            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream);

            ExcelPackage.License.SetNonCommercialPersonal("Farmacol");
            using var package = new ExcelPackage(stream);
            var hoja = package.Workbook.Worksheets[0];
            int totalFilas = hoja.Dimension.Rows;

            for (int i = 2; i <= totalFilas; i++)
            {
                if (hoja.Cells[i, 2].Value == null) continue;

                int? cedParsed = null;
                if (hoja.Cells[i, 11].Value != null &&
                    int.TryParse(hoja.Cells[i, 11].Value.ToString(), out int cedTemp))
                    cedParsed = cedTemp;

                var registro = new Tbinventario
                {
                    Ubicación = hoja.Cells[i, 2].Value?.ToString() ?? "",
                    Ubicación2 = hoja.Cells[i, 3].Value?.ToString(),
                    Dispositivo = hoja.Cells[i, 4].Value?.ToString() ?? "",
                    Modelo = hoja.Cells[i, 5].Value?.ToString(),
                    Serie = hoja.Cells[i, 6].Value?.ToString(),
                    Imei = hoja.Cells[i, 7].Value?.ToString(),
                    Marca = hoja.Cells[i, 8].Value?.ToString(),
                    Observación = hoja.Cells[i, 9].Value?.ToString(),
                    Planta = hoja.Cells[i, 10].Value?.ToString(),
                    CC = cedParsed
                };

                _context.Tbinventarios.Add(registro);

                if (cedParsed != null && cedParsed != 0)
                    await SincronizarResponsiva(registro, null);
            }

            await _context.SaveChangesAsync();
            ViewBag.Exito = "✅ Importación completada correctamente.";
            return View();
        }

        // ── HELPERS ───────────────────────────────────────────────────────
        private async Task SincronizarResponsiva(Tbinventario inv, int? cedulaAnterior)
        {
            // Si cambió de cédula, limpiar la anterior
            if (cedulaAnterior != null && cedulaAnterior != inv.CC)
            {
                var old = await _context.Tbresponsivas
                    .FirstOrDefaultAsync(r => r.CC == cedulaAnterior);
                if (old != null) _context.Tbresponsivas.Remove(old);
            }

            if (inv.CC == null || inv.CC == 0) return;

            var responsiva = await _context.Tbresponsivas
                .FirstOrDefaultAsync(r => r.CC == inv.CC);

            if (responsiva == null)
            {
                _context.Tbresponsivas.Add(new Tbresponsiva
                {
                    CC = inv.CC.Value,
                    Equipo = inv.Dispositivo,
                    Marca = inv.Marca,
                    Serie = inv.Serie,
                    Observación = inv.Observación,
                    Estado = "Activo"
                });
            }
            else
            {
                responsiva.Equipo = inv.Dispositivo;
                responsiva.Marca = inv.Marca;
                responsiva.Serie = inv.Serie;
                responsiva.Observación = inv.Observación;
                responsiva.Estado = "Activo";
                _context.Tbresponsivas.Update(responsiva);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(
        string? busqueda, string? dispositivo, string? marca,
        string? serie, string? cedula, bool todo = false)
        {
            IQueryable<Tbinventario> query = _context.Tbinventarios;
            if (!todo)
            {
                if (!string.IsNullOrEmpty(busqueda))
                    query = query.Where(e =>
                        (e.Dispositivo != null && e.Dispositivo.Contains(busqueda)) ||
                        (e.Marca != null && e.Marca.Contains(busqueda)) ||
                        (e.Serie != null && e.Serie.Contains(busqueda)));
                if (!string.IsNullOrEmpty(dispositivo))
                    query = query.Where(e => e.Dispositivo != null && e.Dispositivo.Contains(dispositivo));
                if (!string.IsNullOrEmpty(marca))
                    query = query.Where(e => e.Marca != null && e.Marca.Contains(marca));
                if (!string.IsNullOrEmpty(serie))
                    query = query.Where(e => e.Serie != null && e.Serie.Contains(serie));
                if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                    query = query.Where(e => e.CC == cedNum);
            }
            var datos = await query.OrderBy(e => e.Dispositivo).ToListAsync();
            var bytes = _excel.ExportarInventario(datos);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"InventarioTI_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        private bool TbinventarioExists(int id) =>
            _context.Tbinventarios.Any(e => e.IdEquipo == id);
    }
}