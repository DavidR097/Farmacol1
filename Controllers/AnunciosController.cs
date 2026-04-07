using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador, RRHH")]
    public class AnunciosController : Controller
    {
        private readonly AnuncioService _anuncioService;
        private readonly Farmacol1Context _context;

        public AnunciosController(AnuncioService anuncioService, Farmacol1Context context)
        {
            _anuncioService = anuncioService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var anuncios = await _anuncioService.ObtenerTodosAsync();
            return View(anuncios);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TbAnuncio anuncio, IFormFile? imagen)
        {
            if (!ModelState.IsValid) return View(anuncio);

            var creadoPor = User.Identity?.Name ?? "Sistema";
            if (imagen != null)
            {
                try
                {
                    var (path, w, h) = await _anuncioService.GuardarImagenAsync(imagen);
                    anuncio.Imagen = path;
                    anuncio.Width = w;
                    anuncio.Height = h;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("imagen", ex.Message);
                    return View(anuncio);
                }
            }

            await _anuncioService.CrearAsync(anuncio, creadoPor);

            TempData["Exito"] = "Anuncio creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var anuncio = await _context.TbAnuncios.FindAsync(id);
            if (anuncio == null) return NotFound();
            return View(anuncio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TbAnuncio anuncio, IFormFile? imagen)
        {
            if (!ModelState.IsValid) return View(anuncio);
            if (imagen != null)
            {
                try
                {
                    var (nueva, w, h) = await _anuncioService.GuardarImagenAsync(imagen);
                    if (!string.IsNullOrEmpty(nueva))
                    {
                        anuncio.Imagen = nueva;
                        anuncio.Width = w;
                        anuncio.Height = h;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("imagen", ex.Message);
                    return View(anuncio);
                }
            }

            await _anuncioService.ActualizarAsync(anuncio);
            TempData["Exito"] = "Anuncio actualizado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var anuncio = await _context.TbAnuncios.FindAsync(id);
            if (anuncio != null)
            {
                anuncio.Activo = !anuncio.Activo;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _anuncioService.EliminarAsync(id);
            TempData["Exito"] = "Anuncio eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}
