using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    /// <summary>
    /// Carga los datos de presentación (iguales para todo el grupo) una sola vez.
    /// Solo el Administrador puede usarlo y, ya cargados, la opción desaparece.
    /// </summary>
    [Authorize(Roles = RolesSistema.Administrador)]
    public class DatosPresentacionController : Controller
    {
        private readonly VitaliaDbContext _context;
        private readonly ILogger<DatosPresentacionController> _logger;

        public DatosPresentacionController(VitaliaDbContext context, ILogger<DatosPresentacionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: DatosPresentacion
        public async Task<IActionResult> Index()
        {
            if (await DatosPresentacion.CargadosAsync(_context)) return RedirectToAction("Index", "Home");
            return View();
        }

        // POST: DatosPresentacion/Cargar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cargar()
        {
            if (await DatosPresentacion.CargadosAsync(_context)) return RedirectToAction("Index", "Home");

            try
            {
                var resultado = await DatosPresentacion.CargarAsync(_context, DateTime.Now);
                ViewBag.Cuentas = DatosPresentacion.Cuentas().ToList();
                return View("Listo", resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron cargar los datos de presentación");
                var detalle = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = detalle.Contains("MEDICAMENTO", StringComparison.OrdinalIgnoreCase)
                    ? "Faltan las tablas de Farmacia en esta base. Créelas primero (script Crear_Tablas_Farmacia.sql) y vuelva a intentarlo. No se guardó nada."
                    : "No se pudieron cargar los datos: " + detalle + " No se guardó nada.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
