using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Farmacol.Controllers
{
    [Authorize(Policy = "SolicitudesAccess")]
    public class TbsolicitudesController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly NotificacionService _notif;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly FlujoAprobacionService _flujo;
        private readonly DelegacionService _delegacion;
        private readonly EmailService _email;
        private readonly AuditService _audit;
        private readonly ExcelService _excel;
        private readonly DocumentoService _docSvc;
        private readonly IWebHostEnvironment _env;
        private readonly VacacionesService _VacacionesService;

        public TbsolicitudesController(
            Farmacol1Context context,
            NotificacionService notif,
            UserManager<IdentityUser> userManager,
            FlujoAprobacionService flujo,
            DelegacionService delegacion,
            EmailService email,
            AuditService audit,
            ExcelService excel,
            DocumentoService docSvc,
            IWebHostEnvironment env,
            VacacionesService vacaciones)
        {
            _context = context;
            _notif = notif;
            _userManager = userManager;
            _flujo = flujo;
            _delegacion = delegacion;
            _email = email;
            _audit = audit;
            _excel = excel;
            _docSvc = docSvc;
            _env = env;
            _VacacionesService = vacaciones;
        }

        [HttpGet]
        public async Task<IActionResult> DescargarAnexo(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null || string.IsNullOrEmpty(solicitud.Anexos)) return NotFound();

            var personal = await BuscarPersonalActual();
            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            bool esSolicitante = personal != null && personal.CC == solicitud.CC;

            if (!esAdmin && !esRRHH && !esSolicitante) return Forbid();

            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot",
                solicitud.Anexos.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            return PhysicalFile(fullPath, "application/pdf", Path.GetFileName(fullPath));
        }

        [HttpGet]
        [Route("Tbsolicitudes/DescargarDocumento/{id}")]
        public async Task<IActionResult> DescargarDocumento(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            var personalUsuario = await BuscarPersonalActual();
            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            bool esSolicitante = personalUsuario != null && personalUsuario.CC == solicitud.CC;
            if (!esAdmin && !esRRHH && !esSolicitante) return Forbid();

            TbPlantilla? plantilla = null;
            try
            {
                plantilla = await _context.TbPlantillas
                    .Where(p => p.Activa && (p.TipoDocumento ?? "").ToLower().Contains((solicitud.TipoSolicitud ?? "").ToLower()))
                    .OrderByDescending(p => p.FechaSubida)
                    .FirstOrDefaultAsync();
            }
            catch { }

            if (plantilla == null)
            {
                plantilla = await _context.TbPlantillas.Where(p => p.Activa).OrderByDescending(p => p.FechaSubida).FirstOrDefaultAsync();
            }
            if (plantilla == null) return NotFound("No existe plantilla disponible.");

            var rutaPlantillaFisica = Path.Combine(_env.WebRootPath ?? "wwwroot", plantilla.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(rutaPlantillaFisica)) return NotFound("Plantilla no encontrada en disco.");

            var solicitante = await _context.Tbpersonals.FindAsync(solicitud.CC);
            if (solicitante == null) return NotFound("No se encontró el perfil del solicitante.");

            byte[] bytes;
            try
            {
                bytes = await _docSvc.GenerarDocxAsync(rutaPlantillaFisica, solicitante, null, null, User.Identity?.Name, solicitud, null);
            }
            catch
            {
                return StatusCode(500, "Error generando documento.");
            }

            try
            {
                var carpeta = Path.Combine(_env.WebRootPath ?? "wwwroot", "expedientes", solicitante.CC.ToString());
                Directory.CreateDirectory(carpeta);
                var nombre = $"solicitud_{solicitud.IdSolicitud}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                var rutaFisica = Path.Combine(carpeta, nombre);
                await System.IO.File.WriteAllBytesAsync(rutaFisica, bytes);
                var rutaRel = $"/expedientes/{solicitante.CC}/{nombre}";
                _context.TbExpedientes.Add(new TbExpediente
                {
                    CC = solicitante.CC,
                    NombreArchivo = $"Solicitud_{solicitud.IdSolicitud}",
                    TipoDocumento = "Solicitud Generada",
                    RutaArchivo = rutaRel,
                    FechaSubida = DateTime.Now,
                    SubidoPor = User.Identity?.Name ?? ""
                });
                await _context.SaveChangesAsync();
            }
            catch { }

            var fileName = $"Solicitud_{solicitud.IdSolicitud}.docx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private async Task<Tbpersonal?> BuscarPersonalActual()
        {
            var userName = User.Identity?.Name ?? "";
            var userObj = await _userManager.FindByNameAsync(userName);
            var email = userObj?.Email ?? "";
            return await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.CorreoCorporativo == userName ||
                p.UsuarioCorporativo == userName ||
                p.CorreoCorporativo == email ||
                p.UsuarioCorporativo == email);
        }

        private async Task<(string userName, string email)> ObtenerIdentidad()
        {
            var userName = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(userName)) return ("", "");

            // Buscar en Tbpersonals primero
            var personal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.UsuarioCorporativo == userName ||
                                           p.CorreoCorporativo == userName);

            if (personal != null)
                return (personal.UsuarioCorporativo ?? userName,
                        personal.CorreoCorporativo ?? "");

            //Buscar usuarios seedeados
            var identityUser = await _userManager.FindByNameAsync(userName);
            var email = identityUser?.Email ?? "";

            return (userName, email);
        }

        public async Task<IActionResult> Index(string? busqueda, string? cedula,
            string? nombre, string? tipoSolicitud, string? estado)
        {
            var userArea = ViewBag.UserArea as string ?? "";
            var userCargo = ViewBag.UserCargo as string ?? "";
            if (string.IsNullOrEmpty(userArea))
            {
                var p = await BuscarPersonalActual();
                userArea = p?.Area ?? "";
                userCargo = p?.Cargo ?? "";
            }

            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            bool esDirectivo = User.IsInRole("Directivo");
            bool esGerente = User.IsInRole("Gerente");
            bool esJefe = User.IsInRole("Jefe");
            bool esCoord = User.IsInRole("Coordinador");
            bool esAsistente = User.IsInRole("Asistente");

            bool esGerGen = string.Equals(userArea, "Gerencia General",
                                StringComparison.OrdinalIgnoreCase);
            bool esGerenteCapHumano = string.Equals(userCargo, "Gerente Capital Humano",
                                          StringComparison.OrdinalIgnoreCase);
            bool esCoordCapHumano = string.Equals(userCargo, "Coordinador Capital Humano",
                                          StringComparison.OrdinalIgnoreCase);
            bool esAsistCapHumano = string.Equals(userCargo, "Asistente Capital Humano",
                                          StringComparison.OrdinalIgnoreCase);

            var query = _context.Tbsolicitudes.AsQueryable();

            if (esAdmin || esRRHH || esDirectivo || esGerenteCapHumano || (esGerente && esGerGen))
            {
            }
            else if (esCoordCapHumano || esAsistCapHumano || esGerente || esJefe || esCoord || esAsistente)
            {
                if (esCoordCapHumano || esAsistCapHumano)
                {
                    query = query.Where(s =>
                        (s.Paso1Aprobador != null && (s.Paso1Aprobador == userCargo || s.Paso1Aprobador == "Capital Humano")) ||
                        (s.Paso2Aprobador != null && (s.Paso2Aprobador == userCargo || s.Paso2Aprobador == "Capital Humano")) ||
                        (s.Paso3Aprobador != null && (s.Paso3Aprobador == userCargo || s.Paso3Aprobador == "Capital Humano")));
                }
                else
                {
                    query = query.Where(s =>
                        (s.Paso1Aprobador != null && s.Paso1Aprobador.Contains(userArea)) ||
                        (s.Paso2Aprobador != null && s.Paso2Aprobador.Contains(userArea)) ||
                        (s.Paso3Aprobador != null && s.Paso3Aprobador.Contains(userArea)));
                }
            }
            else
            {
                var personal = await BuscarPersonalActual();
                query = personal != null
                    ? query.Where(s => s.CC == personal.CC)
                    : query.Where(s => false);
            }

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

            ViewBag.Busqueda = busqueda;
            ViewBag.Cedula = cedula;
            ViewBag.Nombre = nombre;
            ViewBag.TipoSolicitud = tipoSolicitud;
            ViewBag.Estado = estado;

            return View(await query
                .OrderByDescending(s => s.FechaSolicitud ?? DateOnly.MinValue)
                .ToListAsync());
        }

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

        [HttpGet]
        public async Task<IActionResult> Create(string? tipo = null)
        {
            var personal = await BuscarPersonalActual();
            if (personal == null)
                return RedirectToAction(nameof(Index));

            var hoy = DateTime.Today;
            bool vacacionesInhabilitadas = hoy.Day >= 11;

            ViewBag.VacacionesInhabilitadas = vacacionesInhabilitadas;
            ViewBag.MesActual = hoy.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CO"));
            ViewBag.MesSiguiente = hoy.AddMonths(1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CO"));
            ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();

            ViewBag.CedulaActual = personal.CC;
            ViewBag.NombreActual = personal.NombreColaborador;
            ViewBag.CargoActual = personal.Cargo;
            ViewBag.AreaActual = personal.Area;

            try
            {
                ViewBag.Vacaciones = await _VacacionesService.CalcularVacacionesAsync(personal.CC);
            }
            catch
            {
                ViewBag.Vacaciones = null;
            }

            if (tipo?.ToLower() == "vacaciones")
                return View("CreateVacaciones");

            if (tipo?.ToLower() == "permiso")
                return View("CreatePermiso");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CC,Nombre,Cargo,TipoSolicitud,SubtipoPermiso,HoraInicio,HoraFin,TotalHoras," +
          "FechaInicio,FechaFin,TotalDias,Motivo,FechaSolicitud,Observaciones,DiasEnDinero")]
    Tbsolicitude tbsolicitude, IFormFile? archivoAnexo)
        {
            async Task<IActionResult> VolverConError(string errorMsg = "")
            {
                if (!string.IsNullOrEmpty(errorMsg))
                    ModelState.AddModelError("", errorMsg);

                var p = await BuscarPersonalActual();
                ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
                ViewBag.CedulaActual = p?.CC;
                ViewBag.NombreActual = p?.NombreColaborador;
                ViewBag.CargoActual = p?.Cargo;
                ViewBag.AreaActual = p?.Area;

                if (p != null)
                {
                    try { ViewBag.Vacaciones = await _VacacionesService.CalcularVacacionesAsync(p.CC); }
                    catch { ViewBag.Vacaciones = null; }
                }

                string vista = tbsolicitude.TipoSolicitud?.Equals("Vacaciones", StringComparison.OrdinalIgnoreCase) == true
                    ? "CreateVacaciones"
                    : "CreatePermiso";

                return View(vista, tbsolicitude);
            }

            if (TimeSpan.TryParse(Request.Form["HoraInicio"], out TimeSpan hi))
                tbsolicitude.HoraInicio = hi;

            if (TimeSpan.TryParse(Request.Form["HoraFin"], out TimeSpan hf))
                tbsolicitude.HoraFin = hf;

            if (decimal.TryParse(Request.Form["TotalHoras"].ToString().Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal horas))
            {
                tbsolicitude.TotalHoras = horas;
            }

            if (decimal.TryParse(Request.Form["TotalDias"].ToString().Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dias))
            {
                tbsolicitude.TotalDias = dias;
            }

            if (!ModelState.IsValid)
                return await VolverConError("Por favor verifica los datos ingresados.");

            if (archivoAnexo != null && archivoAnexo.Length > 0)
            {
                if (Path.GetExtension(archivoAnexo.FileName).ToLower() != ".pdf")
                    return await VolverConError("Solo se permiten archivos PDF como anexo.");

                var nombreArchivo = $"anexo_{DateTime.Now.Ticks}.pdf";
                var carpeta = Path.Combine(_env.WebRootPath ?? "wwwroot", "anexos");
                Directory.CreateDirectory(carpeta);

                using var stream = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Create);
                await archivoAnexo.CopyToAsync(stream);
                tbsolicitude.Anexos = $"/anexos/{nombreArchivo}";
            }

            var solicitante = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == tbsolicitude.CC);
            if (solicitante == null)
                return await VolverConError("No se encontró el perfil del solicitante.");

            if (tbsolicitude.TipoSolicitud?.Equals("Vacaciones", StringComparison.OrdinalIgnoreCase) == true)
            {
                var diasSolicitados = tbsolicitude.TotalDias ?? 0m;
                var diasEnDinero = tbsolicitude.DiasEnDinero ?? 0;

                var vac = await _VacacionesService.CalcularVacacionesAsync(solicitante.CC);
                var disponibles = (int)Math.Floor(vac.DiasDisponibles);

                if (diasSolicitados + diasEnDinero > disponibles)
                    return await VolverConError($"No tienes suficientes días disponibles. Disponibles: {disponibles}.");

                var maxDinero = (int)Math.Floor(disponibles * 0.5);
                if (diasEnDinero > maxDinero)
                    return await VolverConError($"No puedes solicitar más de {maxDinero} días en dinero.");

                if (diasSolicitados <= 0)
                    return await VolverConError("Debes seleccionar un rango válido de fechas.");
            }

            bool esSST = User.IsInRole("SST");
            tbsolicitude = await _flujo.InicializarFlujo(tbsolicitude, solicitante, esSST);

            _context.Add(tbsolicitude);
            await _context.SaveChangesAsync();

            await NotificarAprobador(tbsolicitude, 1);

            try { await _email.NotificarSolicitudCreadaAsync(tbsolicitude); } catch { }
            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES, AuditService.ACC_CREAR,
                    $"{tbsolicitude.Nombre} creó solicitud de {tbsolicitude.TipoSolicitud}",
                    tbsolicitude.IdSolicitud.ToString());
            }
            catch { }

            TempData["Exito"] = $"✅ Solicitud enviada. Pendiente de: {tbsolicitude.Paso1Aprobador}.";
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Revisar(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            var (userName, email) = await ObtenerIdentidad();
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida && n.IdSolicitud == id &&
                            (n.UsuarioDestino == userName || n.UsuarioDestino == email))
                .ToListAsync();
            notifs.ForEach(n => n.Leida = true);
            if (notifs.Any()) await _context.SaveChangesAsync();

            ViewBag.PuedeActuar = await _flujo.PuedeActuar(solicitud, userName)
                               || await _flujo.PuedeActuar(solicitud, email);
            return View(solicitud);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(int id, string? observacion)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();

            var (completado, _) = await _flujo.AvanzarPaso(solicitud, observacion ?? "");
            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            try
            {
                await _audit.RegistrarAsync(
                    AuditService.MOD_SOLICITUDES,
                    completado ? AuditService.ACC_APROBAR : "Aprobación parcial",
                    $"Solicitud #{id} aprobada por {User.Identity?.Name}.",
                    id.ToString()
                );
            }
            catch { }

            if (!completado)
            {
                var pasoSig = solicitud.PasoActual ?? 1;
                await NotificarAprobador(solicitud, pasoSig);
                try { await _email.NotificarSiguienteAprobadorAsync(solicitud); } catch { }

                TempData["Exito"] = $"✅ Aprobado. Enviado a: {solicitud.EtapaAprobacion}.";
                return RedirectToAction(nameof(Index));
            }

            try { await _email.NotificarSolicitudAprobadaAsync(solicitud); } catch { }

            var personal = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            if (personal == null)
            {
                TempData["Exito"] = "✅ Solicitud aprobada.";
                return RedirectToAction(nameof(Index));
            }

            var esVacaciones = solicitud.TipoSolicitud?.Contains("Vacaciones", StringComparison.OrdinalIgnoreCase) == true;

            await _notif.CrearNotificacion(
                personal.UsuarioCorporativo ?? personal.CorreoCorporativo ?? "",
                $"Tu solicitud de {solicitud.TipoSolicitud} fue APROBADA.",
                id
            );

            if (esVacaciones && solicitud.FechaInicio.HasValue && solicitud.FechaFin.HasValue)
            {
                await _delegacion.CrearDelegacion(
                    personal.CC,
                    personal.NombreColaborador ?? "",
                    personal.Cargo ?? "",
                    personal.Area ?? "",
                    "Vacaciones aprobadas automáticamente",
                    solicitud.FechaInicio.Value,
                    solicitud.FechaFin.Value,
                    User.Identity?.Name ?? ""
                );

                IdentityUser? identityUser = await _userManager.Users.FirstOrDefaultAsync(u =>
                    u.UserName == personal.UsuarioCorporativo ||
                    u.Email == personal.CorreoCorporativo ||
                    u.NormalizedUserName == (personal.UsuarioCorporativo ?? "").ToUpper() ||
                    u.NormalizedEmail == (personal.CorreoCorporativo ?? "").ToUpper()
                );

                if (identityUser != null)
                {
                    await _userManager.SetLockoutEnabledAsync(identityUser, true);


                    var lockoutEndUtc = solicitud.FechaFin.Value
                        .ToDateTime(TimeOnly.MaxValue) 
                        .ToUniversalTime();

                    await _userManager.SetLockoutEnabledAsync(identityUser, true);
                    await _userManager.SetLockoutEndDateAsync(identityUser, lockoutEndUtc);
                    await _userManager.UpdateAsync(identityUser);

                }


                var fi = solicitud.FechaInicio.Value.ToString("dd/MM/yyyy");
                var ff = solicitud.FechaFin.Value.ToString("dd/MM/yyyy");
                TempData["Exito"] = $"✅ Aprobada. {personal.NombreColaborador} inhabilitado del {fi} al {ff}.";

            }
            else
            {
                TempData["Exito"] = "✅ Solicitud aprobada definitivamente.";
            }



            return RedirectToAction(nameof(Index));
        }

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

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES, AuditService.ACC_RECHAZAR,
                $"Solicitud #{id} rechazada por {User.Identity?.Name}. Motivo: {observacion}",
                id.ToString());
            }
            catch { }

            var personal = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            if (personal != null)
            {
                var dest = personal.UsuarioCorporativo ?? personal.CorreoCorporativo ?? "";
                await _notif.CrearNotificacion(dest,
                    $"Tu solicitud de {solicitud.TipoSolicitud} fue RECHAZADA. Motivo: {observacion}", id);
                try { await _email.NotificarSolicitudRechazadaAsync(solicitud, observacion); } catch { }
            }

            TempData["Error"] = "❌ Solicitud rechazada.";
            return RedirectToAction(nameof(Index));
        }

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

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES, AuditService.ACC_DEVOLVER,
                $"Solicitud #{id} devuelta por {User.Identity?.Name}. Motivo: {observacion}",
                id.ToString());
            }
            catch { }

            var personal = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            if (personal != null)
            {
                var dest = personal.UsuarioCorporativo ?? personal.CorreoCorporativo ?? "";
                await _notif.CrearNotificacion(dest,
                    $"Tu solicitud de {solicitud.TipoSolicitud} fue devuelta. Motivo: {observacion}. " +
                    "Tienes 3 días para reenviarla.", id);
                try { await _email.NotificarSolicitudDevueltaAsync(solicitud, observacion); } catch { }
            }

            TempData["Exito"] = "↩️ Solicitud devuelta al solicitante.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            var personal = await BuscarPersonalActual();
            if (personal == null || personal.CC != solicitud.CC) return Forbid();
            if (solicitud.Estado != "Devuelta")
                return RedirectToAction(nameof(Details), new { id });
            ViewBag.SubtiposPermiso = await _context.TbsubtiposPermisos.ToListAsync();
            return View(solicitud);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reenviar(int id, IFormFile? archivoAnexo,
            string? observaciones, string? Motivo, string? SubtipoPermiso,
            string? HoraInicio, string? HoraFin, string? TotalHoras,
            string? FechaInicio, string? FechaFin, string? TotalDias)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            if (solicitud.Estado != "Devuelta") return RedirectToAction(nameof(Index));

            if (!string.IsNullOrWhiteSpace(Motivo)) solicitud.Motivo = Motivo;
            if (!string.IsNullOrWhiteSpace(SubtipoPermiso)) solicitud.SubtipoPermiso = SubtipoPermiso;
            if (!string.IsNullOrWhiteSpace(observaciones)) solicitud.Observaciones = observaciones;

            if (!string.IsNullOrWhiteSpace(HoraInicio) && TimeSpan.TryParse(HoraInicio, out TimeSpan tsInicio))
                solicitud.HoraInicio = tsInicio;

            if (!string.IsNullOrWhiteSpace(HoraFin) && TimeSpan.TryParse(HoraFin, out TimeSpan tsFin))
                solicitud.HoraFin = tsFin;

            if (decimal.TryParse(TotalHoras?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal totalH))
                solicitud.TotalHoras = totalH;

            if (decimal.TryParse(TotalDias?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal totalD))
                solicitud.TotalDias = totalD;

            if (DateOnly.TryParseExact(FechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fi))
                solicitud.FechaInicio = fi;

            if (DateOnly.TryParseExact(FechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ff))
                solicitud.FechaFin = ff;

            if (archivoAnexo != null && archivoAnexo.Length > 0)
            {
                if (Path.GetExtension(archivoAnexo.FileName).ToLower() != ".pdf")
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

            bool esSST = User.IsInRole("SST");
            solicitud = await _flujo.InicializarFlujo(solicitud, personal, esSST);

            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            await NotificarAprobador(solicitud, 1);

            try { await _email.NotificarSolicitudCreadaAsync(solicitud); } catch { }
            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES, AuditService.ACC_REENVIAR,
                    $"Solicitud #{id} reenviada por {User.Identity?.Name}",
                    id.ToString());
            }
            catch { }

            TempData["Exito"] = "✅ Solicitud reenviada correctamente.";
            return RedirectToAction(nameof(Index));
        }

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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var s = await _context.Tbsolicitudes.FirstOrDefaultAsync(m => m.IdSolicitud == id);
            if (s == null) return NotFound();
            var (userName, email) = await ObtenerIdentidad();
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida && n.IdSolicitud == id &&
                            (n.UsuarioDestino == userName || n.UsuarioDestino == email))
                .ToListAsync();
            notifs.ForEach(n => n.Leida = true);
            if (notifs.Any()) await _context.SaveChangesAsync();
            return View(s);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var solicitud = await _context.Tbsolicitudes.FindAsync(id);
            if (solicitud == null) return NotFound();
            var solicitante = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            ViewBag.SolicitanteUsuario = solicitante?.UsuarioCorporativo;
            ViewBag.SolicitanteEmail = solicitante?.CorreoCorporativo;
            ViewBag.SolicitanteCC = solicitante?.CC;
            return View(solicitud);
        }

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
            [Bind("IdSolicitud,CC,Nombre,Cargo,TipoSolicitud,SubtipoPermiso,HoraInicio,HoraFin," +
                  "TotalHoras,FechaInicio,FechaFin,TotalDias,JefeInmediato,CargoJinmediato,Motivo," +
                  "FechaSolicitud,AprobJinmediato,AprobCh,ObservacionJefe,ObservacionRRHH," +
                  "Observaciones,Anexos,Estado,EtapaAprobacion,Paso1Aprobador,Paso1Estado," +
                  "Paso2Aprobador,Paso2Estado,Paso3Aprobador,Paso3Estado,PasoActual," +
                  "TotalPasos,NivelSolicitante")]
            Tbsolicitude tbsolicitude)
        {
            if (id != tbsolicitude.IdSolicitud) return NotFound();
            ModelState.Remove("Motivo"); ModelState.Remove("Anexos");
            if (ModelState.IsValid)
            {
                try { _context.Update(tbsolicitude); await _context.SaveChangesAsync(); }
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

        [HttpGet]
        public async Task<IActionResult> ContarNotificaciones()
        {
            var (userName, email) = await ObtenerIdentidad();
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida && (n.UsuarioDestino == userName || n.UsuarioDestino == email))
                .ToListAsync();
            var distinctCount = notifs
                .GroupBy(n => new { n.Mensaje, n.IdSolicitud })
                .Count();
            return Json(distinctCount);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerNotificaciones()
        {
            var (userName, email) = await ObtenerIdentidad();
            var raw = await _context.Tbnotificaciones
                .Where(n => n.UsuarioDestino == userName || n.UsuarioDestino == email)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(40)
                .ToListAsync();

            var grouped = raw
                .GroupBy(n => new { n.Mensaje, n.IdSolicitud })
                .Select(g => g.OrderByDescending(x => x.FechaCreacion).First())
                .Take(20)
                .Select(n => new { n.IdNotificacion, n.Mensaje, n.Leida, n.IdSolicitud, n.FechaCreacion })
                .ToList();

            return Json(grouped);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarLeidas()
        {
            var (userName, email) = await ObtenerIdentidad();
            var notifs = await _context.Tbnotificaciones
                .Where(n => !n.Leida &&
                            (n.UsuarioDestino == userName || n.UsuarioDestino == email))
                .ToListAsync();
            notifs.ForEach(n => n.Leida = true);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> BuscarJefe(string q)
        {
            if (string.IsNullOrEmpty(q) || q.Length < 2) return Json(new List<object>());
            var resultados = await _context.Tbpersonals
                .Where(p => p.NombreColaborador != null && p.NombreColaborador.Contains(q))
                .Select(p => new { nombre = p.NombreColaborador, cargo = p.Cargo, area = p.Area })
                .Take(8).ToListAsync();
            return Json(resultados);
        }

        private async Task NotificarAprobador(Tbsolicitude solicitud, int paso)
        {
            var cargo = paso == 1 ? solicitud.Paso1Aprobador
                      : paso == 2 ? solicitud.Paso2Aprobador
                      : solicitud.Paso3Aprobador;
            if (string.IsNullOrEmpty(cargo)) return;

            var solicitantePersonal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CC == solicitud.CC);
            var area = solicitantePersonal?.Area ?? "";

            Tbpersonal? aprobadorPersonal;
            if (cargo == "Capital Humano")
            {
                aprobadorPersonal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p =>
                        p.Area != null && (p.Area == "Capital Humano" || p.Area == "RRHH") &&
                        p.Cargo != null && (p.Cargo.StartsWith("Gerente") ||
                            p.Cargo.StartsWith("Coordinador") || p.Cargo.StartsWith("Asistente")));
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

            var destino = aprobadorPersonal.UsuarioCorporativo ?? aprobadorPersonal.CorreoCorporativo ?? "";
            var msg = cargo == "Gerente General"
                ? $"⚠️ Solicitud de {solicitud.TipoSolicitud} de {solicitud.Nombre} requiere tu aprobación como Gerente General."
                : $"Solicitud de {solicitud.TipoSolicitud} de {solicitud.Nombre} requiere tu aprobación como {cargo}.";
            await _notif.CrearNotificacion(destino, msg, solicitud.IdSolicitud);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(
        string? busqueda, string? cedula, string? nombre,
        string? tipoSolicitud, string? estado, bool todo = false)
        {
            IQueryable<Tbsolicitude> query = _context.Tbsolicitudes;
            if (!todo)
            {
                if (!string.IsNullOrEmpty(busqueda))
                    query = query.Where(s =>
                        (s.Nombre != null && s.Nombre.Contains(busqueda)) ||
                        (s.TipoSolicitud != null && s.TipoSolicitud.Contains(busqueda)));
                if (!string.IsNullOrEmpty(cedula) && int.TryParse(cedula, out int cedNum))
                    query = query.Where(s => s.CC == cedNum);
                if (!string.IsNullOrEmpty(nombre))
                    query = query.Where(s => s.Nombre != null && s.Nombre.Contains(nombre));
                if (!string.IsNullOrEmpty(tipoSolicitud))
                    query = query.Where(s => s.TipoSolicitud != null && s.TipoSolicitud.Contains(tipoSolicitud));
                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(s => s.Estado != null && s.Estado.Contains(estado));
            }
            var datos = await query.OrderByDescending(s => s.FechaSolicitud ?? DateOnly.MinValue).ToListAsync();
            var bytes = _excel.ExportarSolicitudes(datos);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Solicitudes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        private bool TbsolicitudeExists(int id) =>
            _context.Tbsolicitudes.Any(e => e.IdSolicitud == id);
    }
}