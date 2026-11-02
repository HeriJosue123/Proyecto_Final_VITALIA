using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models.ViewModels;
using System.IO;
using System.Data;

namespace VITALIA.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class RespaldosController : Controller
    {
        private readonly VitaliaDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RespaldosController(VitaliaDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string ObtenerRutaPrivada()
        {
            var ruta = Path.Combine(_env.ContentRootPath, "Storage", "Respaldos");
            if (!Directory.Exists(ruta))
            {
                Directory.CreateDirectory(ruta);
            }
            return ruta;
        }

        private string SanearNombreArchivo(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return Path.GetFileName(id);
        }

        // GET: Respaldos/Index
        public IActionResult Index()
        {
            var ruta = ObtenerRutaPrivada();
            var archivos = new DirectoryInfo(ruta).GetFiles("*.bak");

            var lista = archivos.Select(f => new RespaldoViewModel
            {
                NombreArchivo = f.Name,
                Tamaño = (f.Length / 1024.0 / 1024.0).ToString("0.00") + " MB",
                FechaCreacion = f.CreationTime
            }).OrderByDescending(x => x.FechaCreacion).ToList();

            return View(lista);
        }

        // GET: Respaldos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Respaldos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RespaldoViewModel model)
        {
            try
            {
                var rutaFinal = ObtenerRutaPrivada();
                string nombreArchivo = $"VITALIA_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string rutaCompletaFinal = Path.Combine(rutaFinal, nombreArchivo);

                var connection = _context.Database.GetDbConnection();
                string dbName = connection.Database;

                // 1. Consultar la ruta por defecto donde SQL Server tiene permisos nativos
                string tempBackupDir = null;
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT SERVERPROPERTY('InstanceDefaultBackupPath')";
                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        tempBackupDir = result.ToString();
                    }
                }

                // Respaldo si SQL Server no retorna ruta por defecto
                if (string.IsNullOrEmpty(tempBackupDir))
                {
                    tempBackupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VITALIA_Temp");
                    if (!Directory.Exists(tempBackupDir)) Directory.CreateDirectory(tempBackupDir);
                }

                string tempRutaCompleta = Path.Combine(tempBackupDir, nombreArchivo);

                // 2. SQL Server genera la copia en su directorio nativo
                string query = $"BACKUP DATABASE [{dbName}] TO DISK = '{tempRutaCompleta}' WITH FORMAT, INIT";
                await _context.Database.ExecuteSqlRawAsync(query);

                // 3. C# mueve la copia a la carpeta privada Storage/Respaldos del proyecto
                if (System.IO.File.Exists(tempRutaCompleta))
                {
                    System.IO.File.Copy(tempRutaCompleta, rutaCompletaFinal, overwrite: true);
                    System.IO.File.Delete(tempRutaCompleta);
                }

                TempData["Exito"] = "Copia de seguridad generada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al generar el respaldo: " + ex.Message);
                return View();
            }
        }

        // GET: Respaldos/Delete?id=nombreArchivo.bak
        public IActionResult Delete(string id)
        {
            var nombreLimpio = SanearNombreArchivo(id);
            if (nombreLimpio == null) return NotFound();

            var rutaCompleta = Path.Combine(ObtenerRutaPrivada(), nombreLimpio);
            if (!System.IO.File.Exists(rutaCompleta)) return NotFound();

            var fileInfo = new FileInfo(rutaCompleta);
            var modelo = new RespaldoViewModel
            {
                NombreArchivo = fileInfo.Name,
                Tamaño = (fileInfo.Length / 1024.0 / 1024.0).ToString("0.00") + " MB",
                FechaCreacion = fileInfo.CreationTime
            };

            return View(modelo);
        }

        // POST: Respaldos/Delete?id=nombreArchivo.bak
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(string id)
        {
            var nombreLimpio = SanearNombreArchivo(id);
            if (nombreLimpio != null)
            {
                var rutaCompleta = Path.Combine(ObtenerRutaPrivada(), nombreLimpio);
                if (System.IO.File.Exists(rutaCompleta))
                {
                    System.IO.File.Delete(rutaCompleta);
                    TempData["Exito"] = "Respaldo eliminado permanentemente.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Respaldos/Download?id=nombreArchivo.bak
        public IActionResult Download(string id)
        {
            var nombreLimpio = SanearNombreArchivo(id);
            if (nombreLimpio == null) return NotFound();

            var rutaCompleta = Path.Combine(ObtenerRutaPrivada(), nombreLimpio);
            if (!System.IO.File.Exists(rutaCompleta)) return NotFound();

            return PhysicalFile(rutaCompleta, "application/octet-stream", nombreLimpio);
        }
    }
}