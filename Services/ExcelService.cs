using ClosedXML.Excel;
using Farmacol.Models;

namespace Farmacol.Services;

public class ExcelService
{
    private static void EstilizarEncabezado(IXLWorksheet ws, int columnas)
    {
        var rango = ws.Range(1, 1, 1, columnas);
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
        rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.SheetView.FreezeRows(1);
    }

    private static void AutoAjustar(IXLWorksheet ws)
    {
        ws.Columns().AdjustToContents(8, 60);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportarPersonal(IEnumerable<Tbpersonal> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Personal Activo");

        // Encabezados
        string[] cols = {
            "CC", "Nombre", "Cargo", "Área", "Gerencia", "Ciudad Trabajo",
            "Fecha Ingreso", "Tipo Contrato", "EPS", "Fondo Pensiones",
            "ARL", "Correo Corporativo", "Usuario Corporativo"
        };
        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
        EstilizarEncabezado(ws, cols.Length);

        int fila = 2;
        foreach (var p in datos)
        {
            ws.Cell(fila, 1).Value = p.CC;
            ws.Cell(fila, 2).Value = p.NombreColaborador ?? "";
            ws.Cell(fila, 3).Value = p.Cargo ?? "";
            ws.Cell(fila, 4).Value = p.Area ?? "";
            ws.Cell(fila, 5).Value = p.Gerencia ?? "";
            ws.Cell(fila, 6).Value = p.CiudadTrabajo ?? "";
            ws.Cell(fila, 7).Value = p.FechaIngreso.HasValue
                ? p.FechaIngreso.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(fila, 8).Value = p.TipoContrato ?? "";
            ws.Cell(fila, 9).Value = p.Eps ?? "";
            ws.Cell(fila, 10).Value = p.FondoPensiones ?? "";
            ws.Cell(fila, 11).Value = p.Arl ?? "";
            ws.Cell(fila, 12).Value = p.CorreoCorporativo ?? "";
            ws.Cell(fila, 13).Value = p.UsuarioCorporativo ?? "";
            if (fila % 2 == 0)
                ws.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0f5ff");
            fila++;
        }

        AutoAjustar(ws);
        ws.Cell(fila + 1, 1).Value = $"Total: {datos.Count()} registros";
        ws.Cell(fila + 1, 1).Style.Font.Bold = true;

        return ToBytes(wb);
    }

    public byte[] ExportarSolicitudes(IEnumerable<Tbsolicitude> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Solicitudes");

        string[] cols = {
            "ID", "CC", "Nombre", "Cargo", "Tipo Solicitud", "Subtipo",
            "Estado", "Fecha Solicitud", "Fecha Inicio", "Fecha Fin",
            "Total Días", "Total Horas", "Motivo",
            "Paso 1 Aprobador", "Paso 1 Estado",
            "Paso 2 Aprobador", "Paso 2 Estado",
            "Paso 3 Aprobador", "Paso 3 Estado",
            "Etapa Aprobación"
        };
        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
        EstilizarEncabezado(ws, cols.Length);

        int fila = 2;
        foreach (var s in datos)
        {
            ws.Cell(fila, 1).Value = s.IdSolicitud;
            ws.Cell(fila, 2).Value = s.CC;
            ws.Cell(fila, 3).Value = s.Nombre ?? "";
            ws.Cell(fila, 4).Value = s.Cargo ?? "";
            ws.Cell(fila, 5).Value = s.TipoSolicitud ?? "";
            ws.Cell(fila, 6).Value = s.SubtipoPermiso ?? "";
            ws.Cell(fila, 7).Value = s.Estado ?? "";
            ws.Cell(fila, 8).Value = s.FechaSolicitud.HasValue
                ? s.FechaSolicitud.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(fila, 9).Value = s.FechaInicio.HasValue
                ? s.FechaInicio.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(fila, 10).Value = s.FechaFin.HasValue
                ? s.FechaFin.Value.ToString("dd/MM/yyyy") : "";
            ws.Cell(fila, 11).Value = s.TotalDias ?? 0;
            ws.Cell(fila, 12).Value = s.TotalHoras ?? 0;
            ws.Cell(fila, 13).Value = s.Motivo ?? "";
            ws.Cell(fila, 14).Value = s.Paso1Aprobador ?? "";
            ws.Cell(fila, 15).Value = s.Paso1Estado ?? "";
            ws.Cell(fila, 16).Value = s.Paso2Aprobador ?? "";
            ws.Cell(fila, 17).Value = s.Paso2Estado ?? "";
            ws.Cell(fila, 18).Value = s.Paso3Aprobador ?? "";
            ws.Cell(fila, 19).Value = s.Paso3Estado ?? "";
            ws.Cell(fila, 20).Value = s.EtapaAprobacion ?? "";

            // Color por estado
            var color = s.Estado switch
            {
                "Aprobada" => XLColor.FromHtml("#d4edda"),
                "Rechazada" => XLColor.FromHtml("#f8d7da"),
                "Devuelta" => XLColor.FromHtml("#fff3cd"),
                "Finalizada" => XLColor.FromHtml("#e2e3e5"),
                _ => fila % 2 == 0 ? XLColor.FromHtml("#f8f9fa") : XLColor.White
            };
            ws.Row(fila).Style.Fill.BackgroundColor = color;
            fila++;
        }

        AutoAjustar(ws);
        ws.Cell(fila + 1, 1).Value = $"Total: {datos.Count()} registros";
        ws.Cell(fila + 1, 1).Style.Font.Bold = true;

        return ToBytes(wb);
    }

    public byte[] ExportarExpedientes(IEnumerable<TbExpediente> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Expedientes");

        string[] cols = {
            "ID", "CC", "Nombre Archivo", "Tipo Documento",
            "Módulo", "Visible", "Fecha Subida", "Subido Por"
        };
        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
        EstilizarEncabezado(ws, cols.Length);

        int fila = 2;
        foreach (var d in datos)
        {
            ws.Cell(fila, 1).Value = d.Id;
            ws.Cell(fila, 2).Value = d.CC;
            ws.Cell(fila, 3).Value = d.NombreArchivo;
            ws.Cell(fila, 4).Value = d.TipoDocumento ?? "";
            ws.Cell(fila, 5).Value = d.Modulo ?? "";
            ws.Cell(fila, 6).Value = d.Visible ? "Sí" : "No";
            ws.Cell(fila, 7).Value = d.FechaSubida.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(fila, 8).Value = d.SubidoPor ?? "";
            if (fila % 2 == 0)
                ws.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
            fila++;
        }

        AutoAjustar(ws);
        ws.Cell(fila + 1, 1).Value = $"Total: {datos.Count()} registros";
        ws.Cell(fila + 1, 1).Style.Font.Bold = true;

        return ToBytes(wb);
    }

    public byte[] ExportarInventario(IEnumerable<Tbinventario> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Inventario TI");

        string[] cols = {
            "ID Equipo", "Dispositivo", "Marca", "Modelo",
            "Serie", "IMEI", "Ubicación", "Ubicación 2",
            "Planta", "CC Asignado", "Observación"
        };
        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
        EstilizarEncabezado(ws, cols.Length);

        int fila = 2;
        foreach (var e in datos)
        {
            ws.Cell(fila, 1).Value = e.IdEquipo;
            ws.Cell(fila, 2).Value = e.Dispositivo ?? "";
            ws.Cell(fila, 3).Value = e.Marca ?? "";
            ws.Cell(fila, 4).Value = e.Modelo ?? "";
            ws.Cell(fila, 5).Value = e.Serie ?? "";
            ws.Cell(fila, 6).Value = e.Imei ?? "";
            ws.Cell(fila, 7).Value = e.Ubicación ?? "";
            ws.Cell(fila, 8).Value = e.Ubicación2 ?? "";
            ws.Cell(fila, 9).Value = e.Planta ?? "";
            ws.Cell(fila, 10).Value = e.CC.HasValue ? e.CC.Value.ToString() : "";
            ws.Cell(fila, 11).Value = e.Observación ?? "";
            if (fila % 2 == 0)
                ws.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#edfaf3");
            fila++;
        }

        AutoAjustar(ws);
        ws.Cell(fila + 1, 1).Value = $"Total: {datos.Count()} registros";
        ws.Cell(fila + 1, 1).Style.Font.Bold = true;

        return ToBytes(wb);
    }

    public byte[] ExportarAuditTrail(IEnumerable<TbAuditTrail> datos)
    {
        using var wb = new XLWorkbook();

        // ==================== HOJA DETALLADA ====================
        var ws = wb.Worksheets.Add("Audit Trail");

        string[] cols = {
        "ID", "Fecha", "Hora", "Usuario", "Módulo",
        "Acción", "Descripción", "ID Entidad", "IP"
    };

        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];

        EstilizarEncabezado(ws, cols.Length);

        int fila = 2;
        foreach (var a in datos)
        {
            ws.Cell(fila, 1).Value = a.Id;
            ws.Cell(fila, 2).Value = a.Fecha.ToString("dd/MM/yyyy");
            ws.Cell(fila, 3).Value = a.Fecha.ToString("HH:mm:ss");
            ws.Cell(fila, 4).Value = a.Usuario ?? "";
            ws.Cell(fila, 5).Value = a.Modulo ?? "";
            ws.Cell(fila, 6).Value = a.Accion ?? "";
            ws.Cell(fila, 7).Value = a.Descripcion ?? "";
            ws.Cell(fila, 8).Value = a.EntidadId ?? "";
            ws.Cell(fila, 9).Value = a.Ip ?? "";

            // Color por acción
            var color = a.Accion switch
            {
                "Crear" or "Aprobar" or "Login" => XLColor.FromHtml("#d4edda"),
                "Rechazar" or "Eliminar" or "Login fallido" => XLColor.FromHtml("#f8d7da"),
                "Devolver" or "Cancelar" => XLColor.FromHtml("#fff3cd"),
                _ => fila % 2 == 0 ? XLColor.FromHtml("#f8f9fa") : XLColor.White
            };
            ws.Row(fila).Style.Fill.BackgroundColor = color;
            fila++;
        }

        AutoAjustar(ws);
        ws.Cell(fila + 2, 1).Value = $"Total registros: {datos.Count()}";
        ws.Cell(fila + 2, 1).Style.Font.Bold = true;

        // ==================== HOJA DE RESUMEN (mejorada) ====================
        var wsResumen = wb.Worksheets.Add("Resumen");

        wsResumen.Cell(1, 1).Value = "RESUMEN DE ACTIVIDADES DEL SISTEMA";
        wsResumen.Cell(1, 1).Style.Font.Bold = true;
        wsResumen.Cell(1, 1).Style.Font.FontSize = 16;
        wsResumen.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        wsResumen.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");

        int row = 4;

        // Por Módulo
        wsResumen.Cell(row, 1).Value = "Actividades por Módulo";
        wsResumen.Cell(row, 1).Style.Font.Bold = true;
        row++;

        var porModulo = datos.GroupBy(a => a.Modulo ?? "Sin Módulo")
                             .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                             .OrderByDescending(x => x.Cantidad);

        foreach (var item in porModulo)
        {
            wsResumen.Cell(row, 1).Value = item.Nombre;
            wsResumen.Cell(row, 2).Value = item.Cantidad;
            row++;
        }

        row += 4;

        // Por Acción
        wsResumen.Cell(row, 1).Value = "Actividades por Acción";
        wsResumen.Cell(row, 1).Style.Font.Bold = true;
        row++;

        var porAccion = datos.GroupBy(a => a.Accion ?? "Sin Acción")
                             .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
                             .OrderByDescending(x => x.Cantidad);

        foreach (var item in porAccion)
        {
            wsResumen.Cell(row, 1).Value = item.Nombre;
            wsResumen.Cell(row, 2).Value = item.Cantidad;
            row++;
        }

        // Formato bonito en resumen
        wsResumen.Columns().AdjustToContents();
        wsResumen.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        return ToBytes(wb);
    }

    public byte[] ExportarInhabilitaciones(IEnumerable<TbDelegacion> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Inhabilitaciones");

        string[] cols = {
            "ID", "CC", "Nombre", "Cargo", "Área",
            "Motivo", "Fecha Inicio", "Fecha Fin", "Activa",
            "Creada Por", "Fecha Creación", "Aprobador Original"
        };
        for (int i = 0; i < cols.Length; i++)
            ws.Cell(1, i + 1).Value = cols[i];
        EstilizarEncabezado(ws, cols.Length);

        int fila = 2;
        foreach (var d in datos)
        {
            ws.Cell(fila, 1).Value = d.Id;
            ws.Cell(fila, 2).Value = d.CC;
            ws.Cell(fila, 3).Value = d.Nombre ?? "";
            ws.Cell(fila, 4).Value = d.Cargo ?? "";
            ws.Cell(fila, 5).Value = d.Area ?? "";
            ws.Cell(fila, 6).Value = d.Motivo ?? "";
            ws.Cell(fila, 7).Value = d.FechaInicio.ToString("dd/MM/yyyy");
            ws.Cell(fila, 8).Value = d.FechaFin.ToString("dd/MM/yyyy");
            ws.Cell(fila, 9).Value = d.Activa ? "Sí" : "No";
            ws.Cell(fila, 10).Value = d.CreadaPor ?? "";
            ws.Cell(fila, 11).Value = d.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(fila, 12).Value = d.AprobadorOriginal ?? "";

            if (!d.Activa)
                ws.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#e2e3e5");
            else if (fila % 2 == 0)
                ws.Row(fila).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
            fila++;
        }

        AutoAjustar(ws);
        ws.Cell(fila + 1, 1).Value = $"Total: {datos.Count()} registros";
        ws.Cell(fila + 1, 1).Style.Font.Bold = true;

        return ToBytes(wb);
    }
}
