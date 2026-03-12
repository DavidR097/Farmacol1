using Farmacol.Models;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Services
{
    public class NotificacionService
    {
        private readonly Farmacol1Context _context;
        private readonly IConfiguration _config;

        public NotificacionService(Farmacol1Context context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task CrearNotificacion(string usuarioDestino, string mensaje, int? idSolicitud = null)
        {
            _context.Tbnotificaciones.Add(new Tbnotificacione
            {
                UsuarioDestino = usuarioDestino,
                Mensaje = mensaje,
                Leida = false,
                FechaCreacion = DateTime.Now,
                IdSolicitud = idSolicitud
            });
            await _context.SaveChangesAsync();
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