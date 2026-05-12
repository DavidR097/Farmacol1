using Farmacol.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Filters
{
    public class UserAreaFilter : IAsyncActionFilter
    {
        private readonly Farmacol1Context _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserAreaFilter(Farmacol1Context context,
                               UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context,
                                                  ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller &&
                controller.User.Identity?.IsAuthenticated == true)
            {
                var userName = controller.User.Identity.Name ?? "";
                Tbpersonal? personal = null;

                if (!string.IsNullOrWhiteSpace(userName))
                {
                    personal = await _context.Tbpersonals
                        .FirstOrDefaultAsync(p => p.UsuarioCorporativo == userName);
                }

                if (personal == null && int.TryParse(userName, out int cc))
                {
                    personal = await _context.Tbpersonals
                        .FirstOrDefaultAsync(p => p.CC == cc);
                }

                if (personal == null)
                {
                    var identityUser = await _userManager.FindByNameAsync(userName);
                    if (!string.IsNullOrWhiteSpace(identityUser?.Email))
                    {
                        personal = await _context.Tbpersonals
                            .FirstOrDefaultAsync(p => p.CorreoCorporativo == identityUser.Email);
                    }
                }

                controller.ViewBag.UserArea = personal?.Area ?? "";
                controller.ViewBag.UserCargo = personal?.Cargo ?? "";
                controller.ViewBag.UserNombre = personal?.NombreColaborador ?? "";
                controller.ViewBag.UserCC = personal?.CC;
            }

            await next();
        }
    }
}