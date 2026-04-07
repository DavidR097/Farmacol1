using Farmacol.Models;
using Microsoft.AspNetCore.Http;

namespace Farmacol.Services;

public class AuditService
{
    private readonly Farmacol1Context _context;
    private readonly IHttpContextAccessor _http;

    public AuditService(Farmacol1Context context, IHttpContextAccessor http)
    {
        _context = context;
        _http = http;
    }

    public async Task RegistrarAsync(
        string modulo,
        string accion,
        string? descripcion = null,
        string? entidadId = null)
    {
        var usuario = _http.HttpContext?.User?.Identity?.Name ?? "Sistema";
        var ip = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";

        _context.TbAuditTrails.Add(new TbAuditTrail
        {
            Fecha = DateTime.Now,
            Usuario = usuario,
            Modulo = modulo,
            Accion = accion,
            Descripcion = descripcion,
            EntidadId = entidadId,
            Ip = ip
        });

        await _context.SaveChangesAsync();
    }

    // ── Constantes de módulos ─────────────────────────────────────────────
    public const string MOD_SOLICITUDES = "Solicitudes";
    public const string MOD_EXPEDIENTES = "Expedientes";
    public const string MOD_USUARIOS = "Usuarios";
    public const string MOD_PERSONAL = "Personal";
    public const string MOD_INHABILITACION = "Inhabilitaciones";
    public const string MOD_PLANTILLAS = "Plantillas";
    public const string MOD_SESION = "Sesión";
    public const string MOD_SALAS = "Salas";
    public const string MOD_SOLICITUD_RRHH = "SolicitudRRHH";
    public const string MOD_ANUNCIOS = "Anuncios";

    // ── Constantes de acciones ────────────────────────────────────────────
    public const string ACC_CREAR = "Crear";
    public const string ACC_EDITAR = "Editar";
    public const string ACC_ELIMINAR = "Eliminar";
    public const string ACC_APROBAR = "Aprobar";
    public const string ACC_RECHAZAR = "Rechazar";
    public const string ACC_DEVOLVER = "Devolver";
    public const string ACC_REENVIAR = "Reenviar";
    public const string ACC_SUBIR = "Subir";
    public const string ACC_DESBLOQUEAR = "Desbloquear";
    public const string ACC_RETIRAR = "Retirar";
    public const string ACC_CANCELAR = "Cancelar";
    public const string ACC_LOGIN = "Login";
    public const string ACC_LOGOUT = "Logout";
    public const string ACC_LOGIN_FAIL = "Login fallido";
    public const string ACC_GENERAR = "Generar";
    public const string ACC_RESERVAR = "Reservar";
}
