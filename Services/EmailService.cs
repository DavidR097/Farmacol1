using Farmacol.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Farmacol.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly Farmacol1Context _context;
    private readonly NotificacionService _notificacion;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, Farmacol1Context context, NotificacionService notificacion, ILogger<EmailService> logger)
    {
        _config = config;
        _context = context;
        _notificacion = notificacion;
        _logger = logger;
    }

    // ── Envío base ────────────────────────────────────────────────────────
    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        if (string.IsNullOrWhiteSpace(destinatario)) return;

        var cfg = _config.GetSection("Email");

        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(
            cfg["Nombre"] ?? "Farmacol RRHH",
            cfg["Remitente"] ?? ""));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("html") { Text = cuerpoHtml };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            cfg["Host"] ?? "smtp.gmail.com",
            int.TryParse(cfg["Port"], out int p) ? p : 587,
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(cfg["Usuario"], cfg["Password"]);
        await smtp.SendAsync(mensaje);
        await smtp.DisconnectAsync(true);
    }

    // ── Buscar correo del solicitante por CC ──────────────────────────────
    private async Task<string?> ObtenerCorreoPorCC(int? cc)
    {
        if (cc == null) return null;
        var p = await _context.Tbpersonals.FindAsync(cc.Value);
        return p?.CorreoCorporativo;
    }

    // ── Buscar correo del aprobador por cargo ─────────────────────────────
    private async Task<string?> ObtenerCorreoAprobador(string? cargo)
    {
        if (string.IsNullOrEmpty(cargo)) return null;

        // "Capital Humano" genérico → buscar Gerente CH primero, luego Coordinador, luego Asistente
        if (string.Equals(cargo, "Capital Humano", StringComparison.OrdinalIgnoreCase))
        {
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(x =>
                x.Cargo != null && (
                    x.Cargo.ToLower() == "gerente capital humano" ||
                    x.Cargo.ToLower() == "coordinador capital humano" ||
                    x.Cargo.ToLower() == "asistente capital humano") &&
                x.CorreoCorporativo != null);
            return p?.CorreoCorporativo;
        }

        // Cargos exactos de Capital Humano
        if (cargo.ToLower().Contains("capital humano"))
        {
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(x =>
                x.Cargo != null &&
                x.Cargo.ToLower() == cargo.ToLower() &&
                x.CorreoCorporativo != null);
            return p?.CorreoCorporativo;
        }

        // Gerente General
        if (string.Equals(cargo, "Gerente General", StringComparison.OrdinalIgnoreCase))
        {
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(x =>
                x.Cargo != null &&
                x.Cargo.ToLower() == "gerente general" &&
                x.CorreoCorporativo != null);
            return p?.CorreoCorporativo;
        }

        // Directivo
        if (string.Equals(cargo, "Directivo", StringComparison.OrdinalIgnoreCase))
        {
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(x =>
                x.Cargo != null &&
                x.Cargo.ToLower().StartsWith("directivo") &&
                x.CorreoCorporativo != null);
            return p?.CorreoCorporativo;
        }

        // Cargos con nivel + área: "Jefe Ventas", "Gerente Logística"
        var partes = cargo.Trim().Split(' ', 2);
        if (partes.Length == 2)
        {
            var nivel = partes[0].ToLower();
            var area = partes[1].ToLower();
            var p = await _context.Tbpersonals.FirstOrDefaultAsync(x =>
                x.Cargo != null && x.Cargo.ToLower().StartsWith(nivel) &&
                x.Area != null && x.Area.ToLower() == area &&
                x.CorreoCorporativo != null);
            return p?.CorreoCorporativo;
        }

        return null;
    }

    // ── Template HTML ─────────────────────────────────────────────────────
    private static string Template(string titulo, string color,
                                   string icono, string cuerpo) => $@"
<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<style>
  body     {{ margin:0; padding:0; background:#f4f6f9; font-family:system-ui,sans-serif; }}
  .wrap    {{ max-width:600px; margin:32px auto; background:#fff; border-radius:12px;
              overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,.08); }}
  .header  {{ background:{color}; color:#fff; padding:28px 32px; }}
  .header h1 {{ margin:0; font-size:1.4rem; }}
  .header p  {{ margin:6px 0 0; opacity:.85; font-size:.9rem; }}
  .body    {{ padding:28px 32px; color:#343a40; line-height:1.6; }}
  .badge   {{ display:inline-block; background:{color}22; color:{color};
              border:1px solid {color}44; border-radius:50px; padding:4px 14px;
              font-size:.82rem; font-weight:600; margin-bottom:16px; }}
  .info-box {{ background:#f8f9fa; border-radius:8px; padding:16px 20px;
               margin:16px 0; font-size:.9rem; }}
  .info-box b {{ color:#495057; }}
  .footer  {{ background:#f8f9fa; padding:16px 32px; font-size:.78rem;
              color:#6c757d; text-align:center; border-top:1px solid #dee2e6; }}
</style>
</head>
<body>
<div class='wrap'>
  <div class='header'><h1>{icono} {titulo}</h1>
    <p>Sistema de Recursos Humanos — Farmacol</p></div>
  <div class='body'>{cuerpo}</div>
  <div class='footer'>Mensaje automático generado por Farmacol RRHH. Por favor no responder.</div>
</div>
</body></html>";

    // ── EVENTO 1: Solicitud creada → primer aprobador ─────────────────────
    public async Task NotificarSolicitudCreadaAsync(Tbsolicitude sol)
    {
        var correo = await ObtenerCorreoAprobador(sol.Paso1Aprobador);
        if (string.IsNullOrEmpty(correo)) return;

        var cuerpo = $@"
<div class='badge'>📋 Nueva solicitud pendiente</div>
<p>Tienes una nueva solicitud esperando tu revisión:</p>
<div class='info-box'>
  <b>Solicitante:</b> {sol.Nombre}<br>
  <b>Tipo:</b> {sol.TipoSolicitud}<br>
  <b>Motivo:</b> {sol.Motivo}<br>
  <b>Fecha solicitud:</b> {sol.FechaSolicitud?.ToString("dd/MM/yyyy")}<br>
  {(sol.FechaInicio.HasValue ? $"<b>Desde:</b> {sol.FechaInicio.Value:dd/MM/yyyy}<br>" : "")}
  {(sol.FechaFin.HasValue ? $"<b>Hasta:</b> {sol.FechaFin.Value:dd/MM/yyyy}<br>" : "")}
</div>
<p>Ingresa al sistema para revisar y aprobar o rechazar.</p>";

        try
        {
            await EnviarAsync(correo,
                $"[Farmacol] Nueva solicitud de {sol.Nombre}",
                Template("Nueva Solicitud", "#0d6efd", "📋", cuerpo));
            // Crear notificación interna también (usar correo como destino si no hay usuario corporativo)
            try { await _notificacion.CrearNotificacion(correo, $"Nueva solicitud de {sol.Nombre}", sol.IdSolicitud); } catch { }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo enviando email de nueva solicitud a {correo}", correo);
        }
    }

    // ── EVENTO 2: Aprobada definitivamente → solicitante ──────────────────
    public async Task NotificarSolicitudAprobadaAsync(Tbsolicitude sol)
    {
        var correo = await ObtenerCorreoPorCC(sol.CC);
        if (string.IsNullOrEmpty(correo)) return;

        var cuerpo = $@"
<div class='badge'>✅ Solicitud aprobada</div>
<p>¡Tu solicitud ha sido <b>aprobada</b>!</p>
<div class='info-box'>
  <b>Tipo:</b> {sol.TipoSolicitud}<br>
  <b>Motivo:</b> {sol.Motivo}<br>
  {(sol.FechaInicio.HasValue ? $"<b>Desde:</b> {sol.FechaInicio.Value:dd/MM/yyyy}<br>" : "")}
  {(sol.FechaFin.HasValue ? $"<b>Hasta:</b> {sol.FechaFin.Value:dd/MM/yyyy}<br>" : "")}
</div>
<p>Puedes consultar el detalle en el sistema.</p>";

        try
        {
            await EnviarAsync(correo,
                "[Farmacol] Tu solicitud fue aprobada ✅",
                Template("Solicitud Aprobada", "#198754", "✅", cuerpo));
            try { await _notificacion.CrearNotificacion(correo, "Tu solicitud fue aprobada", sol.IdSolicitud); } catch { }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo enviando email de solicitud aprobada a {correo}", correo);
        }
    }

    // ── EVENTO 3: Rechazada → solicitante ────────────────────────────────
    public async Task NotificarSolicitudRechazadaAsync(Tbsolicitude sol, string? obs)
    {
        var correo = await ObtenerCorreoPorCC(sol.CC);
        if (string.IsNullOrEmpty(correo)) return;

        var cuerpo = $@"
<div class='badge'>❌ Solicitud rechazada</div>
<p>Tu solicitud fue <b>rechazada</b>.</p>
<div class='info-box'>
  <b>Tipo:</b> {sol.TipoSolicitud}<br>
  <b>Motivo de rechazo:</b> {obs ?? "Sin observación"}<br>
</div>
<p>Si tienes dudas, comunícate con Capital Humano.</p>";

        try
        {
            await EnviarAsync(correo,
                "[Farmacol] Tu solicitud fue rechazada ❌",
                Template("Solicitud Rechazada", "#dc3545", "❌", cuerpo));
            try { await _notificacion.CrearNotificacion(correo, "Tu solicitud fue rechazada", sol.IdSolicitud); } catch { }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo enviando email de solicitud rechazada a {correo}", correo);
        }
    }

    // ── EVENTO 4: Devuelta → solicitante ─────────────────────────────────
    public async Task NotificarSolicitudDevueltaAsync(Tbsolicitude sol, string? obs)
    {
        var correo = await ObtenerCorreoPorCC(sol.CC);
        if (string.IsNullOrEmpty(correo)) return;

        var cuerpo = $@"
<div class='badge'>↩️ Solicitud devuelta</div>
<p>Tu solicitud fue <b>devuelta</b> para corrección.</p>
<div class='info-box'>
  <b>Tipo:</b> {sol.TipoSolicitud}<br>
  <b>Observación:</b> {obs ?? "Sin observación"}<br>
</div>
<p>Ingresa al sistema, corrígela y vuelve a enviarla. Tienes 3 días.</p>";

        try
        {
            await EnviarAsync(correo,
                "[Farmacol] Tu solicitud fue devuelta ↩️",
                Template("Solicitud Devuelta", "#fd7e14", "↩️", cuerpo));
            try { await _notificacion.CrearNotificacion(correo, "Tu solicitud fue devuelta", sol.IdSolicitud); } catch { }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo enviando email de solicitud devuelta a {correo}", correo);
        }
    }

    // ── EVENTO 5: Documento subido → empleado ────────────────────────────
    public async Task NotificarDocumentoSubidoAsync(int cc, string nombreDoc, string modulo)
    {
        var correo = await ObtenerCorreoPorCC(cc);
        if (string.IsNullOrEmpty(correo)) return;

        var cuerpo = $@"
<div class='badge'>📄 Nuevo documento en tu expediente</div>
<p>Se subió un nuevo documento a tu expediente:</p>
<div class='info-box'>
  <b>Documento:</b> {nombreDoc}<br>
  <b>Módulo:</b> {modulo}<br>
</div>
<p>Puedes verlo en la sección <b>Mi Expediente</b> del sistema.</p>";

        await EnviarAsync(correo,
            "[Farmacol] Nuevo documento en tu expediente",
            Template("Nuevo Documento", "#6f42c1", "📄", cuerpo));
    }

    // ── EVENTO 6: Avance de paso → siguiente aprobador ───────────────────
    public async Task NotificarSiguienteAprobadorAsync(Tbsolicitude sol)
    {
        var paso = sol.PasoActual ?? 1;
        var cargo = paso == 2 ? sol.Paso2Aprobador
                  : paso == 3 ? sol.Paso3Aprobador : null;
        if (string.IsNullOrEmpty(cargo)) return;

        var correo = await ObtenerCorreoAprobador(cargo);
        if (string.IsNullOrEmpty(correo)) return;

        var cuerpo = $@"
<div class='badge'>📋 Requiere tu aprobación</div>
<p>Una solicitud ha llegado a tu paso de aprobación:</p>
<div class='info-box'>
  <b>Solicitante:</b> {sol.Nombre}<br>
  <b>Tipo:</b> {sol.TipoSolicitud}<br>
  <b>Motivo:</b> {sol.Motivo}<br>
  {(sol.FechaInicio.HasValue ? $"<b>Desde:</b> {sol.FechaInicio.Value:dd/MM/yyyy}<br>" : "")}
  {(sol.FechaFin.HasValue ? $"<b>Hasta:</b> {sol.FechaFin.Value:dd/MM/yyyy}<br>" : "")}
</div>
<p>Ingresa al sistema para aprobar o rechazar.</p>";

        try
        {
            await EnviarAsync(correo,
                $"[Farmacol] Solicitud de {sol.Nombre} requiere tu aprobación",
                Template("Aprobación Requerida", "#0d6efd", "📋", cuerpo));
            try { await _notificacion.CrearNotificacion(correo, $"Solicitud de {sol.Nombre} requiere tu aprobación", sol.IdSolicitud); } catch { }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fallo enviando email de aprobación requerida a {correo}", correo);
        }
    }
}
