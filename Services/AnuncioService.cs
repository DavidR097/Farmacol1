using Farmacol.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Farmacol.Services
{
    public class AnuncioService
    {
        private readonly Farmacol1Context _context;
        private readonly AuditService _audit;
        private readonly IWebHostEnvironment _env;

        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long _fileSizeLimit = 5 * 1024 * 1024; // 5 MB

        public AnuncioService(Farmacol1Context context, AuditService audit, IWebHostEnvironment env)
        {
            _context = context;
            _audit = audit;
            _env = env;
        }

        private (int width, int height)? GetImageDimensions(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);
                var sig = br.ReadBytes(8);
                if (sig.Length >= 8 && sig[0] == 0x89 && sig[1] == 0x50 && sig[2] == 0x4E && sig[3] == 0x47)
                {
                    fs.Seek(16, SeekOrigin.Begin);
                    var wBytes = br.ReadBytes(4);
                    var hBytes = br.ReadBytes(4);
                    int w = (wBytes[0] << 24) | (wBytes[1] << 16) | (wBytes[2] << 8) | wBytes[3];
                    int h = (hBytes[0] << 24) | (hBytes[1] << 16) | (hBytes[2] << 8) | hBytes[3];
                    return (w, h);
                }
                if (sig.Length >= 3 && sig[0] == 0x47 && sig[1] == 0x49 && sig[2] == 0x46)
                {
                    fs.Seek(6, SeekOrigin.Begin);
                    ushort w = br.ReadUInt16();
                    ushort h = br.ReadUInt16();
                    return (w, h);
                }
                fs.Seek(0, SeekOrigin.Begin);
                if (br.ReadByte() == 0xFF && br.ReadByte() == 0xD8)
                {
                    while (fs.Position < fs.Length)
                    {
                        byte markerStart = br.ReadByte();
                        if (markerStart != 0xFF) continue;
                        byte marker = br.ReadByte();
                        while (marker == 0xFF) marker = br.ReadByte();
                        if (marker == 0xC0 || marker == 0xC1 || marker == 0xC2 || marker == 0xC3 || marker == 0xC5 || marker == 0xC6 || marker == 0xC7 || marker == 0xC9 || marker == 0xCA || marker == 0xCB || marker == 0xCD || marker == 0xCE || marker == 0xCF)
                        {
                            var lenBytes = br.ReadBytes(2);
                            br.ReadByte();
                            var hBytes = br.ReadBytes(2);
                            var wBytes = br.ReadBytes(2);
                            int h = (hBytes[0] << 8) | hBytes[1];
                            int w = (wBytes[0] << 8) | wBytes[1];
                            return (w, h);
                        }
                        else
                        {
                            var lenBytes = br.ReadBytes(2);
                            if (lenBytes.Length < 2) break;
                            int len = (lenBytes[0] << 8) | lenBytes[1];
                            if (len < 2) break;
                            fs.Seek(len - 2, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public async Task<List<TbAnuncio>> ObtenerAnunciosActivosAsync()
        {
            var hoy = DateTime.Now;
            return await _context.TbAnuncios
                .Where(a => a.Activo &&
                            (a.FechaExpiracion == null || a.FechaExpiracion > hoy))
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();
        }

        public async Task<(string? path, int? width, int? height)> GuardarImagenAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return (null, null, null);

            if (file.Length > _fileSizeLimit) throw new InvalidOperationException("Archivo demasiado grande");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_permittedExtensions.Contains(ext)) throw new InvalidOperationException("Tipo de archivo no permitido");

            var carpeta = Path.Combine(_env.WebRootPath ?? "wwwroot", "anuncios");
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

            var nombre = $"anuncio_{DateTime.Now.Ticks}{ext}";
            var ruta = Path.Combine(carpeta, nombre);
            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var dims = GetImageDimensions(ruta);
            return ($"/anuncios/{nombre}", dims?.width, dims?.height);
        }

        public async Task<List<TbAnuncio>> ObtenerTodosAsync()
        {
            return await _context.TbAnuncios
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();
        }

        public async Task CrearAsync(TbAnuncio anuncio, string creadoPor)
        {
            anuncio.CreadoPor = creadoPor;
            anuncio.FechaCreacion = DateTime.Now;
            _context.TbAnuncios.Add(anuncio);
            await _context.SaveChangesAsync();

            await _audit.RegistrarAsync(AuditService.MOD_ANUNCIOS, AuditService.ACC_CREAR,
                $"Anuncio creado: {anuncio.Titulo}", anuncio.Id.ToString());
        }

        public async Task ActualizarAsync(TbAnuncio anuncio)
        {
            // Obtener el registro existente para comprobar si hay una imagen previa y eliminarla si se reemplaza
            var existente = await _context.TbAnuncios.AsNoTracking().FirstOrDefaultAsync(a => a.Id == anuncio.Id);
            if (existente != null && !string.IsNullOrEmpty(existente.Imagen) && !string.Equals(existente.Imagen, anuncio.Imagen, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var rutaFisica = Path.Combine(_env.WebRootPath ?? "wwwroot", existente.Imagen.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(rutaFisica)) File.Delete(rutaFisica);
                }
                catch { /* no bloquear la operación por fallo al borrar archivo */ }
            }

            _context.Update(anuncio);
            await _context.SaveChangesAsync();

            await _audit.RegistrarAsync(AuditService.MOD_ANUNCIOS, AuditService.ACC_EDITAR,
                $"Anuncio actualizado: {anuncio.Titulo}", anuncio.Id.ToString());
        }

        public async Task EliminarAsync(int id)
        {
            var anuncio = await _context.TbAnuncios.FindAsync(id);
            if (anuncio != null)
            {
                // eliminar archivo de imagen si existe
                if (!string.IsNullOrEmpty(anuncio.Imagen))
                {
                    try
                    {
                        var rutaFisica = Path.Combine(_env.WebRootPath ?? "wwwroot", anuncio.Imagen.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(rutaFisica)) File.Delete(rutaFisica);
                    }
                    catch { }
                }

                _context.TbAnuncios.Remove(anuncio);
                await _context.SaveChangesAsync();
                await _audit.RegistrarAsync(AuditService.MOD_ANUNCIOS, AuditService.ACC_ELIMINAR,
                    $"Anuncio eliminado: {anuncio.Titulo}", id.ToString());
            }
        }
    }
}
