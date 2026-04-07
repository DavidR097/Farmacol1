using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Farmacol.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AnuncioService _anuncioService;
        private readonly IWebHostEnvironment _env;

        public HomeController(AnuncioService anuncioService, IWebHostEnvironment env)
        {
            _anuncioService = anuncioService;
            _env = env;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Index()
        {
            var anuncios = await _anuncioService.ObtenerAnunciosActivosAsync();

            // Filtrar en servidor imágenes verticales para laterales (aceptar más formatos que solo 555x774)
            // Consideramos imagen vertical si la altura es mayor que el ancho.
            var laterales = new List<TbAnuncio>();
            foreach (var a in anuncios)
            {
                if (string.IsNullOrWhiteSpace(a.Imagen)) continue;
                try
                {
                    var ruta = Path.Combine(_env.WebRootPath ?? "wwwroot", a.Imagen.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (!System.IO.File.Exists(ruta)) continue;
                    var dims = GetImageDimensions(ruta);
                    if (dims.HasValue && dims.Value.height > dims.Value.width)
                    {
                        laterales.Add(a);
                    }
                }
                catch { /* ignorar imágenes inválidas */ }
            }

            ViewBag.Anuncios = anuncios;
            ViewBag.LateralAnuncios = laterales;
            return View();
        }

        // Leer dimensiones básicas sin depender de System.Drawing
        private (int width, int height)? GetImageDimensions(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);
                var sig = br.ReadBytes(8);
                // PNG
                if (sig.Length >= 8 && sig[0] == 0x89 && sig[1] == 0x50 && sig[2] == 0x4E && sig[3] == 0x47)
                {
                    fs.Seek(16, SeekOrigin.Begin);
                    var wBytes = br.ReadBytes(4);
                    var hBytes = br.ReadBytes(4);
                    int w = (wBytes[0] << 24) | (wBytes[1] << 16) | (wBytes[2] << 8) | wBytes[3];
                    int h = (hBytes[0] << 24) | (hBytes[1] << 16) | (hBytes[2] << 8) | hBytes[3];
                    return (w, h);
                }
                // GIF
                if (sig.Length >= 3 && sig[0] == 0x47 && sig[1] == 0x49 && sig[2] == 0x46)
                {
                    fs.Seek(6, SeekOrigin.Begin);
                    ushort w = br.ReadUInt16();
                    ushort h = br.ReadUInt16();
                    return (w, h);
                }
                // JPEG: parse markers
                fs.Seek(0, SeekOrigin.Begin);
                if (br.ReadByte() == 0xFF && br.ReadByte() == 0xD8)
                {
                    while (fs.Position < fs.Length)
                    {
                        byte markerStart = br.ReadByte();
                        if (markerStart != 0xFF) continue;
                        byte marker = br.ReadByte();
                        while (marker == 0xFF) marker = br.ReadByte();
                        // SOF markers range (we check common SOF types)
                        if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2 || marker == 0xC3 || marker == 0xC5 || marker == 0xC6 || marker == 0xC7 || marker == 0xC9 || marker == 0xCA || marker == 0xCB || marker == 0xCD || marker == 0xCE || marker == 0xCF)
                        {
                            var lenBytes = br.ReadBytes(2);
                            int len = (lenBytes[0] << 8) | lenBytes[1];
                            // precision
                            br.ReadByte();
                            var hBytes = br.ReadBytes(2);
                            var wBytes = br.ReadBytes(2);
                            int h = (hBytes[0] << 8) | hBytes[1];
                            int w = (wBytes[0] << 8) | wBytes[1];
                            return (w, h);
                        }
                        else
                        {
                            var lenBytes = br.ReadBytes(2);
                            if (lenBytes.Length < 2) break;
                            int len = (lenBytes[0] << 8) | lenBytes[1];
                            if (len < 2) break;
                            fs.Seek(len - 2, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

    }
}
