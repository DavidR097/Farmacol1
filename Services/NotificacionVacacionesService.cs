using Farmacol.Models;
using Farmacol.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Farmacol.BackgroundServices
{
    public class NotificacionVacacionesService : BackgroundService
    {
        private readonly ILogger<NotificacionVacacionesService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer? _timer;

        public NotificacionVacacionesService(
            ILogger<NotificacionVacacionesService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task EjecutarCorteManualAsync()
        {
            await EjecutarCorte(CancellationToken.None);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var nextRun = GetNextRunTime();
            var delay = nextRun - DateTime.Now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            _timer = new Timer(async _ => await EjecutarCorte(stoppingToken), null, delay, TimeSpan.FromDays(30));
            return Task.CompletedTask;
        }

        private DateTime GetNextRunTime()
        {
            var now = DateTime.Now;
            var firstDayNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            return firstDayNextMonth; 
        }

        private async Task EjecutarCorte(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ejecutando corte mensual de notificación de vacaciones.");

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Farmacol1Context>();
            var vacacionesService = scope.ServiceProvider.GetRequiredService<VacacionesService>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            try
            {
                var empleadosActivos = await context.Tbpersonals
                    .Where(p => !context.TbpersonalRetirados.Any(r => r.CC == p.CC) &&
                                !string.IsNullOrEmpty(p.CorreoPersonal))
                    .ToListAsync(stoppingToken);

                int enviados = 0;
                foreach (var emp in empleadosActivos)
                {
                    var vac = await vacacionesService.CalcularVacacionesAsync(emp.CC);
                    decimal diasDisponibles = decimal.Round(vac.DiasDisponibles, 2);
                    if (diasDisponibles <= 0) continue;

                    var fechaCorte = DateTime.Now.AddDays(-1).Date;
                    var cultura = new System.Globalization.CultureInfo("es-CO");
                    string fechaLarga = fechaCorte.ToString("dd 'de' MMMM 'de' yyyy", cultura);

                    var asunto = $"Notificación de días de vacaciones - Corte al {fechaLarga}";
                    var mensaje = $@"
Estimado(a) {emp.NombreColaborador},<br><br>

¡Reciba un cordial saludo! Le informamos que, con corte al {fechaLarga}, su saldo de vacaciones es de <strong>{diasDisponibles} días</strong>.

En caso de tener alguna inquietud o requerir una verificación adicional respecto a esta información, por favor no dude en contactar al equipo de <strong>Capital Humano de Farmacol Chinoin</strong>

Estaremos atentos para brindarle el apoyo necesario.<br><br>

Agradecemos su atención.<br><br>

Cordialmente,
Sistema Farmacol Chinoin
";

                    try
                    {
                        await emailService.EnviarAsync(emp.CorreoPersonal, asunto, mensaje);
                        enviados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando correo a {Email}", emp.CorreoPersonal);
                    }
                }

                _logger.LogInformation("Corte mensual completado. Se enviaron {Count} correos.", enviados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el corte mensual de vacaciones.");
            }
            finally
            {
                var next = GetNextRunTime();
                var delay = next - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
                _timer?.Change(delay, TimeSpan.FromDays(30));
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}