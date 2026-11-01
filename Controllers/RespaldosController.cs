using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models.ViewModels;
using System.IO;

namespace VITALIA.Controllers
{
    public class RespaldosController : Controller
    {
        private readonly VitaliaDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RespaldosController(VitaliaDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Respaldos/Index
        public IActionResult Index()
        {
            var ruta = Path.Combine(_env.WebRootPath, "respaldos");
            if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);

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
        public async Task<IActionResult> Create(RespaldoViewModel model) // El modelo va por convención, aunque generamos el nombre internamente
        {
            try
            {
                var ruta = Path.Combine(_env.WebRootPath, "respaldos");
                if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);

                string nombreArchivo = $"VITALIA_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string rutaCompleta = Path.Combine(ruta, nombreArchivo);

                var connection = _context.Database.GetDbConnection();
                string dbName = connection.Database;

                string query = $"BACKUP DATABASE [{dbName}] TO DISK = '{rutaCompleta}'";
                await _context.Database.ExecuteSqlRawAsync(query);

                TempData["Exito"] = "Copia de seguridad generada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al generar el respaldo: " + ex.Message);
                return View(); // Retorna a la vista Create mostrando el error
            }
        }

        // GET: Respaldos/Delete?id=nombreArchivo.bak
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var ruta = Path.Combine(_env.WebRootPath, "respaldos", id);
            if (!System.IO.File.Exists(ruta)) return NotFound();

            var fileInfo = new FileInfo(ruta);
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
            var ruta = Path.Combine(_env.WebRootPath, "respaldos", id);
            if (System.IO.File.Exists(ruta))
            {
                System.IO.File.Delete(ruta);
                TempData["Exito"] = "Respaldo eliminado permanentemente.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Respaldos/Download?id=nombreArchivo.bak
        public IActionResult Download(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var ruta = Path.Combine(_env.WebRootPath, "respaldos", id);
            if (!System.IO.File.Exists(ruta)) return NotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(ruta);
            return File(fileBytes, "application/octet-stream", id);
        }
    }
}