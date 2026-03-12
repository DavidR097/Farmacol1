using Farmacol.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Services;

public class FlujoAprobacionService
{
    private readonly Farmacol1Context _context;
    private readonly UserManager<IdentityUser> _userManager;

    public FlujoAprobacionService(Farmacol1Context context,
                                   UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public string DeterminarNivel(string cargo)
    {
        if (string.IsNullOrEmpty(cargo)) return "Usuario";
        var c = cargo.Trim().ToLower();

        if (c == "directivo" || c.StartsWith("directivo ")) return "Directivo";
        if (c == "gerente general") return "Gerente General";
        if (c.Contains("gerente capital humano")) return "RRHH";
        if (c.StartsWith("gerente")) return "Gerente";
        if (c.StartsWith("jefe")) return "Jefe";
        if (c.StartsWith("coordinador")) return "Coordinador";
        if (c.StartsWith("asistente")) return "Asistente";
        if (c == "administrador") return "Administrador";
        return "Usuario";
    }

    private async Task<bool> ExisteAprobador(string cargo, string area)
    {
        var partes = cargo.Trim().Split(' ', 2);
        var nivelEsperado = partes[0];
        var areaEsperada = partes.Length > 1 ? partes[1] : area;

        return await _context.Tbpersonals.AnyAsync(p =>
            p.Cargo != null &&
            p.Cargo.StartsWith(nivelEsperado) &&
            (string.IsNullOrEmpty(areaEsperada) ||
             (p.Area != null && p.Area.ToLower() == areaEsperada.ToLower())));
    }

    public async Task<List<(string Cargo, string Area, bool EsFallback)>>
        ObtenerAprobadoresConFallback(Tbpersonal solicitante, string nivelSolicitante)
    {
        var pasos = new List<(string Cargo, string Area, bool EsFallback)>();

        async Task<(string Cargo, string Area, bool EsFallback)> Paso(string cargo, string area)
        {
            if (cargo == "Capital Humano" || cargo == "Gerente General" || cargo == "Directivo")
                return (cargo, area, false);
            bool existe = await ExisteAprobador(cargo, area);
            return existe
                ? (cargo, area, false)
                : ("Gerente General", "Gerencia General", true);
        }

        switch (nivelSolicitante)
        {
            case "Gerente":
                pasos.Add(await Paso("Gerente General", "Gerencia General"));
                pasos.Add(await Paso("Capital Humano", "Capital Humano"));
                break;
            case "RRHH":
                pasos.Add(await Paso("Gerente General", "Gerencia General"));
                break;
            case "Gerente General":
                pasos.Add(await Paso("Capital Humano", "Capital Humano"));
                pasos.Add(await Paso("Directivo", "Directivo"));
                break;
            case "Jefe":
                pasos.Add(await Paso("Capital Humano", "Capital Humano"));
                pasos.Add(await Paso("Gerente " + solicitante.Area, solicitante.Area ?? ""));
                break;
            case "Coordinador":
            case "Asistente":
            case "Usuario":
                pasos.Add(await Paso("Capital Humano", "Capital Humano"));
                pasos.Add(await Paso("Jefe " + solicitante.Area, solicitante.Area ?? ""));
                pasos.Add(await Paso("Gerente " + solicitante.Area, solicitante.Area ?? ""));
                break;
        }

        // Eliminar duplicados consecutivos
        var resultado = new List<(string Cargo, string Area, bool EsFallback)>();
        foreach (var p in pasos)
            if (!resultado.Any() || resultado.Last().Cargo != p.Cargo)
                resultado.Add(p);

        return resultado;
    }

    public async Task<Tbsolicitude> InicializarFlujo(
        Tbsolicitude solicitud, Tbpersonal solicitante)
    {
        var nivel = DeterminarNivel(solicitante.Cargo ?? "");
        var pasos = await ObtenerAprobadoresConFallback(solicitante, nivel);

        solicitud.NivelSolicitante = nivel;
        solicitud.TotalPasos = pasos.Count;
        solicitud.PasoActual = 1;
        solicitud.Estado = "En proceso";

        // Limpiar pasos anteriores
        solicitud.Paso1Aprobador = solicitud.Paso1Estado = solicitud.Paso1Obs = null;
        solicitud.Paso2Aprobador = solicitud.Paso2Estado = solicitud.Paso2Obs = null;
        solicitud.Paso3Aprobador = solicitud.Paso3Estado = solicitud.Paso3Obs = null;

        if (pasos.Count >= 1)
        {
            var label = pasos[0].EsFallback
                ? $"Gerente General (por ausencia de {pasos[0].Cargo})"
                : pasos[0].Cargo;
            solicitud.Paso1Aprobador = pasos[0].Cargo;
            solicitud.Paso1Estado = "Pendiente";
            solicitud.EtapaAprobacion = $"Pendiente: {label}";
        }
        if (pasos.Count >= 2)
        {
            solicitud.Paso2Aprobador = pasos[1].Cargo;
            solicitud.Paso2Estado = "Esperando";
        }
        if (pasos.Count >= 3)
        {
            solicitud.Paso3Aprobador = pasos[2].Cargo;
            solicitud.Paso3Estado = "Esperando";
        }

        return solicitud;
    }

    public async Task<(bool completado, string? siguienteDestino)> AvanzarPaso(
        Tbsolicitude solicitud, string observacion)
    {
        var paso = solicitud.PasoActual ?? 1;
        switch (paso)
        {
            case 1: solicitud.Paso1Estado = "Aprobado"; solicitud.Paso1Obs = observacion; break;
            case 2: solicitud.Paso2Estado = "Aprobado"; solicitud.Paso2Obs = observacion; break;
            case 3: solicitud.Paso3Estado = "Aprobado"; solicitud.Paso3Obs = observacion; break;
        }

        if (paso < (solicitud.TotalPasos ?? 1))
        {
            solicitud.PasoActual = paso + 1;
            var siguienteCargo = paso + 1 == 2 ? solicitud.Paso2Aprobador
                               : paso + 1 == 3 ? solicitud.Paso3Aprobador
                               : null;
            solicitud.EtapaAprobacion = $"Pendiente: {siguienteCargo}";
            switch (paso + 1)
            {
                case 2: solicitud.Paso2Estado = "Pendiente"; break;
                case 3: solicitud.Paso3Estado = "Pendiente"; break;
            }
            var area = await ObtenerAreaSolicitante(solicitud);
            var destino = await BuscarAprobadorPorCargo(siguienteCargo ?? "", area);
            return (false, destino);
        }
        else
        {
            solicitud.Estado = "Aprobada";
            solicitud.EtapaAprobacion = "Aprobada";
            return (true, null);
        }
    }

    public Tbsolicitude RechazarPaso(Tbsolicitude solicitud, string observacion)
    {
        var paso = solicitud.PasoActual ?? 1;
        switch (paso)
        {
            case 1: solicitud.Paso1Estado = "Rechazado"; solicitud.Paso1Obs = observacion; break;
            case 2: solicitud.Paso2Estado = "Rechazado"; solicitud.Paso2Obs = observacion; break;
            case 3: solicitud.Paso3Estado = "Rechazado"; solicitud.Paso3Obs = observacion; break;
        }
        solicitud.Estado = "Rechazada";
        solicitud.EtapaAprobacion = "Rechazada";
        return solicitud;
    }

    public Tbsolicitude DevolverSolicitud(Tbsolicitude solicitud, string observacion)
    {
        var paso = solicitud.PasoActual ?? 1;
        switch (paso)
        {
            case 1: solicitud.Paso1Obs = observacion; break;
            case 2: solicitud.Paso2Obs = observacion; break;
            case 3: solicitud.Paso3Obs = observacion; break;
        }
        solicitud.Estado = "Devuelta";
        solicitud.EtapaAprobacion = "Devuelta al solicitante";
        solicitud.FechaDevolucion = DateTime.Now;
        return solicitud;
    }

    public async Task<bool> PuedeActuar(Tbsolicitude solicitud, string userName)
    {
        // Estados cerrados — nadie puede actuar
        if (solicitud.Estado is "Aprobada" or "Rechazada" or "Devuelta" or "Finalizada")
            return false;

        // Buscar identity y email del usuario actual
        var identityUser = await _userManager.FindByNameAsync(userName);
        var emailUser = identityUser?.Email ?? "";

        // Admin NO actúa en el flujo
        if (identityUser != null)
        {
            var roles = await _userManager.GetRolesAsync(identityUser);
            if (roles.Contains("Administrador")) return false;
        }

        // Buscar el personal buscando por usuario o email
        var personal = await _context.Tbpersonals
            .FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == userName ||
                p.CorreoCorporativo == userName ||
                p.UsuarioCorporativo == emailUser ||
                p.CorreoCorporativo == emailUser);

        if (personal == null) return false;

        var cargoUsuario = personal.Cargo?.Trim() ?? "";
        var areaUsuario = personal.Area?.Trim() ?? "";

        var paso = solicitud.PasoActual ?? 1;
        var cargo = paso == 1 ? solicitud.Paso1Aprobador
                  : paso == 2 ? solicitud.Paso2Aprobador
                  : solicitud.Paso3Aprobador;

        if (string.IsNullOrEmpty(cargo)) return false;

        // Capital Humano: Gerente, Coordinador o Asistente del área
        if (cargo == "Capital Humano")
        {
            bool esDeCapHumano = string.Equals(areaUsuario, "Capital Humano",
                                     StringComparison.OrdinalIgnoreCase);
            bool tieneRolValido =
                cargoUsuario.StartsWith("Gerente", StringComparison.OrdinalIgnoreCase) ||
                cargoUsuario.StartsWith("Coordinador", StringComparison.OrdinalIgnoreCase) ||
                cargoUsuario.StartsWith("Asistente", StringComparison.OrdinalIgnoreCase);
            return esDeCapHumano && tieneRolValido;
        }

        // Directivo — cargo puede ser "Directivo" o "Directivo Calidad" etc.
        if (cargo == "Directivo")
            return cargoUsuario.StartsWith("Directivo", StringComparison.OrdinalIgnoreCase);

        // Gerente General
        if (cargo == "Gerente General")
            return string.Equals(cargoUsuario, "Gerente General",
                                 StringComparison.OrdinalIgnoreCase);

        // Gerente/Jefe de área específica
        var partes = cargo.Trim().Split(' ', 2);
        var nivelEsperado = partes[0];
        var areaEsperada = partes.Length > 1 ? partes[1] : "";

        bool nivelOk = cargoUsuario.StartsWith(nivelEsperado, StringComparison.OrdinalIgnoreCase);
        bool areaOk = string.IsNullOrEmpty(areaEsperada) ||
                       string.Equals(areaUsuario, areaEsperada, StringComparison.OrdinalIgnoreCase);

        return nivelOk && areaOk;
    }

    private async Task<string> ObtenerAreaSolicitante(Tbsolicitude solicitud)
    {
        var personal = await _context.Tbpersonals
            .FirstOrDefaultAsync(p => p.CC == solicitud.CC);
        return personal?.Area ?? "";
    }

    private async Task<string?> BuscarAprobadorPorCargo(string cargo, string area)
    {
        var personal = await BuscarPersonalPorCargo(cargo, area);
        return personal?.UsuarioCorporativo ?? personal?.CorreoCorporativo;
    }

    private async Task<Tbpersonal?> BuscarPersonalPorCargo(string cargo, string area)
    {
        var partes = cargo.Trim().Split(' ', 2);
        var nivel = partes[0];
        var areaCargo = partes.Length > 1 ? partes[1] : area;

        return await _context.Tbpersonals
            .FirstOrDefaultAsync(p =>
                p.Cargo != null &&
                p.Cargo.StartsWith(nivel) &&
                (string.IsNullOrEmpty(areaCargo) ||
                 (p.Area != null && p.Area.ToLower() == areaCargo.ToLower())));
    }

    public async Task<IdentityUser?> BuscarAprobador(string cargo, string area)
    {
        var personal = await BuscarPersonalPorCargo(cargo, area);
        if (personal == null) return null;
        return await _userManager.FindByEmailAsync(personal.CorreoCorporativo ?? "") ??
               await _userManager.FindByNameAsync(personal.UsuarioCorporativo ?? "");
    }
}