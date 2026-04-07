using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Farmacol.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using Xceed.Document.NET;
using Xceed.Words.NET;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Farmacol.Services;

public class DocumentoService
{
    private readonly Farmacol1Context _context;

    public DocumentoService(Farmacol1Context context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerarDocxAsync(
        string rutaPlantillaFisica,
        Tbpersonal personal,
        IEnumerable<Tbinventario>? equipos = null,
        Dictionary<int, string>? observaciones = null,
        string? creadorUserName = null,
        Tbsolicitude? solicitud = null,
        string? aprobadoPor = null)
    {
        if (!File.Exists(rutaPlantillaFisica))
            throw new FileNotFoundException($"No se encontró la plantilla: {rutaPlantillaFisica}");

        byte[] plantillaBytes = await File.ReadAllBytesAsync(rutaPlantillaFisica);
        using var msEntrada = new MemoryStream(plantillaBytes);
        using var doc = DocX.Load(msEntrada);

        var hoy = DateTime.Today.ToString("dd 'de' MMMM 'de' yyyy",
            new System.Globalization.CultureInfo("es-CO"));

        // ── Datos auxiliares ──────────────────────────────────────────────
        var gerenteCH = await _context.Tbpersonals
            .FirstOrDefaultAsync(p => p.Cargo != null &&
                p.Cargo.ToLower().Contains("gerente capital humano"));

        var creador = !string.IsNullOrEmpty(creadorUserName)
            ? await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == creadorUserName ||
                p.CorreoCorporativo == creadorUserName)
            : null;

        var obsTexto = observaciones != null && equipos != null
            ? string.Join("\n", equipos
                .Where(e => observaciones.ContainsKey(e.IdEquipo) &&
                            !string.IsNullOrWhiteSpace(observaciones[e.IdEquipo]))
                .Select(e => $"{e.Dispositivo} ({e.Serie}): {observaciones[e.IdEquipo]}"))
            : "";

        // ── Jefe inmediato: último aprobador según TotalPasos ─────────────
        string jefeInmediato = "";
        string cargoJI = "";

        if (solicitud != null && (solicitud.TotalPasos ?? 0) > 0)
        {
            string? ultimoAprobadorId = solicitud.TotalPasos switch
            {
                3 => solicitud.Paso3Aprobador,
                2 => solicitud.Paso2Aprobador,
                1 => solicitud.Paso1Aprobador,
                _ => solicitud.Paso1Aprobador
            };

            if (!string.IsNullOrEmpty(ultimoAprobadorId))
            {
                Tbpersonal? aprobador = null;

                // 1️⃣ Buscar por UsuarioCorporativo exacto
                aprobador = await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                    p.UsuarioCorporativo == ultimoAprobadorId);

                // 2️⃣ Buscar por CorreoCorporativo exacto
                aprobador ??= await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                    p.CorreoCorporativo == ultimoAprobadorId);

                // 3️⃣ El valor es un CARGO (ej: "Gerente Capital Humano", "Capital Humano")
                //    → buscar por Cargo exacto
                if (aprobador == null)
                {
                    var idLower = ultimoAprobadorId.ToLower();
                    aprobador = await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                        p.Cargo != null && p.Cargo.ToLower() == idLower);
                }

                // 4️⃣ Cargo parcial (ej: "Capital Humano" → busca Gerente/Coord/Asistente CH)
                if (aprobador == null)
                {
                    var idLower = ultimoAprobadorId.ToLower();
                    aprobador = await _context.Tbpersonals
                        .Where(p => p.Cargo != null && p.Area != null &&
                                    p.Area.ToLower().Contains("capital humano"))
                        .OrderBy(p =>
                            p.Cargo!.ToLower().Contains("gerente") ? 0 :
                            p.Cargo!.ToLower().Contains("coordinador") ? 1 : 2)
                        .FirstOrDefaultAsync();
                }

                if (aprobador != null)
                {
                    jefeInmediato = aprobador.NombreColaborador ?? "";
                    cargoJI = aprobador.Cargo ?? "";
                }
            }
        }

        // Fallback: jefe registrado en el perfil
        if (string.IsNullOrEmpty(jefeInmediato) && !string.IsNullOrEmpty(personal.JefeInmediato))
        {
            jefeInmediato = personal.JefeInmediato;
            cargoJI = personal.CargoJefeInmediato ?? "";
        }

        // Fallback final: Gerente de Cap. Humano directamente
        if (string.IsNullOrEmpty(jefeInmediato))
        {
            var gch = await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.Cargo != null && p.Cargo.ToLower().Contains("gerente capital humano"));
            jefeInmediato = gch?.NombreColaborador ?? "";
            cargoJI = gch?.Cargo ?? "";
        }

        // ── Reemplazos de texto con Xceed ─────────────────────────────────
        Reemplazar(doc, "{{FECHA_SOLICITUD}}", solicitud?.FechaSolicitud?.ToString("dd/MM/yyyy") ?? "");
        Reemplazar(doc, "{{FECHA_INICIO}}", solicitud?.FechaInicio?.ToString("dd/MM/yyyy") ?? "");
        Reemplazar(doc, "{{FECHA_FIN}}", solicitud?.FechaFin?.ToString("dd/MM/yyyy") ?? "");
        Reemplazar(doc, "{{TOTAL_DIAS}}", solicitud?.TotalDias?.ToString() ?? "0");
        Reemplazar(doc, "{{DIAS PENDIENTES}}", "0");
        Reemplazar(doc, "{{DIAS_PENDIENTES}}", "0");
        Reemplazar(doc, "{{OBSERVACIONES}}", obsTexto);
        Reemplazar(doc, "{{NOMBRE}}", personal.NombreColaborador ?? "");
        Reemplazar(doc, "{{CC}}", personal.CC.ToString());
        Reemplazar(doc, "{{CARGO}}", personal.Cargo ?? "");
        Reemplazar(doc, "{{AREA}}", personal.Area ?? "");
        Reemplazar(doc, "{{CIUDAD}}", personal.CiudadTrabajo ?? "");
        Reemplazar(doc, "{{GERENCIA}}", personal.Gerencia ?? "");
        Reemplazar(doc, "{{FECHA}}", hoy);
        Reemplazar(doc, "{{FECHA_INGRESO}}", personal.FechaIngreso?.ToString("dd/MM/yyyy") ?? "");
        Reemplazar(doc, "{{JEFE_INMEDIATO}}", jefeInmediato);
        Reemplazar(doc, "{{CARGO_JI}}", cargoJI);
        Reemplazar(doc, "{{PERSONAL_TI}}", creador?.NombreColaborador ?? "");
        Reemplazar(doc, "{{CARGO_PERSONAL_TI}}", creador?.Cargo ?? "");
        Reemplazar(doc, "{{APROBADO_POR}}", aprobadoPor ?? "");

        if (equipos != null && equipos.Any())
        {
            Reemplazar(doc, "{{EQUIPOS}}", string.Join("\n", equipos.Select(e => e.Dispositivo ?? "")));
            Reemplazar(doc, "{{MARCAS}}", string.Join("\n", equipos.Select(e => e.Marca ?? "")));
            Reemplazar(doc, "{{MODELOS}}", string.Join("\n", equipos.Select(e => e.Modelo ?? "")));
            Reemplazar(doc, "{{SERIES}}", string.Join("\n", equipos.Select(e => e.Serie ?? "")));
            Reemplazar(doc, "{{IMEI}}", string.Join("\n", equipos.Select(e => e.Imei ?? "")));
        }
        else
        {
            Reemplazar(doc, "{{EQUIPOS}}", ""); Reemplazar(doc, "{{MARCAS}}", "");
            Reemplazar(doc, "{{MODELOS}}", ""); Reemplazar(doc, "{{SERIES}}", "");
            Reemplazar(doc, "{{IMEI}}", "");
        }

        // Guardar resultado de Xceed en memoria
        using var msXceed = new MemoryStream();
        doc.SaveAs(msXceed);
        byte[] bytesXceed = msXceed.ToArray();

        // ── Inyectar firmas con OpenXML SDK ───────────────────────────────
        string? firmaJiPath = await ObtenerFirmaJefeAsync(solicitud, personal);

        var firmas = new Dictionary<string, string?>
        {
            ["{{FIRMA_USUARIO}}"] = personal.FirmaPath,
            ["{{FIRMA_JI}}"] = firmaJiPath,
            ["{{FIRMA_GERENCIA}}"] = null,
            ["{{FIRMA_CH}}"] = gerenteCH?.FirmaPath,
        };

        return InyectarFirmasOpenXml(bytesXceed, firmas);
    }

    // ── Reemplazar texto con Xceed ────────────────────────────────────────
    private static void Reemplazar(DocX doc, string marcador, string valor)
    {
        doc.ReplaceText(new StringReplaceTextOptions
        {
            SearchValue = marcador,
            NewValue = valor?.Replace("\r\n", "\n").Replace("\r", "\n") ?? "",
            TrackChanges = false,
            EscapeRegEx = true
        });
    }

    // ── Inyectar firmas con OpenXML SDK ───────────────────────────────────
    private static byte[] InyectarFirmasOpenXml(
        byte[] docBytes,
        Dictionary<string, string?> firmasPorMarcador)
    {
        var ms = new MemoryStream();
        ms.Write(docBytes, 0, docBytes.Length);

        using (var wordDoc = WordprocessingDocument.Open(ms, isEditable: true))
        {
            var mainPart = wordDoc.MainDocumentPart!;
            var body = mainPart.Document.Body!;

            uint drawingId = 1;

            foreach (var (marcador, rutaRelativa) in firmasPorMarcador)
            {
                // Buscar el Run que contiene el marcador en párrafos normales y tablas
                var run = EncontrarRun(body, marcador);
                if (run == null)
                {
                    Console.WriteLine($"⚠️ Marcador {marcador} no encontrado");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rutaRelativa))
                {
                    // Sin firma: eliminar el run completo
                    run.Remove();
                    continue;
                }

                var fullPath = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(), "wwwroot",
                    rutaRelativa.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));


                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"⚠️ Archivo no encontrado: {fullPath}");
                    run.Remove();
                    continue;
                }

                try
                {
                    // Determinar tipo de imagen
                    var ext = System.IO.Path.GetExtension(fullPath).ToLower();
                    var imagePartType = ext == ".png"
                        ? ImagePartType.Png
                        : ImagePartType.Jpeg;

                    // Agregar imagen al documento
                    var imagePart = mainPart.AddImagePart(imagePartType);
                    using (var imgStream = File.OpenRead(fullPath))
                        imagePart.FeedData(imgStream);

                    var relationshipId = mainPart.GetIdOfPart(imagePart);

                    // Calcular dimensiones en EMU
                    var (cx, cy) = ObtenerDimensionesEmu(fullPath, anchoMaxPx: 130, altoMaxPx: 65);

                    // Crear el elemento Drawing
                    var drawing = CrearDrawing(relationshipId, drawingId, cx, cy);

                    // Reemplazar el Run con un nuevo Run que contiene el Drawing
                    var nuevoRun = new DocumentFormat.OpenXml.Wordprocessing.Run();

                    // Conservar el formato del run original
                    var rPrOriginal = run.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.RunProperties>();
                    if (rPrOriginal != null)
                        nuevoRun.AppendChild(rPrOriginal.CloneNode(true));

                    nuevoRun.AppendChild(drawing);
                    run.InsertAfterSelf(nuevoRun);
                    run.Remove();

                    drawingId++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error inyectando {marcador}: {ex.Message}");
                    run.Remove();
                }
            }

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    // Busca el Run que contiene el marcador en body completo (párrafos + tablas)
    private static DocumentFormat.OpenXml.Wordprocessing.Run? EncontrarRun(
        Body body, string marcador)
    {
        foreach (var run in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>())
        {
            var texto = run.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Text>();
            if (texto?.Text == marcador)
                return run;
        }
        return null;
    }

    // Construye el elemento Drawing con la imagen inline
    private static Drawing CrearDrawing(
        string relationshipId, uint drawingId, long cx, long cy)
    {
        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = cx, Cy = cy },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = drawingId, Name = $"firma{drawingId}" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties
                                {
                                    Id = drawingId,
                                    Name = $"firma{drawingId}"
                                },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip
                                {
                                    Embed = relationshipId,
                                    CompressionState = A.BlipCompressionValues.Print
                                },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = cx, Cy = cy }),
                                new A.PresetGeometry(new A.AdjustValueList())
                                {
                                    Preset = A.ShapeTypeValues.Rectangle
                                })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            });
    }

    // Calcula EMU respetando proporciones (96 DPI, 1px = 9144 EMU)
    private static (long cx, long cy) ObtenerDimensionesEmu(
        string rutaImagen, int anchoMaxPx, int altoMaxPx)
    {
        const long emuPorPx = 9144L;
        try
        {
            using var img = System.Drawing.Image.FromFile(rutaImagen);
            double ratio = Math.Min(
                (double)anchoMaxPx / img.Width,
                (double)altoMaxPx / img.Height);
            return ((long)(img.Width * ratio * emuPorPx),
                    (long)(img.Height * ratio * emuPorPx));
        }
        catch
        {
            return (anchoMaxPx * emuPorPx, altoMaxPx * emuPorPx);
        }
    }

    private async Task<string?> ObtenerFirmaJefeAsync(
        Tbsolicitude? solicitud, Tbpersonal personal)
    {
        if (solicitud != null && !string.IsNullOrEmpty(solicitud.Paso1Aprobador))
        {
            var jefe = await _context.Tbpersonals.FirstOrDefaultAsync(p =>
                p.UsuarioCorporativo == solicitud.Paso1Aprobador ||
                p.CorreoCorporativo == solicitud.Paso1Aprobador ||
                p.NombreColaborador == solicitud.Paso1Aprobador);
            if (jefe?.FirmaPath != null) return jefe.FirmaPath;
        }
        if (personal.JefeInmediatoCC.HasValue)
        {
            var jefe = await _context.Tbpersonals.FindAsync(personal.JefeInmediatoCC.Value);
            if (jefe?.FirmaPath != null) return jefe.FirmaPath;
        }
        return null;
    }
}