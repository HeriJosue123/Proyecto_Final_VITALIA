using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    [Authorize(Roles = RolesSistema.Administrador)]
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

        // GET: Personal/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados
                .Include(e => e.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.IdEmpleado == id);

            if (empleado == null)
            {
                return NotFound();
            }

            return View(empleado);
        }

        // GET: Personal/Create
        public IActionResult Create()
        {
            // Solo usuarios activos que todavía no tienen un empleado vinculado
            var usuariosDisponibles = _context.Usuarios
                .Where(u => u.Estado == true && u.Empleado == null)
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
            ModelState.Remove(nameof(Empleado.IdUsuarioNavigation));

            CambiarMensaje(nameof(Empleado.Nombre), "El nombre es obligatorio.");
            CambiarMensaje(nameof(Empleado.Apellido), "El apellido es obligatorio.");
            CambiarMensaje(nameof(Empleado.Cargo), "El cargo es obligatorio.");
            CambiarMensaje(nameof(Empleado.IdUsuario), "Debe seleccionar un usuario.");

            if (empleado.IdUsuario <= 0)
            {
                if (!ModelState.ContainsKey(nameof(Empleado.IdUsuario)) || ModelState[nameof(Empleado.IdUsuario)]!.Errors.Count == 0)
                {
                    ModelState.AddModelError(nameof(Empleado.IdUsuario), "Debe seleccionar un usuario.");
                }
            }
            else if (await _context.Empleados.AnyAsync(e => e.IdUsuario == empleado.IdUsuario))
            {
                ModelState.AddModelError(nameof(Empleado.IdUsuario), "Ese usuario ya está vinculado a otro empleado.");
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

            var usuariosDisponibles = _context.Usuarios
                .Where(u => u.Estado == true && u.Empleado == null)
                .OrderBy(u => u.Correo)
                .ToList();

            ViewData["IdUsuario"] = new SelectList(usuariosDisponibles, "IdUsuario", "Correo", empleado.IdUsuario);
            return View(empleado);
        }

        // GET: Personal/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            // Para editar, mostramos los usuarios libres MÁS el usuario que el empleado ya tiene asignado
            var usuariosDisponibles = _context.Usuarios
                .Where(u => u.Estado == true && (u.Empleado == null || u.IdUsuario == empleado.IdUsuario))
                .OrderBy(u => u.Correo)
                .ToList();

            ViewData["IdUsuario"] = new SelectList(usuariosDisponibles, "IdUsuario", "Correo", empleado.IdUsuario);
            return View(empleado);
        }

        // POST: Personal/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdEmpleado,IdUsuario,Nombre,Apellido,Cargo")] Empleado empleado)
        {
            if (id != empleado.IdEmpleado)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Empleado.IdUsuarioNavigation));

            CambiarMensaje(nameof(Empleado.Nombre), "El nombre es obligatorio.");
            CambiarMensaje(nameof(Empleado.Apellido), "El apellido es obligatorio.");
            CambiarMensaje(nameof(Empleado.Cargo), "El cargo es obligatorio.");
            CambiarMensaje(nameof(Empleado.IdUsuario), "Debe seleccionar un usuario.");

            // Validar que el nuevo usuario no esté tomado por OTRO empleado
            if (empleado.IdUsuario > 0)
            {
                if (await _context.Empleados.AnyAsync(e => e.IdUsuario == empleado.IdUsuario && e.IdEmpleado != empleado.IdEmpleado))
                {
                    ModelState.AddModelError(nameof(Empleado.IdUsuario), "Ese usuario ya está vinculado a otro empleado.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    empleado.Estado = true; // Mantenerlo activo
                    _context.Update(empleado);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmpleadoExists(empleado.IdEmpleado))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var usuariosDisponibles = _context.Usuarios
                .Where(u => u.Estado == true && (u.Empleado == null || u.IdUsuario == empleado.IdUsuario))
                .OrderBy(u => u.Correo)
                .ToList();

            ViewData["IdUsuario"] = new SelectList(usuariosDisponibles, "IdUsuario", "Correo", empleado.IdUsuario);
            return View(empleado);
        }

        // GET: Personal/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados
                .Include(e => e.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.IdEmpleado == id);

            if (empleado == null)
            {
                return NotFound();
            }

            return View(empleado);
        }

        // POST: Personal/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado != null)
            {
                empleado.Estado = false; // Baja lógica
                _context.Update(empleado);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Reemplaza los mensajes automáticos
        private void CambiarMensaje(string campo, string mensaje)
        {
            if (ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0)
            {
                entrada.Errors.Clear();
                entrada.Errors.Add(mensaje);
            }
        }

        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.IdEmpleado == id);
        }

        // GET: Personal/Asignar
        public IActionResult Asignar()
        {
            ViewData["IdEmpleado"] = new SelectList(
                _context.Empleados.Where(e => e.Estado).OrderBy(e => e.Nombre).ToList(),
                "IdEmpleado",
                "Nombre");

            ViewData["IdResidente"] = new SelectList(
                _context.Residentes.Where(r => r.Estado).OrderBy(r => r.Nombre).ToList(),
                "IdResidente",
                "Nombre");

            return View();
        }

        // POST: Personal/Asignar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar([Bind("IdEmpleado,IdResidente")] AsignacionCuidado asignacion)
        {
            ModelState.Remove("IdEmpleadoNavigation");
            ModelState.Remove("IdResidenteNavigation");

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

            ViewData["IdEmpleado"] = new SelectList(_context.Empleados.Where(e => e.Estado).OrderBy(e => e.Nombre).ToList(), "IdEmpleado", "Nombre", asignacion.IdEmpleado);
            ViewData["IdResidente"] = new SelectList(_context.Residentes.Where(r => r.Estado).OrderBy(r => r.Nombre).ToList(), "IdResidente", "Nombre", asignacion.IdResidente);
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