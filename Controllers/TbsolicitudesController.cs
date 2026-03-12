using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers
{
    [Authorize(Policy = "SolicitudesAccess")]
    public class TbsolicitudesController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly NotificacionService _notif;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly FlujoAprobacionService _flujo;

        public TbsolicitudesController(Farmacol1Context context,
                                        NotificacionService notif,
                                        UserManager<IdentityUser> userManager,
                                        FlujoAprobacionService flujo)
        {
            _context = context;
            _notif = notif;
            _userManager = userManager;
            _flujo = flujo;
        }

        // ── Helper: busca personal por UserName o Email ───────────────────
        private async Task<Tbpersonal?> BuscarPersonalActual()
        {
            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var email = userObj?.Email ?? "";

            return await _context.Tbpersonals
                .FirstOrDefaultAsync(p =>
                    p.CorreoCorporativo == userName ||
                    p.UsuarioCorporativo == userName ||
                    p.CorreoCorporativo == email ||
                    p.UsuarioCorporativo == email);
        }

        // ── INDEX ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? busqueda, string? cedula,
    string? nombre, string? tipoSolicitud, string? estado)
        {
            // Intentar leer área desde ViewBag, si no buscar directo en BD
            var userArea = ViewBag.UserArea as string ?? "";
            var userCargo = ViewBag.UserCargo as string ?? "";

            if (string.IsNullOrEmpty(userArea))
            {
                var personalActual = await BuscarPersonalActual();
                userArea = personalActual?.Area ?? "";
                userCargo = personalActual?.Cargo ?? "";
            }

            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            bool esDirectivo = User.IsInRole("Directivo");
            bool esGerente = User.IsInRole("Gerente");
            bool esJefe = User.IsInRole("Jefe");
            bool esCoord = User.IsInRole("Coordinador");
            bool esAsistente = User.IsInRole("Asistente");
            bool esCapHumano = string.Equals(userArea, "Capital Humano",
                                   StringComparison.OrdinalIgnoreCase);
            bool esGerGen = string.Equals(userArea, "Gerencia General",
                                   StringComparison.OrdinalIgnoreCase);

            var query = _context.Tbsolicitudes.AsQueryable();

            if (esAdmin || esRRHH || esDirectivo ||
                (esGerente && (esCapHumano || esGerGen)) ||
                (esCoord && esCapHumano) ||
                (esAsistente && esCapHumano))
            {
                // Ven todas sin filtro
            }
            else if (esGerente || esJefe)
            {
                query = query.Where(s =>
                    (s.Paso1Aprobador != null && s.Paso1Aprobador.Contains(userArea)) ||
                    (s.Paso2Aprobador != null && s.Paso2Aprobador.Contains(userArea)) ||
                    (s.Paso3Aprobador != null && s.Paso3Aprobador.Contains(userArea)));
            }
            else if (esCoord || esAsistente)
            {
                query = query.Where(s =>
                    (s.Paso1Aprobador != null && s.Paso1Aprobador.Contains(userArea)) ||
                    (s.Paso2Aprobador != null && s.Paso2Aprobador.Contains(userArea)) ||
                    (s.Paso3Aprobador != null && s.Paso3Aprobador.Contains(userArea)));
            }
            else
            {
                var personal2 = await BuscarPersonalActual();
                query = personal2 != null
                    ? query.Where(s => s.CC == personal2.CC)
                    : query.Where(s => false);
            }

            // ... resto del método igual

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(s =>
                    (s.Nombre != null && s.Nombre.Contains(busqueda)) ||
                    (s.TipoSolicitud != null && s.TipoSolicitud.Contains(busqueda)) ||
                    (s.Estado != null && s.Estado.Contains(busqueda)));

            if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                query = query.Where(s => s.CC == cedNum);
            if (!string.IsNullOrEmpty(nombre))
                query = query.Where(s => s.Nombre != null && s.Nombre.Contains(nombre));
            if (!string.IsNullOrEmpty(tipoSolicitud))
                query = query.Where(s => s.TipoSolicitud != null && s.TipoSolicitud.Contains(tipoSolicitud));
            if (!string.IsNullOrEmpty(estado))
                query = query.Where(s => s.Estado != null && s.Estado.Contains(estado));

            // Marcar notificaciones como leídas
            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var emailU = userObj?.Email ?? "";
            var notifsIdx = await _context.Tbnotificaciones
                .Where(n => !n.Leida && (n.UsuarioDestino == userName ||
                                         n.UsuarioDestino == emailU))
                .ToListAsync();
            notifsIdx.ForEach(n => n.Leida = true);
            if (notifsIdx.Any()) await _context.SaveChangesAsync();

            ViewBag.Busqueda = busqueda;
            ViewBag.Cedula = cedula;
            ViewBag.Nombre = nombre;
            ViewBag.TipoSolicitud = tipoSolicitud;
            ViewBag.Estado = estado;

            return View(await query
                .OrderByDescending(s => s.FechaSolicitud ?? DateOnly.MinValue)
                .ToListAsync());
        }

        // ── MIS SOLICITUDES ───────────────────────────────────────────────
        public async Task<IActionResult> MisSolicitudes()
        {
            var personal = await BuscarPersonalActual();
            if (personal == null) return View(new List<Tbsolicitude>());

            var solicitudes = await _context.Tbsolicitudes
                .Where(s => s.CC == personal.CC)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToListAsync();

            return View(solicitudes);
        }

        // ── CREATE GET ────────────────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();

            var personal = await BuscarPersonalActual();
            if (personal != null)
            {
                ViewBag.CedulaActual = personal.CC;
                ViewBag.NombreActual = personal.NombreColaborador;
                ViewBag.CargoActual = personal.Cargo;
                ViewBag.AreaActual = personal.Area;
            }

            return View();
        }

        // ── CREATE POST ───────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CC,Nombre,Cargo,TipoSolicitud,SubtipoPermiso,HoraInicio,HoraFin,TotalHoras,FechaInicio,FechaFin,TotalDias,Motivo,FechaSolicitud,Observaciones")]
            Tbsolicitude tbsolicitude, IFormFile? archivoAnexo)
        {
            ModelState.Remove("Motivo"); ModelState.Remove("Anexos");
            ModelState.Remove("Estado"); ModelState.Remove("EtapaAprobacion");
            ModelState.Remove("ObservacionJefe"); ModelState.Remove("ObservacionRRHH");
            ModelState.Remove("AprobJinmediato"); ModelState.Remove("AprobCh");
            ModelState.Remove("SubtipoPermiso"); ModelState.Remove("HoraInicio");
            ModelState.Remove("HoraFin"); ModelState.Remove("TotalHoras");
            ModelState.Remove("FechaInicio"); ModelState.Remove("FechaFin");
            ModelState.Remove("TotalDias"); ModelState.Remove("CC");
            ModelState.Remove("Nombre"); ModelState.Remove("Cargo");
            ModelState.Remove("FechaSolicitud");
            ModelState.Remove("JefeInmediato"); ModelState.Remove("CargoJinmediato");
            ModelState.Remove("Paso1Aprobador"); ModelState.Remove("Paso2Aprobador");
            ModelState.Remove("Paso3Aprobador"); ModelState.Remove("NivelSolicitante");

            if (!ModelState.IsValid)
            {
                ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
                return View(tbsolicitude);
            }

            if (archivoAnexo != null && archivoAnexo.Length > 0)
            {
                var ext = Path.GetExtension(archivoAnexo.FileName).ToLower();
                if (ext != ".pdf")
                {
                    ModelState.AddModelError("", "Solo se permiten archivos PDF.");
                    ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
                    return View(tbsolicitude);
                }
                var nombreArchivo = $"solicitud_{DateTime.Now.Ticks}.pdf";
                var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "anexos");
                Directory.CreateDirectory(carpeta);
                using var stream = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Create);
                await archivoAnexo.CopyToAsync(stream);
                tbsolicitude.Anexos = $"/anexos/{nombreArchivo}";
            }

            var solicitantePersonal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CC == tbsolicitude.CC);

            if (solicitantePersonal == null)
            {
                ModelState.AddModelError("", "No se encontró el perfil del solicitante.");
                ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
                return View(tbsolicitude);
            }

            tbsolicitude = await _flujo.InicializarFlujo(tbsolicitude, solicitantePersonal);

            _context.Add(tbsolicitude);
            await _context.SaveChangesAsync();

            await NotificarAprobador(tbsolicitude, 1);

            TempData["Exito"] = $"✅ Solicitud enviada. Pendiente de: {tbsolicitude.Paso1Aprobador}.";
            return RedirectToAction(nameof(Index));
        }

        // ── REVISAR ───────────────────────────────────────────────────────
        public async Task<IActionResult> Revisar(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var emailU = userObj?.Email ?? "";

            // Marcar notifs leídas
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida && n.IdSolicitud == id &&
                            (n.UsuarioDestino == userName || n.UsuarioDestino == emailU))
                .ToListAsync();
            notifs.ForEach(n => n.Leida = true);
            if (notifs.Any()) await _context.SaveChangesAsync();

            // Pasar email también para que PuedeActuar encuentre el personal
            ViewBag.PuedeActuar = await _flujo.PuedeActuar(solicitud, userName)
                               || await _flujo.PuedeActuar(solicitud, emailU);

            return View(solicitud);
        }

        // ── APROBAR ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id, string? observacion)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            var (completado, siguienteDestino) =
                await _flujo.AvanzarPaso(solicitud, observacion ?? "");

            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            if (completado)
            {
                var personal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.CC == solicitud.CC);
                if (personal != null)
                {
                    var dest = personal.UsuarioCorporativo ?? personal.CorreoCorporativo ?? "";
                    var msg = $"Tu solicitud de {solicitud.TipoSolicitud} ha sido APROBADA definitivamente.";
                    await _notif.CrearNotificacion(dest, msg, id);
                    await _notif.EnviarEmail(personal.CorreoCorporativo ?? "", "Solicitud aprobada - Farmacol", msg);
                }
                TempData["Exito"] = "✅ Solicitud aprobada definitivamente.";
            }
            else
            {
                var pasoSiguiente = solicitud.PasoActual ?? 1;
                await NotificarAprobador(solicitud, pasoSiguiente);
                var cargo = pasoSiguiente == 2 ? solicitud.Paso2Aprobador : solicitud.Paso3Aprobador;
                TempData["Exito"] = $"✅ Aprobado. Enviado al siguiente aprobador: {cargo}.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── RECHAZAR ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id, string? observacion)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            solicitud = _flujo.RechazarPaso(solicitud, observacion ?? "");
            _context.Update(solicitud);

            var existe = await _context.TbsoliRechazada
                .AnyAsync(r => r.IdSolicitud == solicitud.IdSolicitud);
            if (!existe)
            {
                _context.TbsoliRechazada.Add(new TbsoliRechazadum
                {
                    IdSolicitud = solicitud.IdSolicitud,
                    CC = solicitud.CC,
                    Nombre = solicitud.Nombre,
                    TipoSolicitud = solicitud.TipoSolicitud,
                    Motivo = observacion,
                    Observaciones = observacion,
                    FechaSolicitud = solicitud.FechaSolicitud ?? DateOnly.FromDateTime(DateTime.Today),
                    Anexos = solicitud.Anexos
                });
            }

            await _context.SaveChangesAsync();

            var personalSol = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            if (personalSol != null)
            {
                var destino = personalSol.UsuarioCorporativo ?? personalSol.CorreoCorporativo ?? "";
                var msg = $"Tu solicitud de {solicitud.TipoSolicitud} ha sido RECHAZADA. Motivo: {observacion}";
                await _notif.CrearNotificacion(destino, msg, id);
                await _notif.EnviarEmail(personalSol.CorreoCorporativo ?? "", "Solicitud rechazada - Farmacol", msg);
            }

            TempData["Error"] = "❌ Solicitud rechazada.";
            return RedirectToAction(nameof(Index));
        }

        // ── DEVOLVER ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Devolver(int id, string observacion)
        {
            if (string.IsNullOrWhiteSpace(observacion))
            {
                TempData["Error"] = "La observación es obligatoria al devolver.";
                return RedirectToAction(nameof(Revisar), new { id });
            }

            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            solicitud = _flujo.DevolverSolicitud(solicitud, observacion);  
            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            var personal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            if (personal != null)
            {
                var destino = personal.UsuarioCorporativo ?? personal.CorreoCorporativo ?? "";
                var msg = $"Tu solicitud de {solicitud.TipoSolicitud} fue devuelta para corrección. " +
                          $"Motivo: {observacion}. Tienes 3 días para reenviarla o será finalizada.";
                await _notif.CrearNotificacion(destino, msg, id);
                await _notif.EnviarEmail(personal.CorreoCorporativo ?? "", "Solicitud devuelta - Farmacol", msg);
            }

            TempData["Exito"] = "↩️ Solicitud devuelta al solicitante.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDITAR GET (solicitante, solo Devuelta) ───────────────────────
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            var personal = await BuscarPersonalActual();
            if (personal == null || personal.CC != solicitud.CC)
                return Forbid();

            if (solicitud.Estado != "Devuelta")
                return RedirectToAction(nameof(Details), new { id });

            ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
            return View(solicitud);
        }

        // ── REENVIAR ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reenviar(int id, IFormFile? archivoAnexo,
    string? observaciones, string? Motivo, string? SubtipoPermiso,
    string? HoraInicio, string? HoraFin, string? TotalHoras,
    string? FechaInicio, string? FechaFin, string? TotalDias)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            if (solicitud.Estado != "Devuelta")
                return RedirectToAction(nameof(Index));

            if (!string.IsNullOrWhiteSpace(Motivo)) solicitud.Motivo = Motivo;
            if (!string.IsNullOrWhiteSpace(SubtipoPermiso)) solicitud.SubtipoPermiso = SubtipoPermiso;
            if (!string.IsNullOrWhiteSpace(observaciones)) solicitud.Observaciones = observaciones;

            // HoraInicio/HoraFin son int? — vienen como "HH:mm" del input time
            // Convertir "08:30" → 830 o guardar solo la hora como int
            if (!string.IsNullOrWhiteSpace(HoraInicio) &&
                TimeSpan.TryParse(HoraInicio, out var tsI))
                solicitud.HoraInicio = tsI.Hours * 100 + tsI.Minutes;

            if (!string.IsNullOrWhiteSpace(HoraFin) &&
                TimeSpan.TryParse(HoraFin, out var tsF))
                solicitud.HoraFin = tsF.Hours * 100 + tsF.Minutes;

            if (int.TryParse(TotalHoras, out var th)) solicitud.TotalHoras = th;
            if (int.TryParse(TotalDias, out var td)) solicitud.TotalDias = td;

            if (DateOnly.TryParseExact(FechaInicio, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var fi))
                solicitud.FechaInicio = fi;

            if (DateOnly.TryParseExact(FechaFin, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var ff))
                solicitud.FechaFin = ff;

            if (archivoAnexo != null && archivoAnexo.Length > 0)
            {
                var ext = Path.GetExtension(archivoAnexo.FileName).ToLower();
                if (ext != ".pdf")
                {
                    TempData["Error"] = "Solo se permiten archivos PDF.";
                    return RedirectToAction(nameof(Editar), new { id });
                }
                var nombreArchivo = $"solicitud_{DateTime.Now.Ticks}.pdf";
                var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "anexos");
                Directory.CreateDirectory(carpeta);
                using var stream = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Create);
                await archivoAnexo.CopyToAsync(stream);
                solicitud.Anexos = $"/anexos/{nombreArchivo}";
            }

            var personal = await _context.Tbpersonals.FindAsync(solicitud.CC);
            if (personal == null)
            {
                TempData["Error"] = "No se encontró el perfil del solicitante.";
                return RedirectToAction(nameof(Editar), new { id });
            }

            solicitud = await _flujo.InicializarFlujo(solicitud, personal);
            _context.Update(solicitud);
            await _context.SaveChangesAsync();
            await NotificarAprobador(solicitud, 1);

            TempData["Exito"] = "✅ Solicitud reenviada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── FINALIZAR DEVUELTA ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarDevuelta(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            solicitud.Estado = "Finalizada";
            solicitud.EtapaAprobacion = "Cancelada por solicitante";
            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            TempData["Exito"] = "Solicitud finalizada.";
            return RedirectToAction(nameof(Index));
        }

        // ── DETAILS ───────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var s = await _context.Tbsolicitudes.FirstOrDefaultAsync(m => m.IdSolicitud == id);
            if (s == null) return NotFound();

            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var emailU = userObj?.Email ?? "";
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida && n.IdSolicitud == id &&
                            (n.UsuarioDestino == userName || n.UsuarioDestino == emailU))
                .ToListAsync();
            notifs.ForEach(n => n.Leida = true);
            if (notifs.Any()) await _context.SaveChangesAsync();

            return View(s);
        }

        // ── EDIT (admin) ──────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var s = await _context.Tbsolicitudes.FindAsync(id);
            if (s == null) return NotFound();
            ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
            return View(s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("IdSolicitud,CC,Nombre,Cargo,TipoSolicitud,SubtipoPermiso,HoraInicio,HoraFin,TotalHoras,FechaInicio,FechaFin,TotalDias,JefeInmediato,CargoJinmediato,Motivo,FechaSolicitud,AprobJinmediato,AprobCh,ObservacionJefe,ObservacionRRHH,Observaciones,Anexos,Estado,EtapaAprobacion,Paso1Aprobador,Paso1Estado,Paso2Aprobador,Paso2Estado,Paso3Aprobador,Paso3Estado,PasoActual,TotalPasos,NivelSolicitante")]
            Tbsolicitude tbsolicitude)
        {
            if (id != tbsolicitude.IdSolicitud) return NotFound();
            ModelState.Remove("Motivo"); ModelState.Remove("Anexos");
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tbsolicitude);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TbsolicitudeExists(tbsolicitude.IdSolicitud)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
            return View(tbsolicitude);
        }

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var s = await _context.Tbsolicitudes.FirstOrDefaultAsync(m => m.IdSolicitud == id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var s = await _context.Tbsolicitudes.FindAsync(id);
            if (s != null) _context.Tbsolicitudes.Remove(s);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── NOTIFICACIONES ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ContarNotificaciones()
        {
            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var email = userObj?.Email ?? "";
            var count = await _context.Tbnotificaciones
                .CountAsync(n => !n.Leida &&
                                 (n.UsuarioDestino == userName || n.UsuarioDestino == email));
            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarLeidas()
        {
            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var email = userObj?.Email ?? "";
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida &&
                            (n.UsuarioDestino == userName || n.UsuarioDestino == email))
                .ToListAsync();
            notifs.ForEach(n => n.Leida = true);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ── BUSCAR JEFE ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> BuscarJefe(string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return Json(new List<object>());
            var resultados = await _context.Tbpersonals
                .Where(p => p.NombreColaborador != null && p.NombreColaborador.Contains(q))
                .Select(p => new { nombre = p.NombreColaborador, cargo = p.Cargo, area = p.Area })
                .Take(8)
                .ToListAsync();
            return Json(resultados);
        }

        // ── NOTIFICAR APROBADOR ───────────────────────────────────────────
        private async Task NotificarAprobador(Tbsolicitude solicitud, int paso)
        {
            var cargo = paso == 1 ? solicitud.Paso1Aprobador
                      : paso == 2 ? solicitud.Paso2Aprobador
                      : solicitud.Paso3Aprobador;

            if (string.IsNullOrEmpty(cargo)) return;

            var solicitantePersonal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            var area = solicitantePersonal?.Area ?? "";

            Tbpersonal? aprobadorPersonal = null;

            if (cargo == "Capital Humano")
            {
                aprobadorPersonal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p =>
                        p.Area != null && p.Area == "Capital Humano" &&
                        p.Cargo != null && (
                            p.Cargo.StartsWith("Gerente") ||
                            p.Cargo.StartsWith("Coordinador") ||
                            p.Cargo.StartsWith("Asistente")));
            }
            else
            {
                var partes = cargo.Trim().Split(' ', 2);
                var nivel = partes[0];
                var areaFiltro = partes.Length > 1 ? partes[1] : area;

                aprobadorPersonal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p =>
                        p.Cargo != null && p.Cargo.StartsWith(nivel) &&
                        (string.IsNullOrEmpty(areaFiltro) ||
                         (p.Area != null && p.Area.ToLower() == areaFiltro.ToLower())));
            }

            if (aprobadorPersonal == null) return;

            var destino = aprobadorPersonal.UsuarioCorporativo
                       ?? aprobadorPersonal.CorreoCorporativo ?? "";

            bool esFallback = cargo == "Gerente General" &&
                              (solicitud.Paso1Aprobador == "Gerente General" ||
                               solicitud.Paso2Aprobador == "Gerente General" ||
                               solicitud.Paso3Aprobador == "Gerente General");

            var msg = esFallback
                ? $"⚠️ Solicitud de {solicitud.TipoSolicitud} de {solicitud.Nombre} " +
                  $"requiere tu aprobación como Gerente General (actuando por ausencia de aprobador en área {area})."
                : $"Solicitud de {solicitud.TipoSolicitud} de {solicitud.Nombre} " +
                  $"requiere tu aprobación como {cargo}.";

            await _notif.CrearNotificacion(destino, msg, solicitud.IdSolicitud);
            await _notif.EnviarEmail(aprobadorPersonal.CorreoCorporativo ?? "",
                                      "Solicitud pendiente - Farmacol", msg);
        }

        private bool TbsolicitudeExists(int id) =>
            _context.Tbsolicitudes.Any(e => e.IdSolicitud == id);
    }
}