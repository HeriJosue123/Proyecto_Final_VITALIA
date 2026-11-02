using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VITALIA.Data;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers
{
    // Opciones del propio usuario que inició sesión (cualquier rol)
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly VitaliaDbContext _context;

        public PerfilController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Perfil/CambiarContrasena
        public IActionResult CambiarContrasena()
        {
            return View(new CambiarContrasenaViewModel());
        }

        // POST: Perfil/CambiarContrasena
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel modelo)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (!ContrasenaCorrecta(modelo.ContrasenaActual, usuario.ContrasenaHash))
                {
                    ModelState.AddModelError(nameof(modelo.ContrasenaActual), "La contraseña actual no es correcta.");
                }
                else if (modelo.ContrasenaNueva == modelo.ContrasenaActual)
                {
                    ModelState.AddModelError(nameof(modelo.ContrasenaNueva), "La contraseña nueva debe ser distinta de la actual.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(new CambiarContrasenaViewModel());
            }

            usuario.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(modelo.ContrasenaNueva);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Su contraseña se cambió correctamente.";
            return RedirectToAction(nameof(CambiarContrasena));
        }

        private static bool ContrasenaCorrecta(string contrasena, string hashGuardado)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(contrasena, hashGuardado);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}
