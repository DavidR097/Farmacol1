using Farmacol.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.ViewComponents
{
    public class UserAvatarViewComponent : ViewComponent
    {
        private readonly Farmacol1Context _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserAvatarViewComponent(Farmacol1Context context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new AvatarModel();
            var userName = User.Identity?.Name ?? string.Empty;
            if (!string.IsNullOrEmpty(userName))
            {
                var user = await _userManager.FindByNameAsync(userName);
                var email = user?.Email ?? string.Empty;
                var personal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p => p.CorreoCorporativo == userName
                                           || p.UsuarioCorporativo == userName
                                           || p.CorreoCorporativo == email
                                           || p.UsuarioCorporativo == email);
                if (personal != null)
                {
                    model.Foto = personal.FotoPerfil;
                    model.Nombre = personal.NombreColaborador ?? userName;
                }
                else
                {
                    model.Nombre = userName;
                }
            }
            return View(model);
        }

        public class AvatarModel
        {
            public string? Foto { get; set; }
            public string? Nombre { get; set; }
        }
    }
}
