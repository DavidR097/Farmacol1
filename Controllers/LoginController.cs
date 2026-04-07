using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Farmacol.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _audit;

        public LoginController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            AuditService audit)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _audit = audit;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Index(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Ingresa tu usuario y contraseña.";
                return View();
            }

            var user = await _userManager.FindByNameAsync(usuario);

            if (user == null)
            {
                try
                {
                    await _audit.RegistrarAsync(AuditService.MOD_SESION,
                    AuditService.ACC_LOGIN_FAIL, $"Usuario no encontrado: {usuario}");
                }
                catch { }
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                var hasta = user.LockoutEnd?.ToLocalTime().ToString("HH:mm") ?? "más tarde";
                ViewBag.Error = $"Tu cuenta está bloqueada. Intenta a las {hasta} o contacta al administrador.";
                return View();
            }

            var resultado = await _signInManager.PasswordSignInAsync(
                usuario, password, isPersistent: false, lockoutOnFailure: true);

            // Registrar estado detallado para depuración
            try
            {
                var lockout = user.LockoutEnd?.ToLocalTime().ToString("o") ?? "-";
                var failed = await _userManager.GetAccessFailedCountAsync(user);
                await _audit.RegistrarAsync(AuditService.MOD_SESION, AuditService.ACC_LOGIN,
                    $"Intento login: {usuario}. LockoutEnd={lockout}. FailedCount={failed}");
            }
            catch { }

            if (resultado.Succeeded)
            {
                try
                {
                    await _audit.RegistrarAsync(AuditService.MOD_SESION,
                    AuditService.ACC_LOGIN, $"Login exitoso: {usuario}");
                }
                catch { }
                return RedirectToAction("Index", "Home");
            }

            if (resultado.IsLockedOut)
            {
                try
                {
                    await _audit.RegistrarAsync(AuditService.MOD_SESION,
                    AuditService.ACC_LOGIN_FAIL,
                    $"Cuenta bloqueada tras 3 intentos: {usuario}");
                }
                catch { }
                ViewBag.Error = "Tu cuenta ha sido bloqueada por 3 intentos fallidos. Contacta al administrador.";
                return View();
            }

            if (resultado.IsNotAllowed)
            {
                ViewBag.Error = "Cuenta no permitida. Verifica que tu usuario esté activo o confirmado.";
                return View();
            }

            if (resultado.RequiresTwoFactor)
            {
                ViewBag.Error = "Autenticación de dos factores requerida.";
                return View();
            }

            var intentosFallidos = await _userManager.GetAccessFailedCountAsync(user);
            var restantes = 3 - intentosFallidos;
            try { await _audit.RegistrarAsync(AuditService.MOD_SESION, AuditService.ACC_LOGIN_FAIL, $"Contraseña incorrecta para {usuario}. Intentos restantes: {restantes}"); } catch { }

            ViewBag.Error = restantes > 0
                ? $"Usuario o contraseña incorrectos. Te quedan {restantes} intento(s)."
                : "Tu cuenta ha sido bloqueada. Contacta al administrador.";
            return View();
        }

        public async Task<IActionResult> Salir()
        {
            var nombre = User.Identity?.Name ?? "";
            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SESION,
                AuditService.ACC_LOGOUT, $"Cierre de sesión: {nombre}");
            }
            catch { }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var nombre = User.Identity?.Name ?? "";
            try
            {
                await _audit.RegistrarAsync(AuditService.MOD_SESION,
                AuditService.ACC_LOGOUT, $"Cierre de sesión: {nombre}");
            }
            catch { }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }

        public IActionResult Denegado() => View();
    }
}
