using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Farmacol.Controllers
{
    [Authorize]
    public class TbSolicitudTercerosController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly NotificacionService _notif;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;
        public TbSolicitudTercerosController(
            Farmacol1Context context,
            NotificacionService notif,
            Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _notif = notif;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Vigilancia")) return Forbid();

            var personal = await BuscarPersonalActual();

            var model = new TbSolicitudTerceros
            {
                Solicitante = personal?.NombreColaborador,
                Cargo = personal?.Cargo,
                Area = personal?.Area,
                FechaRegistro = DateOnly.FromDateTime(DateTime.Now)
            };

            return View(model);
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    TbSolicitudTerceros model,
    IFormFile? archivoPlanilla,
    IFormFile? archivoIdentificacion,
    IFormFile? archivoCursos)
{
    if (User.IsInRole("Vigilancia")) return Forbid();

    var personal = await BuscarPersonalActual();
    if (personal == null)
    {
        ModelState.AddModelError("", "No se pudo identificar al solicitante.");
        return View(model);
    }

    // Asignar datos del solicitante
    model.Solicitante = personal.NombreColaborador;
    model.Cargo = personal.Cargo;
    model.Area = personal.Area;
    model.FechaRegistro = DateOnly.FromDateTime(DateTime.Now);

    // Capturar nombres de terceros
    var nombresForm = Request.Form["NombresTerceros[]"].ToArray();
    if (nombresForm.Any())
        model.NombresTerceros = string.Join(";", nombresForm.Where(x => !string.IsNullOrWhiteSpace(x)));

    // Capturar toggles
    model.DocumentacionSST = Request.Form["DocumentacionSST"].ToString();
    model.RequiereCursosEspeciales = Request.Form["RequiereCursosEspeciales"].ToString();
    model.RequiereEPP = Request.Form["RequiereEPP"].ToString();

    // Elementos EPP
    var eppSeleccionados = Request.Form["eppItems"].ToList();
    if (eppSeleccionados.Any())
        model.ElementoEPP = string.Join(";", eppSeleccionados);

    // Vehículo
    model.IngresoVehiculo = Request.Form["IngresoVehiculo"].ToString();
    model.PlacaVehiculo = Request.Form["PlacaVehiculo"].ToString();

    // Subir archivos (solo si el toggle correspondiente es "Sí")
    if (model.DocumentacionSST == "Sí")
    {
        model.PlanillaDePago = await GuardarArchivo(archivoPlanilla, "planilla");
        model.Identificacion = await GuardarArchivo(archivoIdentificacion, "identificacion");
    }

    if (model.RequiereCursosEspeciales == "Sí")
    {
        model.CursosEspeciales = await GuardarArchivo(archivoCursos, "cursos");
    }

    if (!ModelState.IsValid)
        return View(model);

    try
    {
        _context.TbSolicitudTerceros.Add(model);
        await _context.SaveChangesAsync();

        // Notificar a SST
        var usuariosSST = await _userManager.GetUsersInRoleAsync("SST");
        foreach (var u in usuariosSST)
        {
            await _notif.CrearNotificacion(u.UserName ?? "",
                $"Nueva solicitud de terceros #{model.Id} requiere revisión.", model.Id);
        }

        TempData["Exito"] = "Solicitud de ingreso de terceros registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
        return View(model);
    }
}
        private async Task<string?> GuardarArchivo(IFormFile? archivo, string prefijo)
        {
            if (archivo == null || archivo.Length == 0) return null;
            if (Path.GetExtension(archivo.FileName).ToLower() != ".pdf")
                throw new Exception($"El archivo {prefijo} debe ser PDF.");

            var nombre = $"{prefijo}_{DateTime.Now.Ticks}.pdf";
            var carpeta = Path.Combine(_env.WebRootPath ?? "wwwroot", "anexos_terceros");
            Directory.CreateDirectory(carpeta);
            var ruta = Path.Combine(carpeta, nombre);
            using var stream = new FileStream(ruta, FileMode.Create);
            await archivo.CopyToAsync(stream);
            return $"/anexos_terceros/{nombre}";
        }

        public async Task<IActionResult> Index()
        {
            bool esAdmin = User.IsInRole("Administrador");
            bool esSST = User.IsInRole("SST");
            bool esRecepcionista = User.IsInRole("Recepcionista");
            bool esVigilancia = User.IsInRole("Vigilancia");

            var personalActual = await BuscarPersonalActual();
            bool esGerenteCH = personalActual != null &&
                               string.Equals(personalActual.Cargo, "Gerente Capital Humano", StringComparison.OrdinalIgnoreCase);
            bool esGerenteGeneral = personalActual != null &&
                               string.Equals(personalActual.Cargo, "Gerente General", StringComparison.OrdinalIgnoreCase);

            var query = _context.TbSolicitudTerceros.AsQueryable();

            if (esAdmin || esSST || esRecepcionista || esGerenteCH || esGerenteGeneral)
            {
            }
            else if (esVigilancia)
            {
                query = query.Where(s => s.Estado == "Aprobada");
            }
            else
            {
                var personal = await BuscarPersonalActual();
                if (personal != null)
                    query = query.Where(s => s.Solicitante == personal.NombreColaborador);
                else
                    query = query.Where(s => false);
            }

            var solicitudes = await query.OrderByDescending(s => s.FechaRegistro ?? DateOnly.MinValue).ToListAsync();
            return View(solicitudes);
        }

        [Authorize(Roles = "SST,Administrador,RRHH,Gerente Capital Humano,Gerente General,Recepcionista")]
        public async Task<IActionResult> Revisar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var solicitud = await _context.TbSolicitudTerceros.FindAsync(id);
            if (solicitud == null)
            {
                return NotFound();
            }

            var personal = await BuscarPersonalActual();

            bool esSST = User.IsInRole("SST");
            bool esRecepcionista = User.IsInRole("Recepcionista");
            bool esGerenteCH = User.IsInRole("Gerente Capital Humano");
            bool esGerenteGeneral = User.IsInRole("Gerente General");
            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            
            bool puedeRevisar = esSST || esRecepcionista || esGerenteCH || esGerenteGeneral || esAdmin || esRRHH;

            if (!puedeRevisar)
            {
                return Forbid();
            }
            
            bool estaPendiente = string.IsNullOrEmpty(solicitud.Estado) || solicitud.Estado == "Pendiente" || solicitud.Estado == "En proceso";

            if (!estaPendiente)
            {
                TempData["Error"] = "Esta solicitud ya ha sido procesada.";
                return RedirectToAction(nameof(Index));
            }

            return View("Revisar", solicitud); 
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var solicitud = await _context.TbSolicitudTerceros.FindAsync(id);
            if (solicitud == null) return NotFound();

            var personal = await BuscarPersonalActual();

            bool esSST = User.IsInRole("SST");
            bool esVigilancia = User.IsInRole("Vigilancia");
            bool esRecepcionista = User.IsInRole("Recepcionista");
            bool esGerenteCH = User.IsInRole("Gerente Capital Humano");
            bool esGerenteGeneral = User.IsInRole("Gerente General");
            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            

            if (esAdmin || esRRHH || esGerenteCH || esGerenteGeneral || esSST || esRecepcionista)
            {
                return View(solicitud);
            }

            if (esVigilancia)
            {
                if (solicitud.Estado == "Aprobada")
                    return View(solicitud);
                else
                    return Forbid();
            }

            if (personal != null && solicitud.Solicitante == personal.NombreColaborador)
            {
                return View(solicitud);
            }

            return Forbid();  
        }

        private async Task<Tbpersonal?> BuscarPersonalActual()
        {
            var userName = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(userName)) return null;

            return await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == userName || p.CorreoCorporativo == userName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SST,Administrador,RRHH,Gerente Capital Humano,Gerente General,Recepcionista")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var solicitud = await _context.TbSolicitudTerceros.FindAsync(id);
            if (solicitud == null) return NotFound();

            if (!string.IsNullOrEmpty(solicitud.Estado) && solicitud.Estado != "Pendiente" && solicitud.Estado != "En proceso")
            {
                TempData["Error"] = "Esta solicitud ya ha sido procesada.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = "Aprobada";
            solicitud.FechaAprobacionSST = DateTime.Now;
            solicitud.AprobadoPor = User.Identity?.Name;

            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            // Mensaje común para todas las notificaciones
            var mensaje = $"✅ Solicitud de terceros #{solicitud.Id} APROBADA. Solicitante: {solicitud.Solicitante}, Empresa: {solicitud.Empresa}.";

            // 1. Notificar al solicitante
            try
            {
                var solicitante = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.NombreColaborador == solicitud.Solicitante);
                if (solicitante != null)
                {
                    var destino = solicitante.UsuarioCorporativo ?? solicitante.CorreoCorporativo;
                    if (!string.IsNullOrEmpty(destino))
                        await _notif.CrearNotificacion(destino, mensaje, solicitud.Id);
                }
            }
            catch { }

            // 2. Notificar a Gerente Capital Humano (cargo exacto)
            try
            {
                var gerenteCH = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.Cargo == "Gerente Capital Humano");
                if (gerenteCH != null)
                {
                    var destino = gerenteCH.UsuarioCorporativo ?? gerenteCH.CorreoCorporativo;
                    if (!string.IsNullOrEmpty(destino))
                        await _notif.CrearNotificacion(destino, mensaje, solicitud.Id);
                }
            }
            catch { }

            // 3. Notificar a vigilancia (usuario específico)
            try
            {
                var userVigilancia = await _userManager.FindByNameAsync("vigilancia");
                if (userVigilancia != null)
                {
                    var destino = userVigilancia.UserName;
                    var destinoEmail = userVigilancia.Email;
                    if (!string.IsNullOrEmpty(destino))
                        await _notif.CrearNotificacion(destino, mensaje, solicitud.Id);
                    if (!string.IsNullOrEmpty(destinoEmail) && destinoEmail != destino)
                        await _notif.CrearNotificacion(destinoEmail, mensaje, solicitud.Id);
                }
                else
                {
                    var vigilantes = await _userManager.GetUsersInRoleAsync("Vigilancia");
                    foreach (var u in vigilantes)
                    {
                        var dest = u.UserName ?? u.Email;
                        if (!string.IsNullOrEmpty(dest))
                            await _notif.CrearNotificacion(dest, mensaje, solicitud.Id);
                    }
                }
            }
            catch { }

            // 4. Notificar a Recepcionistas
            try
            {
                var recepcionistas = await _userManager.GetUsersInRoleAsync("Recepcionista");
                foreach (var user in recepcionistas)
                {
                    var destino = user.UserName ?? user.Email;
                    if (!string.IsNullOrEmpty(destino))
                        await _notif.CrearNotificacion(destino, mensaje, solicitud.Id);
                }
            }
            catch { }

            TempData["Exito"] = "Solicitud aprobada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SST,Administrador,RRHH,Gerente Capital Humano,Recepcionista")]
        public async Task<IActionResult> Devolver(int id, string observacion)
        {
            if (string.IsNullOrEmpty(observacion))
            {
                TempData["Error"] = "Debe proporcionar una observación para devolver la solicitud.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var solicitud = await _context.TbSolicitudTerceros.FindAsync(id);
            if (solicitud == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(solicitud.Estado) && solicitud.Estado != "Pendiente" && solicitud.Estado != "En proceso")
            {
                TempData["Error"] = "Esta solicitud ya ha sido procesada.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = "Devuelta";
            solicitud.ObservacionEstado = observacion;
            solicitud.FechaDevolucion = DateTime.Now;
            solicitud.DevueltoPor = User.Identity?.Name;

            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            try
            {
                var solicitante = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.NombreColaborador == solicitud.Solicitante);

                if (solicitante != null)
                {
                    var destino = solicitante.UsuarioCorporativo ?? solicitante.CorreoCorporativo ?? "";
                    if (!string.IsNullOrEmpty(destino))
                    {
                        await _notif.CrearNotificacion(destino,
                            $"Tu solicitud de terceros #{solicitud.Id} fue DEVUELTA. Motivo: {observacion}", solicitud.Id);
                    }
                }
            }
            catch { }

            TempData["Exito"] = "Solicitud devuelta al solicitante.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SST,Administrador,RRHH,Gerente Capital Humano,Recepcionista")]
        public async Task<IActionResult> Rechazar(int id, string observacion)
        {
            if (string.IsNullOrEmpty(observacion))
            {
                TempData["Error"] = "Debe proporcionar un motivo para rechazar la solicitud.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var solicitud = await _context.TbSolicitudTerceros.FindAsync(id);
            if (solicitud == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(solicitud.Estado) && solicitud.Estado != "Pendiente" && solicitud.Estado != "En proceso")
            {
                TempData["Error"] = "Esta solicitud ya ha sido procesada.";
                return RedirectToAction(nameof(Index));
            }

            solicitud.Estado = "Rechazada";
            solicitud.ObservacionEstado = observacion;
            solicitud.FechaRechazo = DateTime.Now;
            solicitud.RechazadoPor = User.Identity?.Name;

            _context.Update(solicitud);
            await _context.SaveChangesAsync();

            try
            {
                var solicitante = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.NombreColaborador == solicitud.Solicitante);

                if (solicitante != null)
                {
                    var destino = solicitante.UsuarioCorporativo ?? solicitante.CorreoCorporativo ?? "";
                    if (!string.IsNullOrEmpty(destino))
                    {
                        await _notif.CrearNotificacion(destino,
                            $"Tu solicitud de terceros #{solicitud.Id} fue RECHAZADA. Motivo: {observacion}", solicitud.Id);
                    }
                }
            }
            catch { }

            TempData["Exito"] = "Solicitud rechazada.";
            return RedirectToAction(nameof(Index));
        }
    }
}