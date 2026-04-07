using Microsoft.AspNetCore.Mvc;

namespace Farmacol.Controllers;

public class InhabilitadoController : Controller
{
    public IActionResult Index(string? hasta, string? motivo)
    {
        ViewBag.Hasta = hasta ?? "pronto";
        ViewBag.Motivo = motivo ?? "";
        return View();
    }
}
