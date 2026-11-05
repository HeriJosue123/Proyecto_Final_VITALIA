using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models.Rrhh;

namespace VITALIA.Areas.RRHH.Controllers
{
    [Area("RRHH")]
    [Authorize(Roles = "Administrador")]
    public class PermisosController : Controller
    {
        private readonly VitaliaDbContext _context;

        public PermisosController(VitaliaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var permisos = await _context.SolicitudesPermiso
                .Include(p => p.IdEmpleadoNavigation)
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync();

            return View(permisos);
        }

        public async Task<IActionResult> Crear()
        {
            var empleados = await _context.Empleados
                .Where(e => e.Estado)
                .Select(e => new { e.IdEmpleado, NombreCompleto = $"{e.Nombre} {e.Apellido}" })
                .ToListAsync();

            ViewBag.Empleados = new SelectList(empleados, "IdEmpleado", "NombreCompleto");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(SolicitudPermiso model)
        {
            if (model.FechaFin < model.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la fecha de inicio.");
            }

            if (ModelState.IsValid)
            {
                _context.SolicitudesPermiso.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var empleados = await _context.Empleados
                .Where(e => e.Estado)
                .Select(e => new { e.IdEmpleado, NombreCompleto = $"{e.Nombre} {e.Apellido}" })
                .ToListAsync();

            ViewBag.Empleados = new SelectList(empleados, "IdEmpleado", "NombreCompleto", model.IdEmpleado);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var permiso = await _context.SolicitudesPermiso.FindAsync(id);
            if (permiso != null)
            {
                permiso.Estado = estado;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}