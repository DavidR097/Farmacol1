using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Controllers
{
    // Administrador tiene acceso a todo
    // TI solo tiene acceso a Desbloquear (puede ver la lista de usuarios pero no acciones sensibles)
    [Authorize(Roles = "Administrador,TI")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Farmacol1Context _context;
        private readonly AuditService _audit;

        public UsuariosController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Farmacol1Context context,
            AuditService audit)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _audit = audit;
        }

        // Verifica que solo Admin puede hacer acciones sensibles
        private IActionResult? SoloAdmin()
        {
            if (!User.IsInRole("Administrador"))
            {
                TempData["Error"] = "No tienes permiso para esta acción.";
                return RedirectToAction(nameof(Index));
            }
            return null;
        }

        // ── INDEX ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();

            var personal = await _context.Tbpersonals
                .Select(p => new {
                    p.CC,
                    p.NombreColaborador,
                    p.Cargo,
                    p.Area,
                    p.UsuarioCorporativo,
                    p.CorreoCorporativo
                })
                .ToListAsync();

            var lista = new List<UsuarioViewModel>();

            foreach (var u in users)
            {
                // Buscar por UsuarioCorporativo primero (más confiable), luego por email
                var p = personal.FirstOrDefault(x =>
                    x.UsuarioCorporativo == u.UserName)
                    ?? personal.FirstOrDefault(x => x.CorreoCorporativo == u.Email);

                lista.Add(new UsuarioViewModel
                {
                    Id = u.Id,
                    Email = u.UserName ?? u.Email ?? "",
                    Rol = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? "Sin rol",
                    Nombre = p?.NombreColaborador ?? u.UserName ?? "Sin nombre",
                    Cargo = p?.Cargo ?? "",
                    Area = p?.Area,
                    Cedula = p?.CC ?? 0,
                    Bloqueado = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                });
            }

            ViewBag.EsAdmin = User.IsInRole("Administrador");
            return View(lista.OrderBy(u => u.Nombre).ThenBy(u => u.Email).ToList());
        }

        // ── CREAR ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            if (SoloAdmin() is { } r) return r;

            // 1. Obtener usernames que YA existen en Identity
            var usuariosExistentes = await _userManager.Users
                .Select(u => u.UserName!)
                .ToListAsync();

            // 2. Traer empleados que aún no tienen usuario (sin GroupBy problemático)
            var personalListo = await _context.Tbpersonals
                .Where(p => !string.IsNullOrEmpty(p.UsuarioCorporativo))
                .Where(p => !usuariosExistentes.Contains(p.UsuarioCorporativo))
                .OrderBy(p => p.NombreColaborador)
                .Select(p => new
                {
                    p.CC,
                    Nombre = p.NombreColaborador,
                    p.Cargo,
                    p.UsuarioCorporativo
                })
                .ToListAsync();

            // 3. (Opcional pero recomendado) Eliminar duplicados por CC en memoria (por si hay repetidos en la tabla)
            var personalDistinct = personalListo
                .GroupBy(x => x.CC)
                .Select(g => g.First())
                .ToList();

            ViewBag.Roles = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync();

            ViewBag.Personal = personalDistinct;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int cedula, string password, string rol)
        {
            if (SoloAdmin() is { } r) return r;

            var personal = await _context.Tbpersonals.FindAsync(cedula);
            if (personal == null) { TempData["Error"] = "Empleado no encontrado."; return RedirectToAction(nameof(Crear)); }
            if (string.IsNullOrWhiteSpace(personal.UsuarioCorporativo))
            { TempData["Error"] = "El empleado no tiene usuario corporativo."; return RedirectToAction(nameof(Crear)); }

            var user = new IdentityUser
            {
                UserName = personal.UsuarioCorporativo,
                Email = string.IsNullOrWhiteSpace(personal.CorreoCorporativo) ? null : personal.CorreoCorporativo
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            { TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description)); return RedirectToAction(nameof(Crear)); }

            if (!string.IsNullOrEmpty(rol)) await _userManager.AddToRoleAsync(user, rol);
            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_USUARIOS, AuditService.ACC_CREAR,
                $"Usuario '{personal.UsuarioCorporativo}' creado con rol '{rol}'");
            }
            catch { }

            TempData["Exito"] = $"✅ Usuario '{personal.UsuarioCorporativo}' creado.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDITAR ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            if (SoloAdmin() is { } r) return r;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var rolActual = roles.FirstOrDefault() ?? "";

            // Obtener empleados disponibles (los que NO tienen usuario aún)
            var usuariosExistentes = await _userManager.Users
                .Select(u => u.UserName!)
                .ToListAsync();

            var personalDisponible = await _context.Tbpersonals
                .Where(p => !string.IsNullOrEmpty(p.UsuarioCorporativo))
                .Where(p => !usuariosExistentes.Contains(p.UsuarioCorporativo))
                .OrderBy(p => p.NombreColaborador)
                .Select(p => new
                {
                    p.CC,
                    Nombre = p.NombreColaborador,
                    p.Cargo,
                    p.CorreoCorporativo
                })
                .ToListAsync();

            // Eliminar duplicados por CC en memoria (por seguridad)
            var personalDistinct = personalDisponible
                .GroupBy(x => x.CC)
                .Select(g => g.First())
                .ToList();

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            ViewBag.RolActual = rolActual;
            ViewBag.PersonalDisponible = personalDistinct;   // ← Aquí está la clave

            var pActual = await _context.Tbpersonals.FirstOrDefaultAsync(x =>
                x.CorreoCorporativo == user.Email || x.UsuarioCorporativo == user.UserName);

            return View(new UsuarioViewModel
            {
                Id = user.Id,
                Email = user.UserName ?? user.Email ?? "",
                Rol = rolActual,
                Nombre = pActual?.NombreColaborador ?? "",
                Cargo = pActual?.Cargo ?? "",
                Area = pActual?.Area,
                Cedula = pActual?.CC ?? 0,
                Bloqueado = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string id, string nuevoUserName, string? nuevoEmail, string rol, int? nuevaCedula)
        {
            if (SoloAdmin() is { } r) return r;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Actualizar UserName y Email de forma segura
            if (!string.IsNullOrWhiteSpace(nuevoUserName) && nuevoUserName != user.UserName)
            {
                var existing = await _userManager.FindByNameAsync(nuevoUserName);
                if (existing != null && existing.Id != user.Id)
                {
                    TempData["Error"] = "El nombre de usuario ya está en uso.";
                    return RedirectToAction(nameof(Editar), new { id });
                }
                await _userManager.SetUserNameAsync(user, nuevoUserName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(nuevoEmail) && nuevoEmail != user.Email)
            {
                await _userManager.SetEmailAsync(user, nuevoEmail.Trim());
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["Error"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Editar), new { id });
            }

            // Actualizar Rol
            var rolesActuales = await _userManager.GetRolesAsync(user);
            if (rolesActuales.Any())
                await _userManager.RemoveFromRolesAsync(user, rolesActuales);

            if (!string.IsNullOrEmpty(rol))
                await _userManager.AddToRoleAsync(user, rol);

            // === ACTUALIZAR VÍNCULO CON TBPERSONAL (lo más importante) ===
            if (nuevaCedula.HasValue && nuevaCedula.Value > 0)
            {
                var personal = await _context.Tbpersonals.FindAsync(nuevaCedula.Value);
                if (personal != null)
                {
                    personal.UsuarioCorporativo = user.UserName;
                    personal.CorreoCorporativo = user.Email;
                    await _context.SaveChangesAsync();
                }
            }

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_USUARIOS, AuditService.ACC_EDITAR,
                    $"Usuario '{user.UserName}' editado. Rol: '{rol}'", id);
            }
            catch { }

            TempData["Exito"] = "✅ Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ── CAMBIAR CONTRASEÑA ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> CambiarPassword(string id)
        {
            if (SoloAdmin() is { } r) return r;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.UserName = user.UserName;
            ViewBag.UserId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(string id, string nuevaPassword)
        {
            if (SoloAdmin() is { } r) return r;

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(CambiarPassword), new { id });
            }

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_USUARIOS, "Cambiar contraseña",
                $"Contraseña cambiada para '{user.UserName}' por {User.Identity?.Name}", id);
            }
            catch { }

            TempData["Exito"] = "✅ Contraseña actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // ── DESBLOQUEAR ← TI y Admin pueden hacer esto ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,TI")]
        public async Task<IActionResult> Desbloquear(string id)
        {
            // No tiene restricción SoloAdmin — TI también puede desbloquear
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_USUARIOS, AuditService.ACC_DESBLOQUEAR,
                $"Usuario '{user.UserName}' desbloqueado por {User.Identity?.Name}", id);
            }
            catch { }

            TempData["Exito"] = $"✅ Usuario '{user.UserName}' desbloqueado.";
            return RedirectToAction(nameof(Index));
        }

        // ── ELIMINAR ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(string id)
        {
            if (SoloAdmin() is { } r) return r;

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                try
                {
                    await _audit.RegistrarAsync(AuditService.MOD_USUARIOS, AuditService.ACC_ELIMINAR,
                    $"Usuario '{user.UserName}' eliminado por {User.Identity?.Name}", id);
                }
                catch { }
                await _userManager.DeleteAsync(user);
            }
            TempData["Exito"] = "Usuario eliminado.";
            return RedirectToAction(nameof(Index));
        }
        
        // ── VISTA SOLO PARA TI: ver bloqueados y desbloquear ─────────────
        [HttpGet]
        [Authorize(Roles = "TI,Administrador")]
        public async Task<IActionResult> Bloqueados()
        {
            var users = await _userManager.Users.ToListAsync();

            var personal = await _context.Tbpersonals
                .Select(p => new {
                    p.CC,
                    p.NombreColaborador,
                    p.Cargo,
                    p.Area,
                    p.UsuarioCorporativo,
                    p.CorreoCorporativo
                })
                .ToListAsync();

            var lista = new List<UsuarioViewModel>();

            foreach (var u in users)
            {
                // Solo usuarios bloqueados
                if (!(u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow))
                    continue;

                var p = personal.FirstOrDefault(x => x.UsuarioCorporativo == u.UserName)
                     ?? personal.FirstOrDefault(x => x.CorreoCorporativo == u.Email);

                lista.Add(new UsuarioViewModel
                {
                    Id = u.Id,
                    Email = u.UserName ?? u.Email ?? "",
                    Rol = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? "Sin rol",
                    Nombre = p?.NombreColaborador ?? u.UserName ?? "Sin nombre",
                    Cargo = p?.Cargo ?? "",
                    Area = p?.Area,
                    Cedula = p?.CC ?? 0,
                    Bloqueado = true,
                });
            }

            ViewBag.EsAdmin = User.IsInRole("Administrador");
            return View(lista.OrderBy(u => u.Nombre).ToList());
        }
    }
}
