using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Security.Claims;

namespace Farmacol.Controllers
{
    [Authorize]
    public class SalidaEquiposController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly NotificacionService _notif;
        private readonly EmailService _email;
        private readonly AuditService _audit;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> _userManager;

        public SalidaEquiposController(
            Farmacol1Context context,
            NotificacionService notif,
            EmailService email,
            AuditService audit,
            Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser> userManager)
        {
            _context = context;
            _notif = notif;
            _email = email;
            _audit = audit;
            _userManager = userManager;
        }

        // === Helpers ===
        private int ObtenerNumeroId(string idCompleto)
        {
            if (string.IsNullOrEmpty(idCompleto)) return 0;
            var numeroStr = idCompleto.Replace("RS-", "").Trim();
            return int.TryParse(numeroStr, out int numero) ? numero : 0;
        }

        private async Task<Tbpersonal?> BuscarPersonalActual()
        {
            var userName = User.Identity?.Name ?? "";
            return await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == userName || p.CorreoCorporativo == userName);
        }

        private bool EsEmailValido(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { new MailAddress(email); return true; }
            catch { return false; }
        }

        // === CREATE ===
        [Authorize(Policy = "SolicitudesAccess")]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "SolicitudesAccess")]
        public async Task<IActionResult> Create(TbSalidaEquipos model)
        {
            ModelState.Remove(nameof(model.Id));
            ModelState.Remove(nameof(model.FechaRegistro));
            ModelState.Remove(nameof(model.Solicitante));
            ModelState.Remove(nameof(model.Area));
            ModelState.Remove(nameof(model.Estado));
            ModelState.Remove(nameof(model.EtapaAprobacion));

            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
                TempData["ErrorValidacion"] = "Errores de validación: " + string.Join(" | ", errores);
                return View(model);
            }

            var personal = await BuscarPersonalActual();
            if (personal == null)
            {
                TempData["Error"] = "No se encontró tu perfil de personal.";
                return RedirectToAction(nameof(Index));
            }

            model.Id = await GenerarNuevoIdAsync();
            model.FechaRegistro = DateOnly.FromDateTime(DateTime.Now);
            model.Estado = "Pendiente";
            model.Solicitante = personal.NombreColaborador;
            model.SolicitanteUsuario = personal.UsuarioCorporativo;
            model.SolicitanteCorreo = personal.CorreoCorporativo;
            model.Area = personal.Area;

            string primerAprobador = await ObtenerPrimerAprobador(personal);
            model.EtapaAprobacion = $"Pendiente: {primerAprobador}";

            _context.TbSalidaEquipos.Add(model);
            await _context.SaveChangesAsync();

            await NotificarAprobador(model, primerAprobador, "primer");

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES, "Crear",
                    $"Salida de equipos #{model.Id} creada por {personal.NombreColaborador}", (model.Id));
            }
            catch { }

            TempData["Exito"] = $"Solicitud creada. Pendiente de {primerAprobador}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> ObtenerPrimerAprobador(Tbpersonal solicitante)
        {
            var cargoLower = (solicitante.Cargo ?? "").ToLower().Trim();
            var areaLower = (solicitante.Area ?? "").ToLower().Trim();

            bool esCasoEspecial = areaLower == "ti" ||
                                  cargoLower.Contains("recepcionista") ||
                                  cargoLower.Contains("técnico ti") ||
                                  cargoLower.Contains("coordinadora gestión ambiental") ||
                                  cargoLower.Contains("coordinadora de compras") ||
                                  cargoLower.Contains("jefe mercadotecnia") ||
                                  User.IsInRole("SST") ||
                                  User.IsInRole("RRHH");

            if (esCasoEspecial)
            {
                var gerenteGeneral = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.Cargo != null &&
                        (p.Cargo.ToLower().Trim().Contains("gerente general") ||
                         p.Cargo.ToLower().Trim().Contains("gerencia general")));
                return gerenteGeneral?.NombreColaborador ?? "Gerente General";
            }
            else
            {
                var gerenteArea = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => (p.Area ?? "").ToLower().Trim() == areaLower &&
                        p.Cargo != null && (p.Cargo.ToLower().Trim().StartsWith("gerente") ||
                                            p.Cargo.ToLower().Trim().StartsWith("jefe")));
                return gerenteArea?.NombreColaborador ?? $"Gerente de {solicitante.Area}";
            }
        }

        private async Task NotificarAprobador(TbSalidaEquipos salida, string nombreAprobador, string etapa)
        {
            var aprobador = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.NombreColaborador == nombreAprobador);
            if (aprobador == null) return;

            var destino = aprobador.UsuarioCorporativo ?? aprobador.CorreoCorporativo;
            if (string.IsNullOrEmpty(destino)) return;

            var mensaje = etapa == "primer"
                ? $"Nueva salida de equipos #{salida.Id} requiere tu aprobación (etapa 1). Solicitante: {salida.Solicitante}, Área: {salida.Area}."
                : $"La salida #{salida.Id} fue aprobada en primera instancia. Requiere tu aprobación final (Gerente Capital Humano).";

            await _notif.CrearNotificacion(destino, mensaje, null);
            if (EsEmailValido(destino))
                await _email.EnviarAsync(destino, $"[Farmacol] Solicitud de equipos #{salida.Id}", mensaje);
        }

        private async Task<string> GenerarNuevoIdAsync()
        {
            var ultimo = await _context.TbSalidaEquipos.OrderByDescending(s => s.Id).FirstOrDefaultAsync();
            if (ultimo == null || string.IsNullOrEmpty(ultimo.Id))
                return "RS-0001";
            var numeroStr = ultimo.Id.Replace("RS-", "").Trim();
            if (int.TryParse(numeroStr, out int numero))
                return $"RS-{(numero + 1).ToString("D4")}";
            return "RS-0001";
        }

        // === INDEX Y MIS SALIDAS ===
        [Authorize(Policy = "SolicitudesAccess")]
        public async Task<IActionResult> Index()
        {
            var personal = await BuscarPersonalActual();
            var identityUser = await _userManager.FindByNameAsync(User.Identity?.Name ?? "");
            var roles = identityUser != null ? await _userManager.GetRolesAsync(identityUser) : new List<string>();

            bool esAdmin = roles.Contains("Administrador", StringComparer.OrdinalIgnoreCase);
            bool esRRHH = roles.Contains("RRHH", StringComparer.OrdinalIgnoreCase);
            bool esGerenteCH = roles.Contains("Gerente Capital Humano", StringComparer.OrdinalIgnoreCase);
            bool esRecepcionista = roles.Contains("Recepcionista", StringComparer.OrdinalIgnoreCase);
            bool esGerenteGeneralRol = roles.Contains("Gerencia General", StringComparer.OrdinalIgnoreCase) ||
                                       roles.Contains("Gerente General", StringComparer.OrdinalIgnoreCase);

            bool esGerenteGeneralCargo = personal != null &&
                (personal.Cargo?.ToLower().Trim().Contains("gerente general") == true ||
                 personal.Cargo?.ToLower().Trim().Contains("gerencia general") == true);

            bool puedeVerTodo = esAdmin || esRRHH || esGerenteCH || esRecepcionista || esGerenteGeneralRol || esGerenteGeneralCargo;

            if (puedeVerTodo)
            {
                var todas = await _context.TbSalidaEquipos.OrderByDescending(s => s.FechaRegistro).ToListAsync();
                return View(todas);
            }

            if (personal != null && (User.IsInRole("Gerente") || User.IsInRole("Jefe")))
            {
                var area = personal.Area ?? "";
                var delArea = await _context.TbSalidaEquipos
                    .Where(s => s.Area == area)
                    .OrderByDescending(s => s.FechaRegistro)
                    .ToListAsync();
                return View(delArea);
            }

            return RedirectToAction(nameof(MisSalidasEquipos));
        }

        [Authorize]
        public async Task<IActionResult> MisSalidasEquipos()
        {
            var personal = await BuscarPersonalActual();
            if (personal == null)
                return View("Index", new List<TbSalidaEquipos>());

            var userName = User.Identity?.Name ?? "";
            var misSalidas = await _context.TbSalidaEquipos
                .Where(s => s.SolicitanteUsuario == userName ||
                            s.SolicitanteCorreo == userName ||
                            s.Solicitante == personal.NombreColaborador)
                .OrderByDescending(s => s.FechaRegistro)
                .ToListAsync();

            return View("Index", misSalidas);
        }

        // === PROCESAR APROBACIÓN ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarAprobacion(string id, string decision, string observacionesEstado)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var salida = await _context.TbSalidaEquipos.FindAsync(id);
            if (salida == null) return NotFound();

            var usuario = await BuscarPersonalActual();
            if (!await PuedeActuar(salida)) return Forbid();

            // Normalizar a minúsculas para comparaciones seguras
            var etapa = (salida.EtapaAprobacion ?? "").ToLower().Trim();
            bool esAprobacion = decision.Equals("Aprobar", StringComparison.OrdinalIgnoreCase);

            // Primer paso: cualquier "Pendiente: <nombre>" que NO sea Capital Humano
            if (etapa.Contains("pendiente") && !etapa.Contains("capital humano"))
            {
                salida.AprobacionGerencia = esAprobacion
                    ? usuario?.NombreColaborador
                    : usuario?.NombreColaborador + " (rechazó)";
                salida.FechaAprobacionGerencia = DateTime.Now;

                if (esAprobacion)
                {
                    salida.EtapaAprobacion = "Pendiente: Gerente Capital Humano";
                    salida.Estado = "En proceso";

                    var gerenteCH = await _context.Tbpersonals
                        .FirstOrDefaultAsync(p => p.Cargo != null &&
                            p.Cargo.ToLower().Trim() == "gerente capital humano");
                    if (gerenteCH != null)
                        await NotificarAprobador(salida, gerenteCH.NombreColaborador, "segundo");
                }
                else
                {
                    salida.Estado = "Rechazado";
                    salida.EtapaAprobacion = "Rechazada";
                }
            }
            // Segundo paso: Gerente Capital Humano
            else if (etapa.Contains("capital humano"))
            {
                salida.AprobacionCH = esAprobacion
                    ? usuario?.NombreColaborador
                    : usuario?.NombreColaborador + " (rechazó)";
                salida.FechaAprobacionCH = DateTime.Now;

                if (esAprobacion)
                {
                    salida.Estado = "Aprobada";
                    salida.EtapaAprobacion = "Finalizado";
                    await NotificarVigilancia(salida);
                }
                else
                {
                    salida.Estado = "Rechazada";
                    salida.EtapaAprobacion = "Rechazada";
                }
            }

            // resto igual...

            salida.ObservacionEstado = observacionesEstado;
            _context.Update(salida);
            await _context.SaveChangesAsync();

            // Auditoría
            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SOLICITUDES,
                    esAprobacion ? "Aprobar" : "Rechazar",
                    $"Salida #{salida.Id} {decision.ToLower()} por {User.Identity?.Name}", (salida.Id));
            }
            catch { }

            // Notificar al solicitante
            var destSolicitante = salida.SolicitanteUsuario ?? salida.SolicitanteCorreo;
            if (!string.IsNullOrEmpty(destSolicitante))
            {
                var msg = esAprobacion
                    ? (salida.Estado == "Aprobada" ? $"✅ Tu salida {salida.Id} fue APROBADA." : $"Tu salida {salida.Id} fue aprobada, ahora pendiente de Gerente Capital Humano.")
                    : $"❌ Tu salida {salida.Id} fue RECHAZADA.";
                await _notif.CrearNotificacion(destSolicitante, msg, ObtenerNumeroId(salida.Id));
                if (EsEmailValido(salida.SolicitanteCorreo))
                    await _email.EnviarAsync(salida.SolicitanteCorreo, "Estado de tu salida de equipos", msg);
            }

            TempData["Exito"] = esAprobacion ? "Aprobación registrada." : "Solicitud rechazada.";
            return RedirectToAction(nameof(Index));
        }

        private async Task NotificarVigilancia(TbSalidaEquipos salida)
        {
            var userVigilancia = await _userManager.FindByNameAsync("vigilancia");
            if (userVigilancia == null) return;

            var destino = userVigilancia.UserName ?? userVigilancia.Email;
            if (string.IsNullOrEmpty(destino)) return;

            string mensaje = $"🚨 Salida de equipos #{salida.Id} APROBADA. " +
                             $"Solicitante: {salida.Solicitante}, " +
                             $"Equipo: {salida.Elemento}, " +
                             $"Fecha salida: {salida.FechaSalida?.ToString("dd/MM/yyyy") ?? "N/A"}, " +
                             $"Motivo: {salida.MotivoSalida}";

            // Pasar null en idSolicitud — el JS extrae el RS-XXXX del mensaje
            await _notif.CrearNotificacion(destino, mensaje, null);
        }

        // === VISTAS ===
        [Authorize(Roles = "Vigilancia")]
        public async Task<IActionResult> Vigilancia()
        {
            var salidas = await _context.TbSalidaEquipos
                .Where(s => s.Estado == "Aprobada" || s.Estado == "Finalizada")
                .OrderByDescending(s => s.FechaRegistro)
                .ToListAsync();
            return View(salidas);
        }

        private async Task<bool> PuedeActuar(TbSalidaEquipos salida)
        {
            var userName = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(userName)) return false;

            if (salida.Estado is "Aprobada" or "Rechazada" or "Finalizada")
                return false;

            var identityUser = await _userManager.FindByNameAsync(userName);
            if (identityUser != null)
            {
                var roles = await _userManager.GetRolesAsync(identityUser);
                if (roles.Contains("Administrador") || roles.Contains("RRHH"))
                    return true;
            }

            var personal = await BuscarPersonalActual();
            if (personal == null) return false;

            var etapa = (salida.EtapaAprobacion ?? "").ToLower().Trim();
            var cargoUsuario = (personal.Cargo ?? "").ToLower().Trim();
            var areaUsuario = (personal.Area ?? "").ToLower().Trim();
            var areaSolicitud = (salida.Area ?? "").ToLower().Trim();

            // ── Segundo paso: Capital Humano — SIEMPRE evaluar primero ──────────────
            if (etapa.Contains("capital humano"))
            {
                return cargoUsuario.Contains("gerente capital humano");
            }

            // ── Primer paso ──────────────────────────────────────────────────────────
            if (etapa.Contains("pendiente"))
            {
                // Extraer el nombre del aprobador esperado desde "Pendiente: Nombre Apellido"
                var nombreEsperado = etapa.Replace("pendiente:", "").Trim();

                // Verificar si la etapa apunta a un Gerente/Gerencia General por nombre
                var esParaGerenteGeneral = await _context.Tbpersonals
                    .AnyAsync(p => p.NombreColaborador != null &&
                                   p.NombreColaborador.ToLower().Trim() == nombreEsperado &&
                                   p.Cargo != null &&
                                   (p.Cargo.ToLower().Contains("gerente general") ||
                                    p.Cargo.ToLower().Contains("gerencia general")));

                if (esParaGerenteGeneral)
                {
                    return cargoUsuario.Contains("gerente general") ||
                           cargoUsuario.Contains("gerencia general");
                }

                // Caso normal: Gerente o Jefe del mismo área
                bool cargoValido = cargoUsuario.StartsWith("gerente") ||
                                   cargoUsuario.StartsWith("jefe");

                bool mismaArea = string.Equals(
                    areaUsuario.Trim(),
                    areaSolicitud.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                bool esGerenteOJefe = User.IsInRole("Gerente") || User.IsInRole("Jefe");

                return (cargoValido || esGerenteOJefe) && mismaArea;
            }

            return false;
        }

        [HttpGet]
        public async Task<IActionResult> Revisar([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var salida = await _context.TbSalidaEquipos.FindAsync(id);
            if (salida == null) return NotFound();

            bool puede = await PuedeActuar(salida);

            // Debug temporal — lo quitamos después
            TempData["DebugCanProcess"] = $"CanProcess={puede} | Etapa='{salida.EtapaAprobacion}' | Estado='{salida.Estado}' | Cargo='{(await BuscarPersonalActual())?.Cargo}'";

            ViewBag.CanProcess = puede;
            return View(salida);
        }
    }
}
