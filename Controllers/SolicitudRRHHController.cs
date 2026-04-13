using ClosedXML.Excel;
using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers;

[Authorize]
public class SolicitudRRHHController : Controller
{
    private readonly Farmacol1Context _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly EmailService _email;
    private readonly DocumentoService _docSvc;
    private readonly IWebHostEnvironment _env;
    private readonly AuditService _audit;

    private static readonly string[] TiposDocumento = {
        "Constancia laboral", "Paz y salvo",
        "Certificado de ingresos", "Carta de retiro"
    };

    public SolicitudRRHHController(Farmacol1Context context,
        UserManager<IdentityUser> userManager, EmailService email,
        DocumentoService docSvc, IWebHostEnvironment env, AuditService audit)
    {
        _context = context;
        _userManager = userManager;
        _email = email;
        _docSvc = docSvc;
        _env = env;
        _audit = audit;
    }

    private async Task<Tbpersonal?> BuscarPersonalActual()
    {
        var userName = User.Identity?.Name ?? "";
        var userObj = await _userManager.FindByNameAsync(userName);
        var correo = userObj?.Email ?? "";
        return await _context.Tbpersonals.FirstOrDefaultAsync(p =>
            p.UsuarioCorporativo == userName ||
            p.CorreoCorporativo == userName ||
            p.CorreoCorporativo == correo);
    }

    private bool EsGerenteCH()
    {
        var cargo = ViewBag.UserCargo as string ?? "";
        return string.Equals(cargo, "Gerente Capital Humano",
                   StringComparison.OrdinalIgnoreCase) ||
               User.IsInRole("Administrador") ||
               User.IsInRole("RRHH");
    }

    // ── CREATE GET ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var personal = await BuscarPersonalActual();
        if (personal == null)
        { TempData["Error"] = "No encontramos tu perfil."; return RedirectToAction("Index", "Home"); }
        ViewBag.Personal = personal;
        ViewBag.TiposDocumento = TiposDocumento;
        return View();
    }

    // ── CREATE POST ───────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string documentoSolicitado, string? motivo)
    {
        var personal = await BuscarPersonalActual();
        if (personal == null)
        { TempData["Error"] = "Perfil no encontrado."; return RedirectToAction("Index", "Home"); }

        if (!TiposDocumento.Contains(documentoSolicitado))
        { TempData["Error"] = "Tipo de documento inválido."; return RedirectToAction(nameof(Create)); }

        var gerenteCH = await _context.Tbpersonals.FirstOrDefaultAsync(p =>
            p.Cargo != null && p.Cargo.ToLower() == "gerente capital humano");

        var solicitud = new Tbsolicitude
        {
            CC = personal.CC,
            Nombre = personal.NombreColaborador ?? "",
            Cargo = personal.Cargo ?? "",
            TipoSolicitud = "Documental",
            DocumentoSolicitado = documentoSolicitado,
            TipoFlujo = "Documental",
            Motivo = motivo ?? $"Solicitud de {documentoSolicitado}",
            FechaSolicitud = DateOnly.FromDateTime(DateTime.Today),
            Estado = "Pendiente",
            EtapaAprobacion = "Gerente Capital Humano",
            Paso1Aprobador = "Capital Humano",
            Paso1Estado = "Pendiente",
            TotalPasos = 1,
            PasoActual = 1,
            NivelSolicitante = personal.Cargo ?? ""
        };

        _context.Tbsolicitudes.Add(solicitud);
        await _context.SaveChangesAsync();

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_SOLICITUD_RRHH, AuditService.ACC_CREAR,
            $"{personal.NombreColaborador} solicitó '{documentoSolicitado}'",
            solicitud.IdSolicitud.ToString());
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(gerenteCH?.CorreoCorporativo))
                await _email.EnviarAsync(gerenteCH.CorreoCorporativo,
                    $"[Farmacol] Solicitud de {documentoSolicitado} — {personal.NombreColaborador}",
                    $@"<div style='font-family:system-ui;max-width:600px;margin:32px auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 4px 20px rgba(0,0,0,.08)'>
                       <h2 style='color:#198754'>📄 Nueva solicitud de documento RRHH</h2>
                       <table style='font-size:.9rem;line-height:1.9;width:100%'>
                         <tr><td width='150'><b>Documento:</b></td><td>{documentoSolicitado}</td></tr>
                         <tr><td><b>Solicitante:</b></td><td>{personal.NombreColaborador}</td></tr>
                         <tr><td><b>Cargo:</b></td><td>{personal.Cargo}</td></tr>
                         <tr><td><b>Área:</b></td><td>{personal.Area}</td></tr>
                         <tr><td><b>CC:</b></td><td>{personal.CC}</td></tr>
                         <tr><td><b>Motivo:</b></td><td>{motivo ?? "—"}</td></tr>
                       </table>
                       <p style='margin-top:16px'>Ingresa al sistema → <b>Gestión doc. RRHH</b> para generar el documento.</p></div>");
        }
        catch { }

        TempData["Exito"] = $"✅ Solicitud de '{documentoSolicitado}' enviada a Capital Humano.";
        return RedirectToAction(nameof(MisSolicitudes));
    }

    // ── MIS SOLICITUDES ───────────────────────────────────────────────────
    public async Task<IActionResult> MisSolicitudes()
    {
        var personal = await BuscarPersonalActual();
        if (personal == null) return View(new List<Tbsolicitude>());
        var lista = await _context.Tbsolicitudes
            .Where(s => s.CC == personal.CC && s.TipoFlujo == "DocumentoRRHH")
            .OrderByDescending(s => s.FechaSolicitud).ToListAsync();
        return View(lista);
    }

    // ── GESTION ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Gestion(string? estado)
    {
        if (!EsGerenteCH()) return Forbid();

        var query = _context.Tbsolicitudes
            .Where(s => s.TipoFlujo == "DocumentoRRHH").AsQueryable();
        if (!string.IsNullOrEmpty(estado))
            query = query.Where(s => s.Estado == estado);

        var lista = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

        ViewBag.Estado = estado;
        ViewBag.Pendientes = await _context.Tbsolicitudes
            .CountAsync(s => s.TipoFlujo == "DocumentoRRHH" && s.Estado == "Pendiente");
        ViewBag.Plantillas = await _context.TbPlantillas
            .Where(p => p.Modulo == "RRHH" && p.Activa).ToListAsync();
        return View(lista);
    }

    // ── GENERAR DOCUMENTO ─────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarDocumento(int idSolicitud, string tipoDocumento)
    {
        if (!EsGerenteCH()) return Forbid();

        var solicitud = await _context.Tbsolicitudes.FindAsync(idSolicitud);
        if (solicitud == null) return NotFound();

        var personal = await _context.Tbpersonals.FindAsync(solicitud.CC);
        if (personal == null)
        { TempData["Error"] = "No se encontró el perfil del solicitante."; return RedirectToAction(nameof(Gestion)); }

        var plantilla = await _context.TbPlantillas
            .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.Modulo == "RRHH" && p.Activa);
        if (plantilla == null)
        { TempData["Error"] = $"No hay plantilla activa para '{tipoDocumento}'."; return RedirectToAction(nameof(Gestion)); }

        var rutaFisica = Path.Combine(_env.WebRootPath,
            plantilla.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        var docxBytes = await _docSvc.GenerarDocxAsync(
            rutaFisica, personal,
            creadorUserName: User.Identity?.Name);

        solicitud.Estado = "Aprobada";
        solicitud.EtapaAprobacion = $"Documento generado: {tipoDocumento}";
        solicitud.Paso1Estado = "Aprobado";
        _context.Update(solicitud);
        await _context.SaveChangesAsync();

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_SOLICITUD_RRHH, AuditService.ACC_GENERAR,
            $"{User.Identity?.Name} generó '{tipoDocumento}' para {personal.NombreColaborador} (CC:{personal.CC})",
            idSolicitud.ToString());
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(personal.CorreoCorporativo))
                await _email.EnviarAsync(personal.CorreoCorporativo,
                    $"[Farmacol] Tu {tipoDocumento} está listo",
                    $@"<div style='font-family:system-ui;max-width:600px;margin:32px auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 4px 20px rgba(0,0,0,.08)'>
                       <h2 style='color:#198754'>📄 Documento listo</h2>
                       <p>Tu <b>{tipoDocumento}</b> ha sido generado por Capital Humano.</p>
                       <p style='color:#6c757d;font-size:.85rem'>Solicita el documento físico en Capital Humano.</p></div>");
        }
        catch { }

        var nombreBase = $"{tipoDocumento.Replace(" ", "_")}_{personal.CC}_{DateTime.Now:yyyyMMdd_HHmm}";
        return File(docxBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"{nombreBase}.docx");
    }

    // ── RECHAZAR ──────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string? observacion)
    {
        if (!EsGerenteCH()) return Forbid();

        var solicitud = await _context.Tbsolicitudes.FindAsync(id);
        if (solicitud == null) return NotFound();

        solicitud.Estado = "Rechazada";
        solicitud.EtapaAprobacion = $"Rechazada. Motivo: {observacion}";
        solicitud.Paso1Estado = "Rechazado";
        _context.Update(solicitud);
        await _context.SaveChangesAsync();

        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_SOLICITUD_RRHH, AuditService.ACC_RECHAZAR,
            $"{User.Identity?.Name} rechazó solicitud #{id} de {solicitud.Nombre}. Motivo: {observacion}",
            id.ToString());
        }
        catch { }

        try
        {
            var personal = await _context.Tbpersonals.FindAsync(solicitud.CC);
            if (!string.IsNullOrEmpty(personal?.CorreoCorporativo))
                await _email.EnviarAsync(personal.CorreoCorporativo,
                    $"[Farmacol] Tu solicitud de {solicitud.DocumentoSolicitado} fue rechazada",
                    $@"<div style='font-family:system-ui;max-width:600px;margin:32px auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 4px 20px rgba(0,0,0,.08)'>
                       <h2 style='color:#dc3545'>❌ Solicitud rechazada</h2>
                       <p>Tu solicitud de <b>{solicitud.DocumentoSolicitado}</b> fue rechazada.</p>
                       <p><b>Motivo:</b> {observacion ?? "Sin observación"}</p></div>");
        }
        catch { }

        TempData["Error"] = "❌ Solicitud rechazada.";
        return RedirectToAction(nameof(Gestion));
    }

    // ── EXPORTAR EXCEL ────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ExportarExcel(string? estado, bool todo = false)
    {
        if (!EsGerenteCH()) return Forbid();

        var query = _context.Tbsolicitudes
            .Where(s => s.TipoFlujo == "DocumentoRRHH").AsQueryable();
        if (!todo && !string.IsNullOrEmpty(estado))
            query = query.Where(s => s.Estado == estado);

        var datos = await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Solicitudes RRHH");
        string[] cols = { "ID","Nombre","CC","Cargo","Área","Documento Solicitado",
                          "Motivo","Fecha Solicitud","Estado","Detalle" };
        for (int i = 0; i < cols.Length; i++) ws.Cell(1, i + 1).Value = cols[i];
        var h = ws.Range(1, 1, 1, cols.Length);
        h.Style.Font.Bold = true; h.Style.Font.FontColor = XLColor.White;
        h.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
        h.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.SheetView.FreezeRows(1);

        int fila = 2;
        foreach (var s in datos)
        {
            ws.Cell(fila, 1).Value = s.IdSolicitud;
            ws.Cell(fila, 2).Value = s.Nombre ?? "";
            ws.Cell(fila, 3).Value = s.CC;
            ws.Cell(fila, 4).Value = s.Cargo ?? "";
            ws.Cell(fila, 5).Value = "";  // Área no está en Tbsolicitude directamente
            ws.Cell(fila, 6).Value = s.DocumentoSolicitado ?? "";
            ws.Cell(fila, 7).Value = s.Motivo ?? "";
            ws.Cell(fila, 8).Value = s.FechaSolicitud?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(fila, 9).Value = s.Estado ?? "";
            ws.Cell(fila, 10).Value = s.EtapaAprobacion ?? "";
            ws.Row(fila).Style.Fill.BackgroundColor = s.Estado switch
            {
                "Aprobada" => XLColor.FromHtml("#d4edda"),
                "Rechazada" => XLColor.FromHtml("#f8d7da"),
                _ => fila % 2 == 0 ? XLColor.FromHtml("#f8f9fa") : XLColor.White
            };
            fila++;
        }
        ws.Columns().AdjustToContents(8, 60);
        ws.Cell(fila + 1, 1).Value = $"Total: {datos.Count} registros";
        ws.Cell(fila + 1, 1).Style.Font.Bold = true;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"SolicitudesRRHH_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}