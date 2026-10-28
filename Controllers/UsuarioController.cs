using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly VitaliaDbContext _context;

        public UsuarioController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Usuario
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .OrderByDescending(u => u.IdUsuario)
                .ToListAsync();

            return View(usuarios);
        }

        // GET: Usuario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(m => m.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // GET: Usuario/Create
        public IActionResult Create()
        {
            ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol");
            return View();
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Correo,IdRol")] Usuario usuario, string Contrasena)
        {
            // Remover las validaciones automáticas de los campos no enlazados en la petición
            ModelState.Remove(nameof(Usuario.ContrasenaHash));
            ModelState.Remove(nameof(Usuario.IdRolNavigation));

            if (string.IsNullOrWhiteSpace(Contrasena))
            {
                ModelState.AddModelError("Contrasena", "La contraseña es obligatoria.");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Correo == usuario.Correo))
            {
                ModelState.AddModelError("Correo", "El correo ya está registrado en el sistema.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Hashing seguro de la contraseña antes de guardar
                    usuario.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(Contrasena);
                    usuario.Estado = true;

                    _context.Add(usuario);
                    await _context.SaveChangesAsync();

                    TempData["Mensaje"] = "Usuario creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar el usuario: " + ex.Message);
                }
            }

            ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol", usuario.IdRol);
            return View(usuario);
        }

        // GET: Usuario/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol", usuario.IdRol);
            return View(usuario);
        }

        // POST: Usuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Correo,IdRol,Estado")] Usuario usuario, string? NuevaContrasena)
        {
            if (id != usuario.IdUsuario)
            {
                return NotFound();
            }

            // Remover validaciones automáticas
            ModelState.Remove(nameof(Usuario.ContrasenaHash));
            ModelState.Remove(nameof(Usuario.IdRolNavigation));

            // Validar si el correo cambió y si ya pertenece a otro usuario
            if (await _context.Usuarios.AnyAsync(u => u.Correo == usuario.Correo && u.IdUsuario != id))
            {
                ModelState.AddModelError("Correo", "El correo ingresado ya pertenece a otro usuario.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var usuarioExistente = await _context.Usuarios.FindAsync(id);
                    if (usuarioExistente == null)
                    {
                        return NotFound();
                    }

                    // Actualizar campos
                    usuarioExistente.Correo = usuario.Correo;
                    usuarioExistente.IdRol = usuario.IdRol;
                    usuarioExistente.Estado = usuario.Estado;

                    // Si escribió una nueva contraseña, la actualizamos con hash
                    if (!string.IsNullOrWhiteSpace(NuevaContrasena))
                    {
                        usuarioExistente.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(NuevaContrasena);
                    }

                    _context.Update(usuarioExistente);
                    await _context.SaveChangesAsync();

                    TempData["Mensaje"] = "Usuario actualizado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IdUsuario))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol", usuario.IdRol);
            return View(usuario);
        }

        // GET: Usuario/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(m => m.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Usuario eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}