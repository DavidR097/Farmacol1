using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Farmacol.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly Farmacol1Context _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AnuncioService _anuncioService;
        private readonly IWebHostEnvironment _env;

        public HomeController(
            Farmacol1Context context,
            UserManager<IdentityUser> userManager,
            AnuncioService anuncioService,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _anuncioService = anuncioService;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            // 1. ANUNCIOS Y FILTRAR LATERALES 
            var anuncios = await _anuncioService.ObtenerAnunciosActivosAsync();
            var laterales = anuncios.Where(a => EsImagenVertical(a.Imagen)).ToList();

            ViewBag.Anuncios = anuncios;
            ViewBag.LateralAnuncios = laterales;

            // 2. Personal Activo, Solicitudes, Vacaciones
            ViewBag.PersonalActivo = await _context.Tbpersonals.CountAsync();
            ViewBag.SoliPendientes = await _context.Tbsolicitudes.CountAsync(s => s.Estado == "En proceso");
            ViewBag.SoliAprobadas = await _context.Tbsolicitudes.CountAsync(s => s.Estado == "Aprobada");

            DateOnly fechaHoy = DateOnly.FromDateTime(DateTime.Now);

            // Personas en vacaciones 
            ViewBag.VacacionesActivas = await _context.Tbsolicitudes.CountAsync(s =>
                s.TipoSolicitud == "Vacaciones" &&
                s.Estado == "Aprobada" &&
                s.FechaInicio != null && s.FechaFin != null &&
                fechaHoy >= s.FechaInicio && fechaHoy <= s.FechaFin);

            // 3. ESTADÍSTICAS POR ROL
            if (User.IsInRole("Administrador"))
            {
                // Usuarios bloqueados 
                ViewBag.UsersBloqueados = await _userManager.Users
                    .CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now);

                ViewBag.InhabilitacionesActivas = await _context.TbDelegaciones
                    .CountAsync(d => d.Activa);
            }

            if (User.IsInRole("RRHH"))
            {
                // Tasa de revisión de solicitudes
                double total = await _context.Tbsolicitudes.CountAsync();
                double revisadas = await _context.Tbsolicitudes.CountAsync(s => s.Estado == "Aprobada");
                double tasa = total > 0 ? Math.Round((revisadas / total) * 100) : 0;
                ViewBag.TasaRevision = tasa;

                string ColorClase = "text-primary";

                if (tasa >= 70)
                {
                    ColorClase = "text-success";
                }
                else if (tasa > 30)
                {
                    ColorClase = "text-warning";
                }
                else
                {
                    ColorClase = "text-danger";
                }
                ViewBag.ColorClase = ColorClase;
            }

            return View();
        }

        private bool EsImagenVertical(string? rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa)) return false;

            try
            {
                var rutaFisica = Path.Combine(_env.WebRootPath ?? "wwwroot", rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(rutaFisica)) return false;

                var dims = GetImageDimensions(rutaFisica);
                return dims.HasValue && dims.Value.height > dims.Value.width;
            }
            catch { return false; }
        }

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
                    return ((wBytes[0] << 24) | (wBytes[1] << 16) | (wBytes[2] << 8) | wBytes[3],
                            (hBytes[0] << 24) | (hBytes[1] << 16) | (hBytes[2] << 8) | hBytes[3]);
                }
                // GIF
                if (sig.Length >= 3 && sig[0] == 0x47 && sig[1] == 0x49 && sig[2] == 0x46)
                {
                    fs.Seek(6, SeekOrigin.Begin);
                    return (br.ReadUInt16(), br.ReadUInt16());
                }
                // JPEG
                fs.Seek(0, SeekOrigin.Begin);
                if (br.ReadByte() == 0xFF && br.ReadByte() == 0xD8)
                {
                    while (fs.Position < fs.Length)
                    {
                        if (br.ReadByte() != 0xFF) continue;
                        byte marker = br.ReadByte();
                        while (marker == 0xFF) marker = br.ReadByte();

                        if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                        {
                            fs.Seek(3, SeekOrigin.Current); // Skip len (2) and precision (1)
                            int h = (br.ReadByte() << 8) | br.ReadByte();
                            int w = (br.ReadByte() << 8) | br.ReadByte();
                            return (w, h);
                        }
                        else
                        {
                            int len = (br.ReadByte() << 8) | br.ReadByte();
                            if (len < 2) break;
                            fs.Seek(len - 2, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}