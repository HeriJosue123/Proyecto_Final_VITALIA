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
                return User.IsInRole(RolesSistema.Familiar)
                    ? RedirectToAction("Index", "Familiar")
                    : RedirectToAction("Index", "Home");
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

            // El familiar siempre entra a su vista (solo vuelve a la página anterior si era de su sección)
            if (usuario.IdRolNavigation.NombreRol == RolesSistema.Familiar)
            {
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                       && returnUrl.StartsWith("/Familiar", StringComparison.OrdinalIgnoreCase)
                    ? Redirect(returnUrl)
                    : RedirectToAction("Index", "Familiar");
            }

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
        public IActionResult AccesoDenegado(string? returnUrl)
        {
            // Si el familiar abre la dirección principal (localhost/), se le lleva a su vista
            if (User.IsInRole(RolesSistema.Familiar) && (string.IsNullOrEmpty(returnUrl) || returnUrl == "/"))
            {
                return RedirectToAction("Index", "Familiar");
            }
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
