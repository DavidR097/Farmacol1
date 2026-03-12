using Farmacol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador,RRHH,Gerente,Jefe")]
    public class TbpersonalsController : Controller
    {
        private readonly Farmacol1Context _context;

        public TbpersonalsController(Farmacol1Context context)
        {
            _context = context;
        }

        // ── INDEX ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(
            string? busqueda, string? cedula,
            string? nombre, string? cargo, string? ciudad)
        {
            var userArea = ViewBag.UserArea as string ?? "";
            bool esCapHumano = userArea == "Capital Humano";

            if (!User.IsInRole("Administrador") &&
                !User.IsInRole("RRHH") && !esCapHumano)
                return RedirectToAction("Index", "Home");

            var query = _context.Tbpersonals.AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(p =>
                    (p.NombreColaborador != null && p.NombreColaborador.Contains(busqueda)) ||
                    (p.Cargo != null && p.Cargo.Contains(busqueda)) ||
                    (p.CiudadTrabajo != null && p.CiudadTrabajo.Contains(busqueda)));

            if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                query = query.Where(p => p.CC == cedNum);
            if (!string.IsNullOrEmpty(nombre))
                query = query.Where(p => p.NombreColaborador != null &&
                                         p.NombreColaborador.Contains(nombre));
            if (!string.IsNullOrEmpty(cargo))
                query = query.Where(p => p.Cargo != null &&
                                         p.Cargo.Contains(cargo));
            if (!string.IsNullOrEmpty(ciudad))
                query = query.Where(p => p.CiudadTrabajo != null &&
                                         p.CiudadTrabajo.Contains(ciudad));

            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.Cedula = cedula ?? "";
            ViewBag.Nombre = nombre ?? "";
            ViewBag.Cargo = cargo ?? "";
            ViewBag.Ciudad = ciudad ?? "";

            return View(await query.ToListAsync());
        }

        // ── DETAILS ───────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(m => m.CC == id);
            if (p == null) return NotFound();
            return View(p);
        }

        // ── CREATE ────────────────────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CC,ExpedicionCiudad,CiudadTrabajo,NombreColaborador,Barrio,Rh,Cargo,Area," +
                  "ContactoEmergencia,Parentesco,TelefonoEmergencia,TipoContrato,Eps," +
                  "FondoPensiones,FondoCesantias,CajaCompensacion,Arl,TipoCuenta,NumeroCuenta," +
                  "Banco,TallaCamisa,Contacto,CorreoCorporativo,UsuarioCorporativo")]
            Tbpersonal tbpersonal)
        {
            ModelState.Remove("Area");
            ModelState.Remove("Nombre");
            ModelState.Remove("NúmeroCel");
            ModelState.Remove("CelularEmergencia");
            ModelState.Remove("CajaCompensación");
            ModelState.Remove("Tpcuenta");
            ModelState.Remove("NoCuenta");
            ModelState.Remove("ExpediciónCiudad");

            if (ModelState.IsValid)
            {
                _context.Add(tbpersonal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View(tbpersonal);
        }

        // ── EDIT ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var p = await _context.Tbpersonals.FindAsync(id);
            if (p == null) return NotFound();
            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("CC,ExpedicionCiudad,CiudadTrabajo,NombreColaborador,Barrio,Rh,Cargo,Area," +
                  "ContactoEmergencia,Parentesco,TelefonoEmergencia,TipoContrato,Eps," +
                  "FondoPensiones,FondoCesantias,CajaCompensacion,Arl,TipoCuenta,NumeroCuenta," +
                  "Banco,TallaCamisa,Contacto,CorreoCorporativo,UsuarioCorporativo")]
            Tbpersonal tbpersonal)
        {
            if (id != tbpersonal.CC) return NotFound();

            ModelState.Remove("Area");
            ModelState.Remove("Nombre");
            ModelState.Remove("NúmeroCel");
            ModelState.Remove("CelularEmergencia");
            ModelState.Remove("CajaCompensación");
            ModelState.Remove("Tpcuenta");
            ModelState.Remove("NoCuenta");
            ModelState.Remove("ExpediciónCiudad");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbpersonal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbpersonalExists(tbpersonal.CC)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View(tbpersonal);
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(m => m.CC == id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var p = await _context.Tbpersonals.FindAsync(id);
            if (p != null) _context.Tbpersonals.Remove(p);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── IMPORTAR EXCEL ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult ImportarExcel() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarExcel(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Selecciona un archivo Excel válido.";
                return View();
            }

            var ext = Path.GetExtension(archivo.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] = "Solo se permiten archivos Excel (.xlsx, .xls).";
                return View();
            }

            using var stream = archivo.OpenReadStream();
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var rows = ws.RowsUsed().Skip(1).ToList();

            int importados = 0, errores = 0;

            string Txt(ClosedXML.Excel.IXLCell c)
            {
                try { return c.GetString()?.Trim() ?? ""; }
                catch { try { return c.Value.ToString()?.Trim() ?? ""; } catch { return ""; } }
            }

            string? NullIfEmpty(string s) =>
                string.IsNullOrWhiteSpace(s) ? null : s;

            DateOnly? ParseFecha(string s) =>
                DateOnly.TryParse(s, out var d) ? d : null;

            decimal? ParseDecimal(string s) =>
                decimal.TryParse(
                    s.Replace("$", "").Replace(",", "").Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

            int? ParseInt(string s) =>
                int.TryParse(s.Trim(), out var v) ? v : null;

            foreach (var row in rows)
            {
                try
                {
                    var ccStr = Txt(row.Cell(1));
                    if (!int.TryParse(ccStr, out int cc)) { errores++; continue; }

                    var existente = await _context.Tbpersonals.FindAsync(cc);
                    var p = existente ?? new Tbpersonal { CC = cc };

                    p.ExpedicionCiudad = NullIfEmpty(Txt(row.Cell(2)));
                    p.CiudadTrabajo = NullIfEmpty(Txt(row.Cell(3)));
                    p.NombreColaborador = NullIfEmpty(Txt(row.Cell(4)));
                    p.Cargo = NullIfEmpty(Txt(row.Cell(5)));
                    p.CodCeco = NullIfEmpty(Txt(row.Cell(6)));
                    p.NombreCentroCostos = NullIfEmpty(Txt(row.Cell(7)));
                    p.Area = NullIfEmpty(Txt(row.Cell(8)));
                    p.Gerencia = NullIfEmpty(Txt(row.Cell(9)));
                    p.FechaIngreso = ParseFecha(Txt(row.Cell(10)));
                    p.VencimientoPeriodoPrueba = ParseFecha(Txt(row.Cell(11)));
                    p.AniosAntiguedad = ParseInt(Txt(row.Cell(12)));
                    p.MesesAntiguedad = ParseInt(Txt(row.Cell(13)));
                    p.SalarioEnero2020 = ParseDecimal(Txt(row.Cell(14)));
                    p.SalarioFeb2020 = ParseDecimal(Txt(row.Cell(15)));
                    p.SalarioFeb2021 = ParseDecimal(Txt(row.Cell(16)));
                    p.SalarioFeb2022 = ParseDecimal(Txt(row.Cell(17)));
                    p.SalarioFeb2023 = ParseDecimal(Txt(row.Cell(18)));
                    p.SalarioEneFeb2024 = ParseDecimal(Txt(row.Cell(19)));
                    p.SalarioMar2024 = ParseDecimal(Txt(row.Cell(20)));
                    p.SalarioFeb2025 = ParseDecimal(Txt(row.Cell(21)));
                    p.SalarioFeb2026 = ParseDecimal(Txt(row.Cell(22)));
                    p.FechaNacimiento = ParseFecha(Txt(row.Cell(23)));
                    p.Edad = ParseInt(Txt(row.Cell(24)));
                    p.MesesEdad = ParseInt(Txt(row.Cell(25)));
                    p.MesNacimiento = NullIfEmpty(Txt(row.Cell(26)));
                    p.Generacion = NullIfEmpty(Txt(row.Cell(27)));
                    p.Genero = NullIfEmpty(Txt(row.Cell(28)));
                    p.CiudadNacimiento = NullIfEmpty(Txt(row.Cell(29)));
                    p.EstadoCivil = NullIfEmpty(Txt(row.Cell(30)));
                    p.CorreoPersonal = NullIfEmpty(Txt(row.Cell(31)));
                    p.Contacto = NullIfEmpty(Txt(row.Cell(32)));
                    p.DireccionResidencia = NullIfEmpty(Txt(row.Cell(33)));
                    p.Barrio = NullIfEmpty(Txt(row.Cell(34)));
                    p.Rh = NullIfEmpty(Txt(row.Cell(35)));
                    p.ContactoEmergencia = NullIfEmpty(Txt(row.Cell(36)));
                    p.Parentesco = NullIfEmpty(Txt(row.Cell(37)));
                    p.TelefonoEmergencia = NullIfEmpty(Txt(row.Cell(38)));
                    p.TipoContrato = NullIfEmpty(Txt(row.Cell(39)));
                    p.Eps = NullIfEmpty(Txt(row.Cell(40)));
                    p.FondoPensiones = NullIfEmpty(Txt(row.Cell(41)));
                    p.FondoCesantias = NullIfEmpty(Txt(row.Cell(42)));
                    p.CajaCompensacion = NullIfEmpty(Txt(row.Cell(43)));
                    p.Arl = NullIfEmpty(Txt(row.Cell(44)));
                    p.TipoCuenta = NullIfEmpty(Txt(row.Cell(45)));
                    p.NumeroCuenta = NullIfEmpty(Txt(row.Cell(46)));
                    p.Banco = NullIfEmpty(Txt(row.Cell(47)));
                    p.TallaCamisa = NullIfEmpty(Txt(row.Cell(48)));
                    p.Grupo = NullIfEmpty(Txt(row.Cell(49)));
                    p.Concepto = NullIfEmpty(Txt(row.Cell(50)));
                    p.CorreoCorporativo = NullIfEmpty(Txt(row.Cell(63)));
                    p.UsuarioCorporativo = NullIfEmpty(Txt(row.Cell(64)));

                    if (p.Area == "Contraloría") p.Area = "Contraloria";

                    if (existente == null) _context.Add(p);
                    else _context.Update(p);

                    importados++;
                }
                catch { errores++; }
            }

            await _context.SaveChangesAsync();
            TempData["Exito"] = $"✅ Importados: {importados}. Errores: {errores}.";
            return RedirectToAction(nameof(Index));
        }

        private bool TbpersonalExists(int id) =>
            _context.Tbpersonals.Any(e => e.CC == id);
    }
}