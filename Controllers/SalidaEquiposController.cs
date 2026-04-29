using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Farmacol.Controllers
{
    [Authorize(Policy = "SolicitudesAccess")]
    public class SalidaEquiposController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly NotificacionService _notif;
        private readonly EmailService _email;
        private readonly AuditService _audit;
        private readonly FlujoAprobacionService _flujo;

        public SalidaEquiposController(
            Farmacol1Context context,
            NotificacionService notif,
            EmailService email,
            AuditService audit,
            FlujoAprobacionService flujo)
        {
            _context = context;
            _notif = notif;
            _email = email;
            _audit = audit;
            _flujo = flujo;
        }

        // GET: Crear
        public async Task<IActionResult> Create()
        {
            var personal = await BuscarPersonalActual();

            var model = new TbSalidaEquipos
            {
                Solicitante = personal?.NombreColaborador,
                Area = personal?.Area,
                FechaRegistro = DateOnly.FromDateTime(DateTime.Now),
                Estado = "Pendiente",
                EtapaAprobacion = "Pendiente Gerente de Área",
                DebeRegresar = "Sí"
            };

            return View(model);
        }

        // POST: Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TbSalidaEquipos model)
        {
            // FIX: Limpiar del ModelState los campos que se asignan manualmente
            // para que no bloqueen la validación con valores vacíos del form
            ModelState.Remove(nameof(model.Id));
            ModelState.Remove(nameof(model.FechaRegistro));
            ModelState.Remove(nameof(model.Solicitante));
            ModelState.Remove(nameof(model.Area));
            ModelState.Remove(nameof(model.Estado));
            ModelState.Remove(nameof(model.EtapaAprobacion));

            if (!ModelState.IsValid)
            {
                // Mostrar en TempData qué campos fallaron (útil para depurar)
                var errores = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");

                TempData["ErrorValidacion"] = "Errores de validación: " + string.Join(" | ", errores);
                return View(model);
            }

            var personal = await BuscarPersonalActual();
            if (personal == null)
            {
                TempData["Error"] = "No se encontró tu perfil de personal. Contacta al administrador.";
                return RedirectToAction(nameof(Index));
            }

            model.Id = await GenerarNuevoIdAsync();
            model.FechaRegistro = DateOnly.FromDateTime(DateTime.Now);
            model.Estado = "Pendiente";
            model.EtapaAprobacion = "Pendiente Gerente de Área";
            model.Solicitante = personal.NombreColaborador;
            model.SolicitanteUsuario = personal.UsuarioCorporativo;
            model.SolicitanteCorreo = personal.CorreoCorporativo;
            model.Area = personal.Area;

            _context.TbSalidaEquipos.Add(model);
            await _context.SaveChangesAsync();
            // Inicializar flujo de aprobación: determinar pasos y notificar primer aprobador
            try
            {
                var nivel = _flujo.DeterminarNivel(personal.Cargo);
                var pasos = await _flujo.ObtenerAprobadoresConFallback(personal, nivel);

                // Tratamiento especial: si el solicitante es TI o cargo contiene 'Recepcionista',
                // asignar directamente a Gerente Capital Humano
                var areaLower = (personal.Area ?? "").ToLower();
                var cargoLower = (personal.Cargo ?? "").ToLower();
                if (areaLower == "ti" || cargoLower.Contains("recepcionista") || nivel == "Técnico TI")
                {
                    pasos = new System.Collections.Generic.List<(string Cargo, string Area, bool EsFallback)>
                    {
                        ("Gerente Capital Humano", "Capital Humano", false)
                    };
                }

                    if (pasos != null && pasos.Count > 0)
                {
                    model.EtapaAprobacion = pasos[0].Cargo.StartsWith("Capital Humano") ? "Pendiente: Gerente Capital Humano" : $"Pendiente: {pasos[0].Cargo}";
                    _context.TbSalidaEquipos.Update(model);
                    await _context.SaveChangesAsync();

                    // Notificar primer aprobador
                    var primer = pasos[0];
                    var aprobadorUser = await _flujo.BuscarAprobador(primer.Cargo, primer.Area);
                        string destino = aprobadorUser?.Email ?? aprobadorUser?.UserName ?? "";
                        if (!string.IsNullOrEmpty(destino))
                    {
                        var mensaje = $"Nueva salida de equipos {model.Id} requiere tu aprobación. Solicitante: {model.Solicitante}, Área: {model.Area}.";
                        try { await _notif.CrearNotificacion(destino, mensaje, null); } catch { }
                        try
                        {
                            await _email.EnviarAsync(destino, $"[Farmacol] Nueva salida {model.Id} requiere aprobación", $"Solicitud {model.Id} de {model.Solicitante} requiere tu revisión.");
                        }
                        catch { }
                    }
                }
            }
            catch { }

            // Notificar al primer aprobador (Gerente del Área)
            await NotificarPrimerAprobadorAsync(model, personal);

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES, "Crear",
                    $"Salida de equipos #{model.Id} creada por {personal.NombreColaborador}", model.Id);
            }
            catch { }

            TempData["Exito"] = $"Salida de equipos registrada correctamente (ID: {model.Id})";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> GenerarNuevoIdAsync()
        {
            var ultimo = await _context.TbSalidaEquipos
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (ultimo == null || string.IsNullOrEmpty(ultimo.Id))
                return "RS-0001";

            var numeroStr = ultimo.Id.Replace("RS-", "").Trim();
            if (int.TryParse(numeroStr, out int numero))
                return $"RS-{(numero + 1).ToString("D4")}";

            return "RS-0001";
        }

        private async Task NotificarPrimerAprobadorAsync(TbSalidaEquipos salida, Tbpersonal solicitante)
        {
            if (string.IsNullOrEmpty(solicitante.Area))
                return;

            var gerenteArea = await _context.Tbpersonals
                .FirstOrDefaultAsync(p =>
                    p.Area != null &&
                    p.Area == solicitante.Area &&
                    p.Cargo != null &&
                    (p.Cargo.ToLower().Contains("gerente") ||
                     p.Cargo.ToLower().Contains("jefe")));

            if (gerenteArea != null)
            {
                var destino = gerenteArea.UsuarioCorporativo ?? gerenteArea.CorreoCorporativo ?? "";
                var mensaje = $"Nueva salida de equipos **{salida.Id}** requiere tu aprobación como Gerente/Jefe de {salida.Area}.";
                await _notif.CrearNotificacion(destino, mensaje, null);
            }

            var gerenteCH = await _context.Tbpersonals
                .FirstOrDefaultAsync(p =>
                    p.Cargo != null &&
                    (p.Cargo.Contains("Gerente Capital Humano") ||
                     p.Cargo.Contains("Gerente CH")));

            if (gerenteCH != null)
            {
                var destinoCH = gerenteCH.UsuarioCorporativo ?? gerenteCH.CorreoCorporativo ?? "";
                var mensajeCH = $"Hay una nueva salida de equipos ({salida.Id}) pendiente de revisión del Gerente de Área.";
                await _notif.CrearNotificacion(destinoCH, mensajeCH, null);
            }
        }

        public async Task<IActionResult> Index()
        {
            var salidas = await _context.TbSalidaEquipos
                .OrderByDescending(s => s.FechaRegistro)
                .ToListAsync();

            return View(salidas);
        }

        // POST: Procesar aprobación/rechazo de una salida
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Solo Gerentes de área y Gerente Capital Humano pueden aprobar/rechazar
        [Authorize(Roles = "Gerente,Gerente Capital Humano")]
        public async Task<IActionResult> ProcesarAprobacion(string id, string decision, string observacionesEstado)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var salida = await _context.TbSalidaEquipos.FindAsync(id);
            if (salida == null) return NotFound();
            // Obtener información del usuario (personal)
            var personalUsuario = await BuscarPersonalActual();
            var cargoUsuario = personalUsuario?.Cargo?.Trim() ?? "";
            var areaUsuario = personalUsuario?.Area?.Trim() ?? "";

            // Determinar etapa actual: esperamos dos etapas: Gerente de área -> Gerente Capital Humano
            var etapa = salida.EtapaAprobacion ?? "";

            // Validar permiso según etapa
            if (etapa.Contains("Gerente") && !etapa.Contains("Capital Humano"))
            {
                // Etapa: Gerente de área -> permitir solo gerentes cuyo cargo inicia con "Gerente" y área coincide
                if (!cargoUsuario.StartsWith("Gerente", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(areaUsuario, salida.Area ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                // Registrar aprobación/rechazo por Gerente de área
                salida.AprobacionGerencia = decision.Equals("Aprobar", StringComparison.OrdinalIgnoreCase)
                    ? personalUsuario?.NombreColaborador : personalUsuario?.NombreColaborador + " (rechazó)";

                if (string.Equals(decision, "Aprobar", StringComparison.OrdinalIgnoreCase))
                {
                    salida.EtapaAprobacion = "Pendiente: Gerente Capital Humano";
                    salida.Estado = "En proceso";
                    salida.FechaAprobacionGerencia = DateTime.Now;
                }
                else
                {
                    salida.Estado = "Rechazado";
                    salida.EtapaAprobacion = "Rechazada";
                    salida.FechaAprobacionGerencia = DateTime.Now;
                }
            }
            else
            {
                // Etapa: Gerente Capital Humano o final
                // Permitir solo si cargo exacto es Gerente Capital Humano
                if (!string.Equals(cargoUsuario, "Gerente Capital Humano", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                salida.AprobacionCH = decision.Equals("Aprobar", StringComparison.OrdinalIgnoreCase)
                    ? personalUsuario?.NombreColaborador : personalUsuario?.NombreColaborador + " (rechazó)";

                if (string.Equals(decision, "Aprobar", StringComparison.OrdinalIgnoreCase))
                {
                    salida.Estado = "Aprobada";
                    salida.EtapaAprobacion = "Aprobada";
                    salida.FechaAprobacionCH = DateTime.Now;
                }
                else
                {
                    salida.Estado = "Rechazada";
                    salida.EtapaAprobacion = "Rechazada";
                    salida.FechaAprobacionCH = DateTime.Now;
                }
            }

            salida.ObservacionEstado = observacionesEstado;

            _context.TbSalidaEquipos.Update(salida);
            await _context.SaveChangesAsync();

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES,
                    string.Equals(decision, "Aprobar", StringComparison.OrdinalIgnoreCase) ? "Aprobar" : "Rechazar",
                    $"Salida de equipos #{salida.Id} {decision.ToLower()} por {User.Identity?.Name}", salida.Id);
            }
            catch { }
            // Si el aprobador fue Gerente de área y la decisión fue aprobar, notificar al siguiente aprobador
            try
            {
                if (etapa.Contains("Gerente") && !etapa.Contains("Capital Humano") && string.Equals(decision, "Aprobar", StringComparison.OrdinalIgnoreCase))
                {
                    // Encontrar al solicitante en TBPersonal (por usuario o correo guardados)
                    var solicit = await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                        (!string.IsNullOrEmpty(salida.SolicitanteUsuario) && p.UsuarioCorporativo == salida.SolicitanteUsuario) ||
                        (!string.IsNullOrEmpty(salida.SolicitanteCorreo) && p.CorreoCorporativo == salida.SolicitanteCorreo));

                    if (solicit != null)
                    {
                        var nivelSolic = _flujo.DeterminarNivel(solicit.Cargo);
                        var pasos = await _flujo.ObtenerAprobadoresConFallback(solicit, nivelSolic);
                        if (pasos != null && pasos.Count > 1)
                        {
                            var siguiente = pasos[1];
                            var aprobadorUser = await _flujo.BuscarAprobador(siguiente.Cargo, siguiente.Area);
                            var destino = aprobadorUser?.Email ?? aprobadorUser?.UserName ?? "";
                            if (!string.IsNullOrEmpty(destino))
                            {
                                var mensaje = $"La salida {salida.Id} fue aprobada por el gerente de área. Requiere tu aprobación (paso siguiente).";
                                try { await _notif.CrearNotificacion(destino, mensaje, null); } catch { }
                                try { await _email.EnviarAsync(destino, $"[Farmacol] Solicitud {salida.Id} requiere tu aprobación", mensaje); } catch { }
                            }

                            // Notificar al solicitante que su solicitud avanzó al siguiente paso
                            try
                            {
                                var destSolic = salida.SolicitanteUsuario ?? salida.SolicitanteCorreo ?? "";
                                if (!string.IsNullOrEmpty(destSolic))
                                {
                                    var msgSolic = $"Tu salida {salida.Id} fue aprobada por el gerente de área y ahora está en la siguiente etapa de aprobación.";
                                    try { await _notif.CrearNotificacion(destSolic, msgSolic, null); } catch { }
                                    try { await _email.EnviarAsync(salida.SolicitanteCorreo ?? destSolic, $"[Farmacol] Tu salida {salida.Id} avanzó", msgSolic); } catch { }
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            // Si no hay siguiente (fallback), notificar al Gerente General
                            var gerenteGen = await _context.Tbpersonals.FirstOrDefaultAsync(p => p.Cargo != null && p.Cargo.ToLower() == "gerente general" && p.CorreoCorporativo != null);
                            var destino = gerenteGen?.CorreoCorporativo ?? gerenteGen?.UsuarioCorporativo ?? "";
                            if (!string.IsNullOrEmpty(destino))
                            {
                                var mensaje = $"La salida {salida.Id} fue aprobada por el gerente de área y requiere la revisión del Gerente General.";
                                try { await _notif.CrearNotificacion(destino, mensaje, null); } catch { }
                                try { await _email.EnviarAsync(destino, $"[Farmacol] Solicitud {salida.Id} requiere aprobación Gerente General", mensaje); } catch { }
                            }
                        }
                    }
                }
            }
            catch { }

            // Notificar al solicitante por notificación interna y correo
            try
            {
                var dest = salida.SolicitanteUsuario ?? salida.SolicitanteCorreo ?? "";
                if (!string.IsNullOrEmpty(dest))
                {
                    var msg = string.Equals(decision, "Aprobar", StringComparison.OrdinalIgnoreCase)
                        ? $"Tu salida de equipos {salida.Id} ha sido aprobada." : $"Tu salida de equipos {salida.Id} ha sido rechazada.";
                    try { await _notif.CrearNotificacion(dest, msg, null); } catch { }
                    try { await _email.EnviarAsync(salida.SolicitanteCorreo ?? dest, "Estado de tu salida de equipos", msg); } catch { }
                }
            }
            catch { }

            TempData["Exito"] = string.Equals(decision, "Aprobar", StringComparison.OrdinalIgnoreCase) ? "Salida aprobada." : "Salida rechazada.";
            return RedirectToAction("Index", "SalidaEquipos");
        }

        [Authorize(Roles = "Gerente,Jefe,RRHH,Administrador,Gerente Capital Humano")]
        public async Task<IActionResult> Revisar(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var salida = await _context.TbSalidaEquipos.FindAsync(id);
            if (salida == null) return NotFound();
            return View(salida);
        }

        private async Task<Tbpersonal?> BuscarPersonalActual()
        {
            var userName = User.Identity?.Name ?? "";
            return await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == userName ||
                p.CorreoCorporativo == userName);
        }
    }
}