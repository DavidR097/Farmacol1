using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Farmacol.Models;

namespace Farmacol.Middleware;

public class InhabilitadoMiddleware
{
    private readonly RequestDelegate _next;

    public InhabilitadoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context,
        UserManager<IdentityUser> userManager,
        Farmacol1Context db)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Rutas siempre permitidas aunque esté inhabilitado
        bool esRutaLibre = path.StartsWith("/login")
                        || path.StartsWith("/inhabilitado")
                        || path.StartsWith("/account")
                        || path.StartsWith("/css")
                        || path.StartsWith("/js")
                        || path.StartsWith("/lib")
                        || path.StartsWith("/favicon")
                        || path.StartsWith("/images")
                        || path == "/";

        if (esRutaLibre)
        {
            await _next(context);
            return;
        }

        var userName = context.User.Identity.Name ?? "";
        if (string.IsNullOrEmpty(userName))
        {
            await _next(context);
            return;
        }

        // Solo revisar delegaciones si el usuario es Jefe o Gerente
        bool esJefeOGerente = context.User.IsInRole("Jefe") || context.User.IsInRole("Gerente");

        if (!esJefeOGerente)
        {
            // Usuarios normales (TI, Asistente, Usuario, etc.) NO se inhabilitan por delegación
            await _next(context);
            return;
        }

        // Buscar el registro en Tbpersonal
        var personal = await db.Tbpersonals.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UsuarioCorporativo == userName
                                   || p.CorreoCorporativo == userName);

        if (personal == null)
        {
            await _next(context);
            return;
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // Revisar si tiene delegación activa
        var delegacionActiva = await db.TbDelegaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Activa
                                   && d.CC == personal.CC
                                   && d.FechaInicio <= hoy
                                   && d.FechaFin >= hoy);

        if (delegacionActiva != null)
        {
            var hasta = delegacionActiva.FechaFin.ToString("dd/MM/yyyy");
            var motivo = string.IsNullOrEmpty(delegacionActiva.Motivo)
                        ? "Inhabilitación temporal"
                        : delegacionActiva.Motivo;

            context.Response.Redirect($"/Inhabilitado?hasta={hasta}&motivo={Uri.EscapeDataString(motivo)}");
            return;
        }

        // Solo si es Jefe/Gerente y NO tiene delegación, revisamos Lockout de Identity (bloqueo manual)
        var identityUser = await userManager.FindByNameAsync(userName);
        if (identityUser?.LockoutEnd.HasValue == true && identityUser.LockoutEnd > DateTimeOffset.UtcNow)
        {
            context.Response.Redirect("/Inhabilitado?hasta=indefinido&motivo=Bloqueo%20manual%20por%20administrador");
            return;
        }

        await _next(context);
    }
}

public static class InhabilitadoMiddlewareExtensions
{
    public static IApplicationBuilder UseInhabilitadoCheck(this IApplicationBuilder app)
        => app.UseMiddleware<InhabilitadoMiddleware>();
}