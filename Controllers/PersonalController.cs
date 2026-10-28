using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    public class PersonalController : Controller
    {
        private readonly VitaliaDbContext _context;

        public PersonalController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Personal
        public async Task<IActionResult> Index()
        {
            var empleados = await _context.Empleados
                .Include(e => e.IdUsuarioNavigation)
                .Where(e => e.Estado == true)
                .OrderBy(e => e.Apellido)
                .ToListAsync();

            var asignaciones = await _context.AsignacionCuidados
                .Include(a => a.IdEmpleadoNavigation)
                .Include(a => a.IdResidenteNavigation)
                .Where(a => a.Estado == true)
                .OrderByDescending(a => a.FechaInicio)
                .ToListAsync();

            ViewBag.Asignaciones = asignaciones;

            return View(empleados);
        }

        // GET: Personal/Create
        public IActionResult Create()
        {
            var usuariosDisponibles = _context.Usuarios
                .Where(u => u.Estado == true)
                .OrderBy(u => u.Correo)
                .ToList();

            ViewData["IdUsuario"] = new SelectList(usuariosDisponibles, "IdUsuario", "Correo");
            return View();
        }

        // POST: Personal/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,Apellido,Cargo")] Empleado empleado)
        {
            if (empleado.IdUsuario <= 0)
            {
                ModelState.AddModelError("IdUsuario", "Debe seleccionar un usuario válido.");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    empleado.Estado = true;
                    _context.Add(empleado);
                    await _context.SaveChangesAsync();

                    TempData["Mensaje"] = "Empleado registrado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
            }

            // Si falla, recargamos el dropdown
            var usuariosDisponibles = _context.Usuarios
                .Where(u => u.Estado == true)
                .OrderBy(u => u.Correo)
                .ToList();

            ViewData["IdUsuario"] = new SelectList(usuariosDisponibles, "IdUsuario", "Correo", empleado.IdUsuario);
            return View(empleado);
        }

        // POST: Personal/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado != null)
            {
                empleado.Estado = false;
                _context.Update(empleado);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Personal/Asignar
        public IActionResult Asignar()
        {
            ViewData["IdEmpleado"] = new SelectList(
                _context.Empleados.OrderBy(e => e.Nombre).ToList(),
                "IdEmpleado",
                "Nombre");

            ViewData["IdResidente"] = new SelectList(
                _context.Residentes.OrderBy(r => r.Nombre).ToList(),
                "IdResidente",
                "Nombre");

            return View();
        }

        // POST: Personal/Asignar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar([Bind("IdEmpleado,IdResidente")] AsignacionCuidado asignacion)
        {
            var existe = await _context.AsignacionCuidados
                .AnyAsync(a => a.IdEmpleado == asignacion.IdEmpleado
                            && a.IdResidente == asignacion.IdResidente
                            && a.Estado == true);

            if (existe)
            {
                ModelState.AddModelError("", "El empleado ya está asignado a este residente actualmente.");
            }

            if (ModelState.IsValid)
            {
                asignacion.FechaInicio = DateTime.Now;
                asignacion.Estado = true;
                _context.Add(asignacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["IdEmpleado"] = new SelectList(_context.Empleados.OrderBy(e => e.Nombre).ToList(), "IdEmpleado", "Nombre", asignacion.IdEmpleado);
            ViewData["IdResidente"] = new SelectList(_context.Residentes.OrderBy(r => r.Nombre).ToList(), "IdResidente", "Nombre", asignacion.IdResidente);
            return View(asignacion);
        }

        // POST: Personal/FinalizarAsignacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarAsignacion(int id)
        {
            var asignacion = await _context.AsignacionCuidados.FindAsync(id);
            if (asignacion != null)
            {
                asignacion.Estado = false;
                asignacion.FechaFin = DateTime.Now;
                _context.Update(asignacion);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}