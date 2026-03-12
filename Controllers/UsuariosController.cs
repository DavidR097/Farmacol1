using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Farmacol.Models;

namespace Farmacol.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Farmacol1Context _context;

        public UsuariosController(UserManager<IdentityUser> userManager,
                                   RoleManager<IdentityRole> roleManager,
                                   Farmacol1Context context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var lista = new List<UsuarioViewModel>();

            foreach (var u in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var personal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.CorreoCorporativo == u.Email);

                lista.Add(new UsuarioViewModel
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    Rol = roles.FirstOrDefault() ?? "Sin rol",
                    Nombre = personal?.Nombre ?? "Sin vincular",
                    Cargo = personal?.Cargo ?? ""
                });
            }

            return View(lista);
        }

        public async Task<IActionResult> Crear()
        {
            await CargarViewBagCrear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(int cedula, string password, string rol)
        {
            var personal = await _context.Tbpersonals.FindAsync(cedula);
            if (personal == null)
            {
                ViewBag.Error = "No se encontró el empleado.";
                await CargarViewBagCrear();
                return View();
            }

            if (string.IsNullOrEmpty(personal.CorreoCorporativo))
            {
                ViewBag.Error = "El empleado no tiene correo corporativo registrado. Edítalo primero en Personal.";
                await CargarViewBagCrear();
                return View();
            }

            var email = personal.CorreoCorporativo;
            var userName = !string.IsNullOrEmpty(personal.UsuarioCorporativo)
                           ? personal.UsuarioCorporativo
                           : email;

            var user = new IdentityUser { UserName = userName, Email = email };
            var resultado = await _userManager.CreateAsync(user, password);

            if (!resultado.Succeeded)
            {
                ViewBag.Error = string.Join(", ", resultado.Errors.Select(e => e.Description));
                await CargarViewBagCrear();
                return View();
            }

            await _userManager.AddToRoleAsync(user, rol);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Editar(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var personal = await _context.Tbpersonals
                .FirstOrDefaultAsync(p => p.CorreoCorporativo == user.Email);

            // Empleados sin usuario + el empleado actual vinculado
            var emailsConUsuario = await _userManager.Users
                .Where(u => u.Id != id)
                .Select(u => u.Email)
                .ToListAsync();

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            ViewBag.RolActual = roles.FirstOrDefault() ?? "";
            ViewBag.PersonalDisponible = await _context.Tbpersonals
                .Where(p => !string.IsNullOrEmpty(p.CorreoCorporativo) &&
                            !string.IsNullOrEmpty(p.UsuarioCorporativo) &&
                            (!emailsConUsuario.Contains(p.CorreoCorporativo) ||
                             p.CorreoCorporativo == user.Email))
                .ToListAsync();

            var vm = new UsuarioViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Rol = roles.FirstOrDefault() ?? "",
                Nombre = personal?.Nombre ?? "",
                Cargo = personal?.Cargo ?? "",
                Cedula = personal?.CC ?? 0
            };

            return View(vm);
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Editar(string id, string rol, string nuevoUserName, int? nuevaCedula)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Cambiar username si es diferente
            if (!string.IsNullOrEmpty(nuevoUserName) && nuevoUserName != user.UserName)
            {
                var resultadoUser = await _userManager.SetUserNameAsync(user, nuevoUserName);
                if (!resultadoUser.Succeeded)
                {
                    ViewBag.Error = string.Join(", ", resultadoUser.Errors.Select(e => e.Description));
                    ViewBag.Roles = await _roleManager.Roles.ToListAsync();
                    ViewBag.RolActual = rol;
                    var vm2 = new UsuarioViewModel { Id = user.Id, Email = user.Email ?? "" };
                    return View(vm2);
                }
            }

            // Si se seleccionó un nuevo empleado, actualizar el correo corporativo vinculado
            if (nuevaCedula.HasValue && nuevaCedula.Value > 0)
            {
                var nuevoPersonal = await _context.Tbpersonals.FindAsync(nuevaCedula.Value);
                if (nuevoPersonal != null && !string.IsNullOrEmpty(nuevoPersonal.CorreoCorporativo))
                {
                    // Actualizar email en Identity para que coincida con el nuevo empleado
                    await _userManager.SetEmailAsync(user, nuevoPersonal.CorreoCorporativo);
                }
            }

            // Cambiar rol
            var rolesActuales = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, rolesActuales);
            await _userManager.AddToRoleAsync(user, rol);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CambiarPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.UserId = id;
            ViewBag.Email = user.Email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CambiarPassword(string id, string nuevaPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resultado = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);

            if (resultado.Succeeded)
                return RedirectToAction(nameof(Index));

            ViewBag.Error = string.Join(", ", resultado.Errors.Select(e => e.Description));
            ViewBag.UserId = id;
            ViewBag.Email = user.Email;
            return View();
        }

        private async Task CargarViewBagCrear()
        {
            var emailsConUsuario = await _userManager.Users
                .Select(u => u.Email)
                .ToListAsync();

            ViewBag.Personal = await _context.Tbpersonals
                .Where(p => !string.IsNullOrEmpty(p.CorreoCorporativo) &&
                            !emailsConUsuario.Contains(p.CorreoCorporativo))
                .ToListAsync();

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
        }
    }
}