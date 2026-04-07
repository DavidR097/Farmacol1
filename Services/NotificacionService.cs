using Farmacol.Models;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Services
{
    public class NotificacionService
    {
        private readonly Farmacol1Context _context;
        private readonly IConfiguration _config;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(Farmacol1Context context, IConfiguration config, ILogger<NotificacionService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task CrearNotificacion(string usuarioDestino, string mensaje, int? idSolicitud = null)
        {
            try
            {
                // Crear la notificación para el destino proporcionado
                _context.Tbnotificaciones.Add(new Tbnotificacione
                {
                    UsuarioDestino = usuarioDestino,
                    Mensaje = mensaje,
                    Leida = false,
                    FechaCreacion = DateTime.Now,
                    IdSolicitud = idSolicitud
                });

                // Si el destino parece ser un correo o usuario, intentar encontrar el otro identificador
                // y crear notificación para ambos (usuario corporativo y correo) para asegurar entrega.
                try
                {
                    var p = await _context.Tbpersonals.FirstOrDefaultAsync(x => x.UsuarioCorporativo == usuarioDestino || x.CorreoCorporativo == usuarioDestino);
                    if (p != null)
                    {
                        var user = p.UsuarioCorporativo ?? string.Empty;
                        var mail = p.CorreoCorporativo ?? string.Empty;
                        if (!string.IsNullOrEmpty(user) && !string.Equals(user, usuarioDestino, StringComparison.OrdinalIgnoreCase))
                        {
                            _context.Tbnotificaciones.Add(new Tbnotificacione
                            {
                                UsuarioDestino = user,
                                Mensaje = mensaje,
                                Leida = false,
                                FechaCreacion = DateTime.Now,
                                IdSolicitud = idSolicitud
                            });
                        }
                        if (!string.IsNullOrEmpty(mail) && !string.Equals(mail, usuarioDestino, StringComparison.OrdinalIgnoreCase))
                        {
                            _context.Tbnotificaciones.Add(new Tbnotificacione
                            {
                                UsuarioDestino = mail,
                                Mensaje = mensaje,
                                Leida = false,
                                FechaCreacion = DateTime.Now,
                                IdSolicitud = idSolicitud
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error al intentar duplicar notificación para usuario/correo: {dest}", usuarioDestino);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creando notificación para {dest}", usuarioDestino);
            }
        }

        public async Task EnviarEmail(string destinatario, string asunto, string cuerpo)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var remitente = _config["Email:Remitente"] ?? "";
                var password = _config["Email:Password"] ?? "";

                using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(remitente, password),
                    EnableSsl = true
                };

                var mensaje = new System.Net.Mail.MailMessage(remitente, destinatario, asunto, cuerpo)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mensaje);
            }
            catch
            {
                // Si falla el email, la notificación interna igual se crea
            }
        }

        public async Task<int> ContarNoLeidas(string usuarioDestino)
        {
            return await _context.Tbnotificaciones
                .CountAsync(n => n.UsuarioDestino == usuarioDestino && !n.Leida);
        }
    }
}