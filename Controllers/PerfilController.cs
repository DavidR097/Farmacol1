using System;
using System.IO;
using System.Threading.Tasks;
using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Farmacol1Context _context;
        private readonly VacacionesService _vacacionesService;

        public PerfilController(UserManager<IdentityUser> userManager,
                                Farmacol1Context context,
                                VacacionesService vacacionesService)
        {
            _userManager = userManager;
            _context = context;
            _vacacionesService = vacacionesService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirFirma(IFormFile firma)
        {
            if (firma == null || firma.Length == 0)
            {
                TempData["Error"] = "Selecciona una imagen de firma válida.";
                return RedirectToAction(nameof(Index));
            }

            var personal = await BuscarPersonalActualAsync();
            if (personal == null)
            {
                TempData["Error"] = "Error de sesión. Inicia sesión nuevamente.";
                return RedirectToAction("Login", "Login");
            }

            var ext = Path.GetExtension(firma.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            {
                TempData["Error"] = "Solo se permiten archivos PNG o JPG para la firma.";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar firma anterior si existe
            if (!string.IsNullOrEmpty(personal.FirmaPath))
            {
                var rutaAnterior = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    personal.FirmaPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaAnterior))
                {
                    try { System.IO.File.Delete(rutaAnterior); } catch { }
                }
            }

            // Guardar nueva firma en wwwroot/firmas
            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "firmas");
            Directory.CreateDirectory(carpeta);
            var nombreArchivo = $"firma_{personal.CC}_{DateTime.Now.Ticks}{ext}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);
            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await firma.CopyToAsync(stream);
            }

            personal.FirmaPath = $"/firmas/{nombreArchivo}";
            await _context.SaveChangesAsync();

            TempData["Exito"] = "✅ Firma subida correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index()
        {
            var personal = await BuscarPersonalActualAsync();
            if (personal == null)
            {
                TempData["Error"] = "Perfil no encontrado.";
                return RedirectToAction("Index", "Home");
            }

            var vacaciones = await _vacacionesService.CalcularVacacionesAsync(personal.CC);

            var model = new PerfilViewModel
            {
                Personal = personal,
                Vacaciones = vacaciones,
                FotoPerfil = personal.FotoPerfil
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirFoto(IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
            {
                TempData["Error"] = "Selecciona una imagen válida.";
                return RedirectToAction(nameof(Index));
            }

            var personal = await BuscarPersonalActualAsync();
            if (personal == null)
            {
                TempData["Error"] = "Error de sesión. Inicia sesión nuevamente.";
                return RedirectToAction("Login", "Login");
            }

            // Eliminar foto anterior
            if (!string.IsNullOrEmpty(personal.FotoPerfil))
            {
                var rutaAnterior = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    personal.FotoPerfil.TrimStart('/'));

                if (System.IO.File.Exists(rutaAnterior))
                {
                    try { System.IO.File.Delete(rutaAnterior); } catch { }
                }
            }

            // Guardar nueva foto
            var carpeta = Path.Combine("wwwroot", "fotos-perfil");
            Directory.CreateDirectory(carpeta);

            var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
            var nombreArchivo = $"{personal.CC}_{DateTime.Now.Ticks}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            personal.FotoPerfil = $"/fotos-perfil/{nombreArchivo}";
            await _context.SaveChangesAsync();

            TempData["Exito"] = "✅ Foto de perfil actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Helper reutilizable
        // ✅ CORREGIDO — mismo patrón que el filtro
        private async Task<Tbpersonal?> BuscarPersonalActualAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var userName = user.UserName ?? "";

            // 1️⃣ Por UsuarioCorporativo exacto
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var p = await _context.Tbpersonals
                    .FirstOrDefaultAsync(x => x.UsuarioCorporativo == userName);
                if (p != null) return p;
            }

            // 2️⃣ Por CC si el username es numérico
            if (int.TryParse(userName, out int cc))
            {
                var p = await _context.Tbpersonals
                    .FirstOrDefaultAsync(x => x.CC == cc);
                if (p != null) return p;
            }

            // 3️⃣ Por correo solo si no está vacío
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return await _context.Tbpersonals
                    .FirstOrDefaultAsync(x => x.CorreoCorporativo == user.Email);
            }

            return null;
        }


    }
}