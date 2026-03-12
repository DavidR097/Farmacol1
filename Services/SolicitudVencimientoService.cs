using Farmacol.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Farmacol.Services;

public class SolicitudVencimientoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SolicitudVencimientoService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcesarVencidas();
            // Revisar cada hora
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcesarVencidas()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Farmacol1Context>();
        var limite = DateTime.Now.AddDays(-3);

        var vencidas = await context.Tbsolicitudes
            .Where(s => s.Estado == "Devuelta" &&
                        s.FechaDevolucion != null &&
                        s.FechaDevolucion < limite)
            .ToListAsync();

        foreach (var s in vencidas)
        {
            s.Estado = "Finalizada";
            s.EtapaAprobacion = "Finalizada por vencimiento (3 días)";
        }

        if (vencidas.Any())
            await context.SaveChangesAsync();
    }
}