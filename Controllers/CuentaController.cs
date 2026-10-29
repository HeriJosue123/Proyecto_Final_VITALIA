using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers
{
    // Inicio y cierre de sesión. Es la única parte del sistema que se ve sin haber entrado.
    [AllowAnonymous]
    public class CuentaController : Controller
    {
        private readonly VitaliaDbContext _context;

        public CuentaController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Cuenta/Login
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Cuenta/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string correo = modelo.Correo.Trim();
            var usuario = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Include(u => u.Empleado)
                .FirstOrDefaultAsync(u => u.Correo == correo);

            // El mismo mensaje si no existe el correo o si la contraseña está mal
            if (usuario == null || !ContrasenaCorrecta(modelo.Contrasena, usuario.ContrasenaHash))
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return View(modelo);
            }

            if (!usuario.Estado)
            {
                ModelState.AddModelError(string.Empty, "Este usuario está desactivado. Consulte con el administrador.");
                return View(modelo);
            }

            // Si es un empleado activo se muestra su nombre; si no, su correo
            string nombre = usuario.Empleado != null && usuario.Empleado.Estado
                ? usuario.Empleado.Nombre + " " + usuario.Empleado.Apellido
                : usuario.Correo;

            var datos = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, nombre),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.IdRolNavigation.NombreRol)
            };
            var identidad = new ClaimsIdentity(datos, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad),
                new AuthenticationProperties { IsPersistent = modelo.Recordarme });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // POST: Cuenta/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET: Cuenta/AccesoDenegado
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        // Compara la contraseña escrita con la guardada (BCrypt). Si lo guardado no es BCrypt, no entra.
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
