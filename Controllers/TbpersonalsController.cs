using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador,RRHH,Gerente,Jefe,Coordinador,Asistente")]
    public class TbpersonalsController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly PersonalRetiroService _retiro;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _audit;
        private readonly ExcelService _excel;

        public TbpersonalsController(
            Farmacol1Context context,
            UserManager<IdentityUser> userManager,
            PersonalRetiroService retiro,
            AuditService audit,
            ExcelService excel)
        {
            _context = context;
            _userManager = userManager;
            _retiro = retiro;
            _audit = audit;
            _excel = excel;
        }

        private bool TieneAcceso()
        {
            var userArea = ViewBag.UserArea as string ?? "";
            bool esCapHumano = string.Equals(userArea, "Capital Humano", StringComparison.OrdinalIgnoreCase);
            return User.IsInRole("Administrador") ||
                   User.IsInRole("RRHH") ||
                   esCapHumano;
        }

        // ====================== INDEX ======================
        public async Task<IActionResult> Index(string? busqueda, string? cedula, string? nombre,
            string? cargo, string? ciudad)
        {
            if (!TieneAcceso())
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
                query = query.Where(p => p.NombreColaborador != null && p.NombreColaborador.Contains(nombre));

            if (!string.IsNullOrEmpty(cargo))
                query = query.Where(p => p.Cargo != null && p.Cargo.Contains(cargo));

            if (!string.IsNullOrEmpty(ciudad))
                query = query.Where(p => p.CiudadTrabajo != null && p.CiudadTrabajo.Contains(ciudad));

            ViewBag.Busqueda = busqueda ?? "";
            ViewBag.Cedula = cedula ?? "";
            ViewBag.Nombre = nombre ?? "";
            ViewBag.Cargo = cargo ?? "";
            ViewBag.Ciudad = ciudad ?? "";

            return View(await query.OrderBy(p => p.NombreColaborador).ToListAsync());
        }

        // ====================== DETAILS ======================
        public async Task<IActionResult> Details(int? id)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            var personal = await _context.Tbpersonals.FindAsync(id);
            if (personal == null) return NotFound();

            return View(personal);
        }

        // ====================== CREATE ======================
        public async Task<IActionResult> Create()
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");
            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tbpersonal tbpersonal)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                _context.Add(tbpersonal);
                await _context.SaveChangesAsync();

                await _audit.RegistrarAsync(AuditService.MOD_PERSONAL, AuditService.ACC_CREAR,
                    $"Personal creado: {tbpersonal.NombreColaborador} (CC: {tbpersonal.CC})",
                    tbpersonal.CC.ToString());

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View(tbpersonal);
        }

        // ====================== EDIT ======================
        public async Task<IActionResult> Edit(int? id)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            var personal = await _context.Tbpersonals.FindAsync(id);
            if (personal == null) return NotFound();

            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View(personal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tbpersonal tbpersonal)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");
            if (id != tbpersonal.CC) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbpersonal);
                    await _context.SaveChangesAsync();

                    await _audit.RegistrarAsync(AuditService.MOD_PERSONAL, AuditService.ACC_EDITAR,
                        $"Personal editado: {tbpersonal.NombreColaborador} (CC: {id})", id.ToString());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbpersonalExists(id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Areas = await _context.Tbareas.OrderBy(a => a.Nombre).ToListAsync();
            return View(tbpersonal);
        }

        // ====================== DELETE (Retiro) ======================
        public async Task<IActionResult> Delete(int? id)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            var personal = await _context.Tbpersonals.FindAsync(id);
            if (personal == null) return NotFound();

            return View(personal);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string? motivoRetiro)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");

            var personal = await _context.Tbpersonals.FindAsync(id);
            if (personal == null) return NotFound();

            // Eliminar usuario de Identity si existe
            if (!string.IsNullOrEmpty(personal.CorreoCorporativo))
            {
                var usuario = await _userManager.FindByEmailAsync(personal.CorreoCorporativo);
                if (usuario != null)
                    await _userManager.DeleteAsync(usuario);
            }

            await _retiro.RetirarPersonal(personal, motivoRetiro);

            await _audit.RegistrarAsync(AuditService.MOD_PERSONAL, AuditService.ACC_RETIRAR,
                $"{personal.NombreColaborador} retirado. Motivo: {motivoRetiro ?? "Sin especificar"}",
                id.ToString());

            TempData["Exito"] = $"✅ {personal.NombreColaborador} movido a Personal Retirado.";
            return RedirectToAction(nameof(Index));
        }

        // ====================== IMPORTAR EXCEL ======================
        [HttpGet]
        public IActionResult ImportarExcel()
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarExcel(IFormFile archivo)
        {
            if (!TieneAcceso())
                return RedirectToAction("Index", "Home");

            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Selecciona un archivo Excel válido.";
                return View();
            }

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] = "Solo se permiten archivos Excel (.xlsx o .xls).";
                return View();
            }

            int importados = 0;
            int actualizados = 0;
            int errores = 0;

            try
            {
                using var memoryStream = new MemoryStream();
                await archivo.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using var workbook = new XLWorkbook(memoryStream);
                var worksheet = workbook.Worksheet(1);

                // Leer encabezado y mapear índices por texto para evitar mezclas
                var header = worksheet.Row(1);
                var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 64;
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int c = 1; c <= lastCol; c++)
                {
                    var txt = (header.Cell(c).GetString() ?? "").Trim();
                    if (string.IsNullOrEmpty(txt)) continue;
                    var key = new string(txt.ToLowerInvariant().Where(ch => !char.IsWhiteSpace(ch) && !char.IsPunctuation(ch)).ToArray());
                    if (!headerMap.ContainsKey(key)) headerMap[key] = c;
                }

                int Col(string[] names, int fallback)
                {
                    foreach (var n in names)
                    {
                        var k = new string(n.ToLowerInvariant().Where(ch => !char.IsWhiteSpace(ch) && !char.IsPunctuation(ch)).ToArray());
                        var found = headerMap.FirstOrDefault(kv => kv.Key.Contains(k));
                        if (!string.IsNullOrEmpty(found.Key)) return found.Value;
                    }
                    return fallback;
                }

                // según tu encabezado: CC está en columna B (2)
                var colCC = Col(new[] { "cc", "c.c", "cedula", "cédula" }, 2);
                var colExped = Col(new[] { "expedicionciudad", "expedicion" }, 3);
                var colCiudadTrabajo = Col(new[] { "ciudaddetrabajo", "ciudadtrabajo" }, 4);
                var colNombre = Col(new[] { "nombrecolaborador", "nombre" }, 5);
                var colCargo = Col(new[] { "cargo" }, 6);
                var colCodCeco = Col(new[] { "codceco", "ceco" }, 7);
                var colCentroCostos = Col(new[] { "nombrecentrodecostos", "centrocostos" }, 8);
                var colArea = Col(new[] { "area" }, 9);
                var colGerencia = Col(new[] { "gerencia", "gerencia(reporte)" }, 10);
                var colFechaIngreso = Col(new[] { "fechaingreso" }, 11);
                var colVencPrueba = Col(new[] { "vencimientoperiododeprueba", "vencimientoperiodoprueba" }, 12);
                var colAnio = Col(new[] { "años" }, 13);
                var colMes = Col(new[] { "meses" }, 14);
                var colSalarioO = 15; // salario columns start 15..23
                var colFechaNac = Col(new[] { "fechanac", "fechanacimiento", "fecha nac" }, 35);
                var colEdad = Col(new[] { "años.1", "años1", "años_1" }, 36);
                var colMesNacimiento = Col(new[] { "mesdenacimiento", "mesdenac" }, 38);
                var colGeneracion = Col(new[] { "generacion" }, 39);
                var colGenero = Col(new[] { "genero" }, 40);
                var colCiudadNac = Col(new[] { "ciudaddenac", "ciudaddenacimiento", "ciudad nac" }, 41);
                var colEstadoCivil = Col(new[] { "estadocivil" }, 42);
                var colCorreoPersonal = Col(new[] { "correopersonal" }, 43);
                var colContacto = Col(new[] { "contacto" }, 44);
                var colDireccion = Col(new[] { "direccionresidencia" }, 45);
                var colBarrio = Col(new[] { "barrio" }, 46);
                var colRh = Col(new[] { "rh" }, 47);
                var colContactoEmerg = Col(new[] { "contactoencasodeemergencia", "contactoencasodeemergencia" }, 48);
                var colParentesco = Col(new[] { "parentesco" }, 49);
                var colTelefonoEmerg = Col(new[] { "telefono/celularencasodeemergencia", "telefonocelularencasodeemergencia" }, 50);
                var colTipoContrato = Col(new[] { "tipocontrato" }, 51);
                var colEps = Col(new[] { "eps" }, 52);
                var colFondoPensiones = Col(new[] { "fondopensiones", "fondopensiones" }, 53);
                var colFondoCesantias = Col(new[] { "fondocesantias" }, 54);
                var colCajaComp = Col(new[] { "cajadecompensacion", "cajadecompensaciòn" }, 55);
                var colArl = Col(new[] { "arl" }, 56);
                var colTipoCuenta = Col(new[] { "tipocta", "tipocta" }, 57);
                var colNoCuenta = Col(new[] { "cta.no", "cta.no" }, 58);
                var colBanco = Col(new[] { "banco" }, 59);
                var colTallaCamisa = Col(new[] { "tallacamisa" }, 60);
                var colGrupo = Col(new[] { "grupo" }, 61);
                var colConcepto = Col(new[] { "concepto" }, 62);
                var colCorreoCorp = Col(new[] { "correocorporativo", "correocorporativo" }, 63);
                var colUsuarioCorp = Col(new[] { "usuariocorporativo", "usuariocorporativo" }, 64);

                var rows = worksheet.RowsUsed().Skip(1).ToList(); // Saltar fila de encabezado
                var errorDetails = new List<string>();
                int rowIndex = 1;
                foreach (var row in rows)
                {
                    rowIndex++;
                    try
                    {
                        if (!row.CellsUsed().Any()) continue;
                        var ccRaw = GetString(row.Cell(colCC));
                        if (string.IsNullOrWhiteSpace(ccRaw) || !int.TryParse(ccRaw.Replace(".", "").Replace(",", ""), out int cc))
                        {
                            errores++; errorDetails.Add($"Fila {rowIndex}: C.C. inválida ('{ccRaw}')."); continue;
                        }

                        var personal = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == cc);
                        bool esNuevo = personal == null;
                        personal ??= new Tbpersonal { CC = cc };

                        personal.ExpedicionCiudad = GetString(row.Cell(colExped));
                        personal.CiudadTrabajo = GetString(row.Cell(colCiudadTrabajo));
                        personal.NombreColaborador = GetString(row.Cell(colNombre));
                        personal.Cargo = GetString(row.Cell(colCargo));
                        personal.CodCeco = GetString(row.Cell(colCodCeco));
                        personal.NombreCentroCostos = GetString(row.Cell(colCentroCostos));
                        personal.Area = GetString(row.Cell(colArea));
                        personal.Gerencia = GetString(row.Cell(colGerencia));
                        personal.FechaIngreso = GetDateOnly(row.Cell(colFechaIngreso));
                        personal.VencimientoPeriodoPrueba = GetDateOnly(row.Cell(colVencPrueba));

                        personal.SalarioEnero2020 = GetDecimal(row.Cell(colSalarioO));
                        personal.SalarioFeb2020 = GetDecimal(row.Cell(colSalarioO + 1));
                        personal.SalarioFeb2021 = GetDecimal(row.Cell(colSalarioO + 2));
                        personal.SalarioFeb2022 = GetDecimal(row.Cell(colSalarioO + 3));
                        personal.SalarioFeb2023 = GetDecimal(row.Cell(colSalarioO + 4));
                        personal.SalarioEneFeb2024 = GetDecimal(row.Cell(colSalarioO + 5));
                        personal.SalarioMar2024 = GetDecimal(row.Cell(colSalarioO + 6));
                        personal.SalarioFeb2025 = GetDecimal(row.Cell(colSalarioO + 7));
                        personal.SalarioFeb2026 = GetDecimal(row.Cell(colSalarioO + 8));

                        personal.FechaNacimiento = GetDateOnly(row.Cell(colFechaNac));
                        personal.Edad = GetInt(row.Cell(colEdad));
                        personal.MesesEdad = GetInt(row.Cell(colMes));
                        personal.MesNacimiento = GetString(row.Cell(colMesNacimiento));
                        personal.Generacion = GetString(row.Cell(colGeneracion));
                        personal.Genero = GetString(row.Cell(colGenero));
                        personal.CiudadNacimiento = GetString(row.Cell(colCiudadNac));
                        personal.EstadoCivil = GetString(row.Cell(colEstadoCivil));
                        personal.CorreoPersonal = GetString(row.Cell(colCorreoPersonal));
                        personal.Contacto = GetString(row.Cell(colContacto));
                        personal.DireccionResidencia = GetString(row.Cell(colDireccion));
                        personal.Barrio = GetString(row.Cell(colBarrio));
                        personal.Rh = GetString(row.Cell(colRh));
                        personal.ContactoEmergencia = GetString(row.Cell(colContactoEmerg));
                        personal.Parentesco = GetString(row.Cell(colParentesco));
                        personal.TelefonoEmergencia = GetString(row.Cell(colTelefonoEmerg));
                        personal.TipoContrato = GetString(row.Cell(colTipoContrato));
                        personal.Eps = GetString(row.Cell(colEps));
                        personal.FondoPensiones = GetString(row.Cell(colFondoPensiones));
                        personal.FondoCesantias = GetString(row.Cell(colFondoCesantias));
                        personal.CajaCompensacion = GetString(row.Cell(colCajaComp));
                        personal.Arl = GetString(row.Cell(colArl));
                        personal.TipoCuenta = GetString(row.Cell(colTipoCuenta));
                        personal.NumeroCuenta = GetString(row.Cell(colNoCuenta));
                        personal.Banco = GetString(row.Cell(colBanco));
                        personal.TallaCamisa = GetString(row.Cell(colTallaCamisa));
                        personal.Grupo = GetString(row.Cell(colGrupo));
                        personal.Concepto = GetString(row.Cell(colConcepto));

                        personal.CorreoCorporativo = GetString(row.Cell(colCorreoCorp));
                        personal.UsuarioCorporativo = GetString(row.Cell(colUsuarioCorp));

                        if (personal.Area?.Trim().Equals("Contraloría", StringComparison.OrdinalIgnoreCase) == true)
                            personal.Area = "Contraloria";

                        if (esNuevo) _context.Tbpersonals.Add(personal); else _context.Tbpersonals.Update(personal);
                        if (esNuevo) importados++; else actualizados++;
                    }
                    catch (Exception ex)
                    {
                        errores++; errorDetails.Add($"Fila {rowIndex}: Excepción: {ex.Message}");
                    }
                }

                if (errorDetails.Any()) TempData["ErrorDetalle"] = string.Join("\n", errorDetails.Take(200));

                await _context.SaveChangesAsync();

                await _audit.RegistrarAsync(AuditService.MOD_PERSONAL, "Importar Excel",
                    $"Importados: {importados}, Actualizados: {actualizados}, Errores: {errores}");

                TempData["Exito"] = $"✅ Importación completada.<br>Nuevos: {importados} | Actualizados: {actualizados} | Errores: {errores}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Error al procesar el archivo: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ====================== EXPORTAR EXCEL ======================
        [HttpPost]   // Cambié a POST para mayor seguridad
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarExcel(string? busqueda, string? cedula,
            string? nombre, string? cargo, string? ciudad, bool todo = false)
        {
            if (!TieneAcceso()) return RedirectToAction("Index", "Home");

            IQueryable<Tbpersonal> query = _context.Tbpersonals;

            if (!todo)
            {
                if (!string.IsNullOrEmpty(busqueda))
                    query = query.Where(p =>
                        (p.NombreColaborador != null && p.NombreColaborador.Contains(busqueda)) ||
                        (p.Cargo != null && p.Cargo.Contains(busqueda)) ||
                        (p.CiudadTrabajo != null && p.CiudadTrabajo.Contains(busqueda)));

                if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                    query = query.Where(p => p.CC == cedNum);

                if (!string.IsNullOrEmpty(nombre))
                    query = query.Where(p => p.NombreColaborador != null && p.NombreColaborador.Contains(nombre));

                if (!string.IsNullOrEmpty(cargo))
                    query = query.Where(p => p.Cargo != null && p.Cargo.Contains(cargo));

                if (!string.IsNullOrEmpty(ciudad))
                    query = query.Where(p => p.CiudadTrabajo != null && p.CiudadTrabajo.Contains(ciudad));
            }

            var datos = await query.OrderBy(p => p.NombreColaborador).ToListAsync();
            var bytes = _excel.ExportarPersonal(datos);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Personal_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        private bool TbpersonalExists(int id) =>
            _context.Tbpersonals.Any(e => e.CC == id);

        // ====================== HELPERS PARA IMPORTACIÓN ======================
        private string? GetString(IXLCell cell)
        {
            try { return string.IsNullOrWhiteSpace(cell.GetString()) ? null : cell.GetString().Trim(); }
            catch { return null; }
        }

        private DateOnly? GetDateOnly(IXLCell cell)
        {
            try
            {
                if (cell.IsEmpty()) return null;
                if (cell.TryGetValue(out DateTime dt)) return DateOnly.FromDateTime(dt);
                return DateOnly.TryParse(cell.GetString(), out var d) ? d : null;
            }
            catch { return null; }
        }

        private int? GetInt(IXLCell cell)
        {
            try
            {
                var s = cell.GetString()?.Trim();
                return int.TryParse(s, out var v) ? v : null;
            }
            catch { return null; }
        }

        private decimal? GetDecimal(IXLCell cell)
        {
            try
            {
                var s = cell.GetString()?.Replace("$", "").Replace(",", "").Trim();
                return decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
            }
            catch { return null; }
        }
    }
}