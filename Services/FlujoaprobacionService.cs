using Farmacol.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Services;

public class FlujoAprobacionService
{
    private readonly Farmacol1Context _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly DelegacionService _delegacion;

    public FlujoAprobacionService(Farmacol1Context context,
                                   UserManager<IdentityUser> userManager,
                                   DelegacionService delegacion)
    {
        _context = context;
        _userManager = userManager;
        _delegacion = delegacion;
    }

    // ── Constantes Capital Humano ─────────────────────────────────────────
    private const string CARGO_ASISTENTE_CH = "Asistente Capital Humano";
    private const string CARGO_COORD_CH = "Coordinador Capital Humano";
    private const string CARGO_GERENTE_CH = "Gerente Capital Humano";

    // ── Helpers en memoria (NO van a SQL, seguros con OrdinalIgnoreCase) ──
    private static bool EsAreaCapHumano(string? area) =>
        string.Equals(area, "Capital Humano", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(area, "RRHH", StringComparison.OrdinalIgnoreCase);

    // ── DeterminarNivel ───────────────────────────────────────────────────
    public string DeterminarNivel(string? cargo)
    {
        if (string.IsNullOrWhiteSpace(cargo)) return "Usuario";

        var c = cargo.Trim();

        var cLower = c.ToLowerInvariant();

        if (cLower.Contains("técnico ti") || cLower.Contains("tecnico ti"))
            return "Técnico TI";

        if (cLower.Contains("asistente administrativo") ||
            cLower.Contains("asistente administrativo de recursos humanos"))
            return "Asistente Administrativo RRHH";

        if (string.Equals(c, CARGO_GERENTE_CH, StringComparison.OrdinalIgnoreCase))
            return "RRHH";
        if (string.Equals(c, CARGO_COORD_CH, StringComparison.OrdinalIgnoreCase))
            return "Coordinador Capital Humano";
        if (string.Equals(c, CARGO_ASISTENTE_CH, StringComparison.OrdinalIgnoreCase))
            return "Asistente Capital Humano";

        if (cLower == "directivo" || cLower.StartsWith("directivo ")) return "Directivo";
        if (cLower == "gerente general") return "Gerente General";
        if (cLower.Contains("capital humano")) return "RRHH";
        if (cLower.StartsWith("gerente")) return "Gerente";
        if (cLower.StartsWith("jefe")) return "Jefe";
        if (cLower.StartsWith("coordinador")) return "Coordinador";
        if (cLower.StartsWith("asistente")) return "Asistente";
        if (cLower == "administrador") return "Administrador";

        return "Usuario";
    }

    // ── Verificar aprobador disponible ───────────────────────────────────
    private async Task<bool> AprobadorDisponible(string cargo, string area)
    {
        var partes = cargo.Trim().Split(' ', 2);
        var nivelEsperado = partes[0];
        var areaEsperada = partes.Length > 1 ? partes[1] : area;

        bool existe = await _context.Tbpersonals.AnyAsync(p =>
            p.Cargo != null &&
            p.Cargo.StartsWith(nivelEsperado) &&
            (string.IsNullOrEmpty(areaEsperada) ||
             (p.Area != null && p.Area.ToLower() == areaEsperada.ToLower())));

        if (!existe) return false;
        return !await _delegacion.EstaInhabilitado(cargo, areaEsperada);
    }

    private async Task<bool> AprobadorExactoDisponible(string cargoExacto)
    {
        var cargoLower = cargoExacto.ToLower();
        bool existe = await _context.Tbpersonals
            .AnyAsync(p => p.Cargo != null && p.Cargo.ToLower() == cargoLower);
        if (!existe) return false;
        return !await _delegacion.EstaInhabilitado(cargoExacto, "Capital Humano");
    }

    // ── ObtenerAprobadoresConFallback ─────────────────────────────────────
    public async Task<List<(string Cargo, string Area, bool EsFallback)>>
        ObtenerAprobadoresConFallback(Tbpersonal solicitante, string nivelSolicitante)
    {
        var pasos = new List<(string Cargo, string Area, bool EsFallback)>();

        switch (nivelSolicitante)
        {
            case "Técnico TI":
            case "Asistente Administrativo RRHH":
                {
                    pasos.Add(("Capital Humano", "Capital Humano", false));
                    pasos.Add((CARGO_GERENTE_CH, "Capital Humano", false));
                    break;
                }

            case "Asistente Capital Humano":
                {
                    bool coordDisp = await AprobadorExactoDisponible(CARGO_COORD_CH);
                    bool gerenteDisp = await AprobadorExactoDisponible(CARGO_GERENTE_CH);
                    pasos.Add(coordDisp
                        ? (CARGO_COORD_CH, "Capital Humano", false)
                        : ("Gerente General", "Gerencia General", true));
                    if (gerenteDisp)
                        pasos.Add((CARGO_GERENTE_CH, "Capital Humano", false));
                    else if (!pasos.Any(p => p.Cargo == "Gerente General"))
                        pasos.Add(("Gerente General", "Gerencia General", true));
                    break;
                }

            case "Coordinador Capital Humano":
                {
                    bool asistDisp = await AprobadorExactoDisponible(CARGO_ASISTENTE_CH);
                    bool gerenteDisp = await AprobadorExactoDisponible(CARGO_GERENTE_CH);
                    pasos.Add(asistDisp
                        ? (CARGO_ASISTENTE_CH, "Capital Humano", false)
                        : ("Gerente General", "Gerencia General", true));
                    if (gerenteDisp)
                        pasos.Add((CARGO_GERENTE_CH, "Capital Humano", false));
                    else if (!pasos.Any(p => p.Cargo == "Gerente General"))
                        pasos.Add(("Gerente General", "Gerencia General", true));
                    break;
                }

            case "RRHH":
                pasos.Add(("Gerente General", "Gerencia General", false));
                break;

            case "Gerente":
                pasos.Add(("Capital Humano", "Capital Humano", false));
                pasos.Add(("Gerente General", "Gerencia General", false));
                break;

            case "Gerente General":
                pasos.Add(("Capital Humano", "Capital Humano", false));
                pasos.Add(("Directivo", "Directivo", false));
                break;

            case "Jefe":
                {
                    pasos.Add(("Capital Humano", "Capital Humano", false));
                    var gerenteCargo = "Gerente " + solicitante.Area;
                    var gerenteArea = solicitante.Area ?? "";
                    bool gerenteDisp = await AprobadorDisponible(gerenteCargo, gerenteArea);
                    pasos.Add(gerenteDisp
                        ? (gerenteCargo, gerenteArea, false)
                        : ("Gerente General", "Gerencia General", true));
                    break;
                }

            case "Coordinador":
            case "Asistente":
            case "Usuario":
                {
                    pasos.Add(("Capital Humano", "Capital Humano", false));
                    var jefeCargo = "Jefe " + solicitante.Area;
                    var gerenteCargo = "Gerente " + solicitante.Area;
                    var area = solicitante.Area ?? "";
                    bool jefeDisp = await AprobadorDisponible(jefeCargo, area);
                    bool gerenteDisp = await AprobadorDisponible(gerenteCargo, area);
                    pasos.Add(jefeDisp
                        ? (jefeCargo, area, false)
                        : ("Gerente General", "Gerencia General", true));
                    if (gerenteDisp)
                        pasos.Add((gerenteCargo, area, false));
                    else if (!pasos.Any(p => p.Cargo == "Gerente General"))
                        pasos.Add(("Gerente General", "Gerencia General", true));
                    break;
                }
        }

        var resultado = new List<(string Cargo, string Area, bool EsFallback)>();
        foreach (var p in pasos)
            if (!resultado.Any() || resultado.Last().Cargo != p.Cargo)
                resultado.Add(p);

        return resultado;
    }

    // ── InicializarFlujo (con parámetro esSST) ────────────────────────────
    public async Task<Tbsolicitude> InicializarFlujo(
        Tbsolicitude solicitud, Tbpersonal solicitante, bool esSST = false)
    {
        // ========== FLUJO ESPECIAL PARA SST ==========
        if (esSST)
        {
            solicitud.NivelSolicitante = "SST";
            solicitud.TotalPasos = 2;
            solicitud.PasoActual = 1;
            solicitud.Estado = "En proceso";
            solicitud.Paso1Aprobador = "Capital Humano";
            solicitud.Paso1Estado = "Pendiente";
            solicitud.Paso2Aprobador = CARGO_GERENTE_CH;
            solicitud.Paso2Estado = "Esperando";
            solicitud.Paso3Aprobador = null;
            solicitud.Paso3Estado = null;
            solicitud.EtapaAprobacion = "Pendiente: Capital Humano";
            return solicitud;
        }

        // ========== FLUJO NORMAL (por nivel detectado) ==========
        var nivel = DeterminarNivel(solicitante.Cargo ?? "");
        var pasos = await ObtenerAprobadoresConFallback(solicitante, nivel);

        solicitud.NivelSolicitante = nivel;
        solicitud.TotalPasos = pasos.Count;
        solicitud.PasoActual = 1;
        solicitud.Estado = "En proceso";

        solicitud.Paso1Aprobador = solicitud.Paso1Estado = solicitud.Paso1Obs = null;
        solicitud.Paso2Aprobador = solicitud.Paso2Estado = solicitud.Paso2Obs = null;
        solicitud.Paso3Aprobador = solicitud.Paso3Estado = solicitud.Paso3Obs = null;

        if (pasos.Count >= 1)
        {
            solicitud.Paso1Aprobador = pasos[0].Cargo;
            solicitud.Paso1Estado = "Pendiente";
            solicitud.EtapaAprobacion = pasos[0].EsFallback
                ? $"Pendiente: Gerente General (por ausencia de aprobador)"
                : $"Pendiente: {pasos[0].Cargo}";
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

    // ── AvanzarPaso ───────────────────────────────────────────────────────
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

            if (siguienteCargo != null
                && siguienteCargo != "Gerente General"
                && siguienteCargo != "Capital Humano"
                && siguienteCargo != "Directivo"
                && siguienteCargo != CARGO_ASISTENTE_CH
                && siguienteCargo != CARGO_COORD_CH
                && siguienteCargo != CARGO_GERENTE_CH)
            {
                var partesS = siguienteCargo.Trim().Split(' ', 2);
                var areaS = partesS.Length > 1 ? partesS[1] : "";
                if (await _delegacion.EstaInhabilitado(siguienteCargo, areaS))
                {
                    siguienteCargo = "Gerente General";
                    switch (paso + 1)
                    {
                        case 2: solicitud.Paso2Aprobador = "Gerente General"; break;
                        case 3: solicitud.Paso3Aprobador = "Gerente General"; break;
                    }
                }
            }

            solicitud.EtapaAprobacion = $"Pendiente: {siguienteCargo}";
            switch (paso + 1)
            {
                case 2: solicitud.Paso2Estado = "Pendiente"; break;
                case 3: solicitud.Paso3Estado = "Pendiente"; break;
            }

            var areaSol = await ObtenerAreaSolicitante(solicitud);
            var destino = await BuscarAprobadorPorCargo(siguienteCargo ?? "", areaSol);
            return (false, destino);
        }
        else
        {
            solicitud.Estado = "Aprobada";
            solicitud.EtapaAprobacion = "Aprobada";
            return (true, null);
        }
    }

    // ── RechazarPaso ──────────────────────────────────────────────────────
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

    // ── DevolverSolicitud ─────────────────────────────────────────────────
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

    // ── PuedeActuar ───────────────────────────────────────────────────────
    public async Task<bool> PuedeActuar(Tbsolicitude solicitud, string userName)
    {
        if (solicitud.Estado is "Aprobada" or "Rechazada" or "Devuelta" or "Finalizada")
            return false;

        var identityUser = await _userManager.FindByNameAsync(userName);
        var emailUser = identityUser?.Email ?? "";

        if (identityUser != null)
        {
            var roles = await _userManager.GetRolesAsync(identityUser);
            if (roles.Contains("Administrador")) return false;
        }

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

        // Cargos exactos Capital Humano
        if (string.Equals(cargo, CARGO_COORD_CH, StringComparison.OrdinalIgnoreCase))
            return string.Equals(cargoUsuario, CARGO_COORD_CH, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(cargo, CARGO_ASISTENTE_CH, StringComparison.OrdinalIgnoreCase))
            return string.Equals(cargoUsuario, CARGO_ASISTENTE_CH, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(cargo, CARGO_GERENTE_CH, StringComparison.OrdinalIgnoreCase))
            return string.Equals(cargoUsuario, CARGO_GERENTE_CH, StringComparison.OrdinalIgnoreCase);

        // Capital Humano genérico
        if (cargo == "Capital Humano")
        {
            bool esDeCapHumano = EsAreaCapHumano(areaUsuario);
            bool tieneRolValido =
                cargoUsuario.StartsWith("Gerente", StringComparison.OrdinalIgnoreCase) ||
                cargoUsuario.StartsWith("Coordinador", StringComparison.OrdinalIgnoreCase) ||
                cargoUsuario.StartsWith("Asistente", StringComparison.OrdinalIgnoreCase);
            return esDeCapHumano && tieneRolValido;
        }

        if (cargo == "Directivo")
            return cargoUsuario.StartsWith("Directivo", StringComparison.OrdinalIgnoreCase);

        if (cargo == "Gerente General")
            return string.Equals(cargoUsuario, "Gerente General", StringComparison.OrdinalIgnoreCase);

        var partes = cargo.Trim().Split(' ', 2);
        var nivelEsperado = partes[0];
        var areaEsperada = partes.Length > 1 ? partes[1] : "";

        bool nivelOk = cargoUsuario.StartsWith(nivelEsperado, StringComparison.OrdinalIgnoreCase);
        bool areaOk = string.IsNullOrEmpty(areaEsperada) ||
                       string.Equals(areaUsuario, areaEsperada, StringComparison.OrdinalIgnoreCase);
        return nivelOk && areaOk;
    }

    // ── Helpers privados ──────────────────────────────────────────────────
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
        var cargoLower = cargo.ToLower();
        if (cargoLower == CARGO_ASISTENTE_CH.ToLower() ||
            cargoLower == CARGO_COORD_CH.ToLower() ||
            cargoLower == CARGO_GERENTE_CH.ToLower())
        {
            return await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.Cargo != null && p.Cargo.ToLower() == cargoLower);
        }

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