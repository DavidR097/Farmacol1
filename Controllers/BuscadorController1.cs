using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Farmacol.Models;

namespace Farmacol.Controllers
{
    [Authorize]
    public class BuscadorController : Controller
    {
        private readonly Farmacol1Context _context;

        public BuscadorController(Farmacol1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string termino)
        {
            if (string.IsNullOrEmpty(termino))
                return View(new BuscadorViewModel());

            bool esAdmin = User.IsInRole("Administrador");
            bool esRRHH = User.IsInRole("RRHH");
            bool esTI = User.IsInRole("TI");
            bool esUsuario = User.IsInRole("Usuario");

            var modelo = new BuscadorViewModel { Termino = termino };

            if (esAdmin || esTI)
            {
                modelo.Inventario = await _context.Tbinventarios.Where(i =>
                    (i.Ubicación != null && i.Ubicación.Contains(termino)) ||
                    (i.Dispositivo != null && i.Dispositivo.Contains(termino)) ||
                    (i.Marca != null && i.Marca.Contains(termino)) ||
                    (i.Serie != null && i.Serie.Contains(termino))
                ).ToListAsync();

                modelo.Responsivas = await _context.Tbresponsivas.Where(r =>
                    (r.Equipo != null && r.Equipo.Contains(termino)) ||
                    (r.Marca != null && r.Marca.Contains(termino)) ||
                    (r.Serie != null && r.Serie.Contains(termino))
                ).ToListAsync();
            }

            if (esAdmin || esRRHH || esTI)
            {
                // ← NombreColaborador, no Nombre (que es [NotMapped])
                modelo.Personal = await _context.Tbpersonals.Where(p =>
                    (p.NombreColaborador != null && p.NombreColaborador.Contains(termino)) ||
                    (p.Cargo != null && p.Cargo.Contains(termino))
                ).ToListAsync();

                // Búsqueda por CC separada (int no se puede usar Contains)
                if (int.TryParse(termino, out int ccBusq))
                {
                    var porCC = await _context.Tbpersonals
                        .Where(p => p.CC == ccBusq)
                        .ToListAsync();
                    modelo.Personal = modelo.Personal
                        .Union(porCC)
                        .DistinctBy(p => p.CC)
                        .ToList();
                }
            }

            if (esAdmin || esRRHH)
            {
                modelo.Solicitudes = await _context.Tbsolicitudes.Where(s =>
                    (s.Nombre != null && s.Nombre.Contains(termino)) ||
                    (s.TipoSolicitud != null && s.TipoSolicitud.Contains(termino)) ||
                    (s.SubtipoPermiso != null && s.SubtipoPermiso.Contains(termino)) ||
                    (s.Estado != null && s.Estado.Contains(termino))
                ).ToListAsync();

                if (int.TryParse(termino, out int ccSoli))
                {
                    var porCC = await _context.Tbsolicitudes
                        .Where(s => s.CC == ccSoli).ToListAsync();
                    modelo.Solicitudes = modelo.Solicitudes
                        .Union(porCC)
                        .DistinctBy(s => s.IdSolicitud)
                        .ToList();
                }

                modelo.SolicitudesRechazadas = await _context.TbsoliRechazada.Where(r =>
                    (r.Nombre != null && r.Nombre.Contains(termino)) ||
                    (r.TipoSolicitud != null && r.TipoSolicitud.Contains(termino))
                ).ToListAsync();

                if (int.TryParse(termino, out int ccRech))
                {
                    var porCC = await _context.TbsoliRechazada
                        .Where(r => r.CC == ccRech).ToListAsync();
                    modelo.SolicitudesRechazadas = modelo.SolicitudesRechazadas
                        .Union(porCC)
                        .DistinctBy(r => r.IdSolicitud)
                        .ToList();
                }
            }
            else if (esUsuario)
            {
                var emailActual = User.Identity?.Name ?? "";
                var personal = await _context.Tbpersonals
                    .FirstOrDefaultAsync(p =>
                        p.CorreoCorporativo == emailActual ||
                        p.UsuarioCorporativo == emailActual);

                if (personal != null)
                {
                    modelo.Solicitudes = await _context.Tbsolicitudes.Where(s =>
                        s.CC == personal.CC &&
                        ((s.TipoSolicitud != null && s.TipoSolicitud.Contains(termino)) ||
                         (s.SubtipoPermiso != null && s.SubtipoPermiso.Contains(termino)) ||
                         (s.Estado != null && s.Estado.Contains(termino)) ||
                         s.CC.ToString() == termino)
                    ).ToListAsync();
                }
            }

            return View(modelo);
        }
    }
}