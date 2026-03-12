using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Farmacol.Models;

namespace Farmacol.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public LoginController(SignInManager<IdentityUser> signInManager,
                               UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            // Buscar usuario por email si lo que ingresaron parece un correo
            string userName = email;
            if (email.Contains("@"))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                    userName = user.UserName ?? email;
            }

            var resultado = await _signInManager.PasswordSignInAsync(userName, password, false, false);

            if (resultado.Succeeded)
                return RedirectToAction("Index", "Home");

            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        public async Task<IActionResult> Salir()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }

        public IActionResult Denegado()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }
    }
}