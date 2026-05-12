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
        "Certificado Laboral", "Paz y salvo",
        "Retiro de Cesantias"
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

    // ── CREATE POST (con campos dinámicos) ────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string documentoSolicitado,
        string? motivo,
        bool IncluirSueldo = false,
        bool IncluirFunciones = false,
        string? DirigidoA = null,
        string? MotivoCesantias = null,
        decimal? MontoCesantias = null,
        DateTime? FechaRetiro = null)
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
            TipoFlujo = "DocumentoRRHH",
            Motivo = motivo ?? $"Solicitud de {documentoSolicitado}",
            FechaSolicitud = DateOnly.FromDateTime(DateTime.Today),
            Estado = "Pendiente",
            EtapaAprobacion = "Gerente Capital Humano",
            Paso1Aprobador = "Capital Humano",
            Paso1Estado = "Pendiente",
            TotalPasos = 1,
            PasoActual = 1,
            NivelSolicitante = personal.Cargo ?? "",

            IncluirSueldo = (documentoSolicitado == "Certificado Laboral") ? IncluirSueldo : null,
            IncluirFunciones = (documentoSolicitado == "Certificado Laboral") ? IncluirFunciones : null,
            DirigidoA = (documentoSolicitado == "Certificado Laboral") ? DirigidoA : null,
            MotivoCesantias = (documentoSolicitado == "Retiro de Cesantias") ? MotivoCesantias : null,
            MontoCesantias = (documentoSolicitado == "Retiro de Cesantias") ? MontoCesantias : null,
            FechaRetiro = (documentoSolicitado == "Paz y salvo" && FechaRetiro.HasValue)
                            ? DateOnly.FromDateTime(FechaRetiro.Value) : null
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
        return RedirectToAction(nameof(MisSolicitudes), new { controller = "Tbsolicitudes" });
    }

    // ── MIS SOLICITUDES (solo para que compile, realmente se usa Tbsolicitudes) ──
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

    // ── REVISAR DOCUMENTO (detalle antes de generar) ──────────────────────
    [HttpGet]
    public async Task<IActionResult> RevisarDocumento(int id)
    {
        if (!EsGerenteCH()) return Forbid();

        var solicitud = await _context.Tbsolicitudes
            .FirstOrDefaultAsync(s => s.IdSolicitud == id && s.TipoFlujo == "DocumentoRRHH");
        if (solicitud == null) return NotFound();

        var personal = await _context.Tbpersonals.FindAsync(solicitud.CC);
        ViewBag.Personal = personal;
        
        var plantillas = await _context.TbPlantillas
            .Where(p => p.Modulo == "RRHH" && p.Activa && p.TipoDocumento == solicitud.DocumentoSolicitado)
            .ToListAsync();
        ViewBag.Plantillas = plantillas;

        return View(solicitud);
    }

    // ── GENERAR DOCUMENTO (con guardado en expediente y correo solo personal) ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarDocumento(int idSolicitud, string tipoDocumento)
    {
        if (!EsGerenteCH()) return Forbid();

        var solicitud = await _context.Tbsolicitudes.FindAsync(idSolicitud);
        if (solicitud == null) return NotFound();

        var personal = await _context.Tbpersonals.FindAsync(solicitud.CC);
        if (personal == null)
        {
            TempData["Error"] = "No se encontró el perfil del solicitante.";
            return RedirectToAction(nameof(Gestion));
        }

        var plantilla = await _context.TbPlantillas
            .FirstOrDefaultAsync(p => p.TipoDocumento == tipoDocumento && p.Modulo == "RRHH" && p.Activa);
        if (plantilla == null)
        {
            TempData["Error"] = $"No hay plantilla activa para '{tipoDocumento}'.";
            return RedirectToAction(nameof(Gestion));
        }

        var rutaFisica = Path.Combine(_env.WebRootPath,
            plantilla.RutaArchivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        // 1️⃣ Generar el documento
        var docxBytes = await _docSvc.GenerarDocxAsync(
            rutaFisica, personal,
            creadorUserName: User.Identity?.Name,
            solicitud: solicitud);

        // 2️⃣ Guardar en expediente digital
        string carpetaExpediente = Path.Combine(_env.WebRootPath, "expedientes", personal.CC.ToString());
        Directory.CreateDirectory(carpetaExpediente);
        string nombreArchivo = $"{tipoDocumento.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
        string rutaRelativa = $"/expedientes/{personal.CC}/{nombreArchivo}";
        string rutaFisicaExpediente = Path.Combine(carpetaExpediente, nombreArchivo);
        await System.IO.File.WriteAllBytesAsync(rutaFisicaExpediente, docxBytes);

        var expediente = new TbExpediente
        {
            CC = personal.CC,
            NombreArchivo = $"{tipoDocumento} - Solicitud #{solicitud.IdSolicitud}",
            TipoDocumento = tipoDocumento,
            RutaArchivo = rutaRelativa,
            FechaSubida = DateTime.Now,
            SubidoPor = User.Identity?.Name ?? "Capital Humano",
            Modulo = "RRHH",
            Visible = true
        };
        _context.TbExpedientes.Add(expediente);
        await _context.SaveChangesAsync();

        // 3️⃣ Enviar correo SOLO al correo personal (sin fallback a corporativo)
        try
        {
            string correoPersonal = personal.CorreoPersonal?.Trim();
            if (!string.IsNullOrEmpty(correoPersonal))
            {
                using var msAdjunto = new MemoryStream(docxBytes);
                await _email.EnviarConAdjuntoAsync(
                    destinatario: correoPersonal,
                    asunto: $"[Farmacol] {tipoDocumento} generado",
                    mensajeHtml: $@"
                        <div style='font-family:system-ui;max-width:600px;margin:32px auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 4px 20px rgba(0,0,0,.08)'>
                            <h2 style='color:#198754'>📄 Documento generado</h2>
                            <p>Hola <b>{personal.NombreColaborador}</b>,</p>
                            <p>Tu <b>{tipoDocumento}</b> ha sido generado por Capital Humano.</p>
                            <p>Adjunto encontrarás el documento en formato Word.</p>
                            <p>También puedes consultarlo en tu <b>Expediente Digital</b> dentro del sistema.</p>
                            <hr />
                            <small class='text-muted'>Solicitud #{solicitud.IdSolicitud} - {DateTime.Now:dd/MM/yyyy HH:mm}</small>
                        </div>",
                    archivoAdjunto: msAdjunto,
                    nombreAdjunto: $"{tipoDocumento.Replace(" ", "_")}_{personal.CC}.docx"
                );
            }
            else
            {
                Console.WriteLine($"El colaborador CC {personal.CC} no tiene registrado un correo personal. No se envió notificación.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo a personal {personal.CorreoPersonal}: {ex.Message}");
        }

        // 4️⃣ Actualizar estado de la solicitud
        solicitud.Estado = "Aprobada";
        solicitud.EtapaAprobacion = $"Documento generado: {tipoDocumento} - Guardado en expediente";
        solicitud.Paso1Estado = "Aprobado";
        _context.Update(solicitud);
        await _context.SaveChangesAsync();

        // 5️⃣ Auditoría
        try
        {
            await _audit.RegistrarAsync(AuditService.MOD_SOLICITUD_RRHH, AuditService.ACC_GENERAR,
                $"{User.Identity?.Name} generó '{tipoDocumento}' para {personal.NombreColaborador} (CC:{personal.CC}) - Guardado en expediente",
                idSolicitud.ToString());
        }
        catch { }

        TempData["Exito"] = $"✅ {tipoDocumento} generado correctamente. Se ha enviado a su correo personal y guardado en expediente.";
        return RedirectToAction(nameof(RevisarDocumento), new { id = idSolicitud });
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
        return RedirectToAction(nameof(RevisarDocumento), new { id });
    }

    // ── EXPORTAR EXCEL (igual que antes) ──────────────────────────────────
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
            ws.Cell(fila, 5).Value = "";
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