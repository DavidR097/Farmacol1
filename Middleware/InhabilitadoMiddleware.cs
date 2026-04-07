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

        // Rutas que siempre se permiten aunque esté inhabilitado
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
        var identityUser = await userManager.FindByNameAsync(userName);

        // 1. Revisar Lockout de Identity (esto es lo que más falta)
        if (identityUser?.LockoutEnd.HasValue == true && identityUser.LockoutEnd > DateTimeOffset.UtcNow)
        {
            var hasta = identityUser.LockoutEnd.Value.LocalDateTime.ToString("dd/MM/yyyy");
            context.Response.Redirect($"/Inhabilitado?hasta={hasta}&motivo=Inhabilitación%20por%20vacaciones");
            return;
        }

        Tbpersonal? personal = null;
        if (!string.IsNullOrWhiteSpace(userName))
        {
            personal = await db.Tbpersonals.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UsuarioCorporativo == userName);
        }
        if (personal == null && identityUser != null &&
            !string.IsNullOrWhiteSpace(identityUser.Email))
        {
            personal = await db.Tbpersonals.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CorreoCorporativo == identityUser.Email);
        }

        if (personal != null)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var delegacionActiva = await db.TbDelegaciones
                .AsNoTracking()
                .AnyAsync(d => d.Activa &&
                               d.CC == personal.CC &&
                               d.FechaInicio <= hoy &&
                               d.FechaFin >= hoy);

            if (delegacionActiva)
            {
                context.Response.Redirect("/Inhabilitado");
                return;
            }
        }

        await _next(context);
    }
}

public static class InhabilitadoMiddlewareExtensions
{
    public static IApplicationBuilder UseInhabilitadoCheck(this IApplicationBuilder app)
        => app.UseMiddleware<InhabilitadoMiddleware>();
}