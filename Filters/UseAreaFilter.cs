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

                // Resolver email real desde Identity
                var identityUser = await _userManager.FindByNameAsync(userName);
                var email = identityUser?.Email ?? "";

                var personal = await _context.Tbpersonals
                    .Where(p => p.CorreoCorporativo == userName
                             || p.UsuarioCorporativo == userName
                             || p.CorreoCorporativo == email
                             || p.UsuarioCorporativo == email)
                    .Select(p => new
                    {
                        p.Area,
                        p.Cargo,
                        p.NombreColaborador,
                        p.CC
                    })
                    .FirstOrDefaultAsync();

                controller.ViewBag.UserArea = personal?.Area ?? "";
                controller.ViewBag.UserCargo = personal?.Cargo ?? "";
                controller.ViewBag.UserNombre = personal?.NombreColaborador ?? "";
                controller.ViewBag.UserCC = personal?.CC;
            }
            await next();
        }
    }
}