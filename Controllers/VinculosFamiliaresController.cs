using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers
{
    // El Administrador vincula usuarios con rol Familiar a los residentes (tabla FAMILIAR_RESIDENTE)
    [Authorize(Roles = RolesSistema.Administrador)]
    public class VinculosFamiliaresController : Controller
    {
        private readonly VitaliaDbContext _context;

        public VinculosFamiliaresController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: VinculosFamiliares/Create?idResidente=5
        public async Task<IActionResult> Create(int idResidente)
        {
            var residente = await _context.Residentes.FirstOrDefaultAsync(r => r.IdResidente == idResidente && r.Estado);
            if (residente == null) return NotFound();

            var modelo = new VinculoFamiliarViewModel
            {
                IdResidente = residente.IdResidente,
                ResidenteNombre = residente.Nombre + " " + residente.Apellido
            };
            await CargarUsuariosAsync(idResidente, null);
            return View(modelo);
        }

        // POST: VinculosFamiliares/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VinculoFamiliarViewModel modelo)
        {
            var residente = await _context.Residentes.FirstOrDefaultAsync(r => r.IdResidente == modelo.IdResidente && r.Estado);
            if (residente == null) return NotFound();
            modelo.ResidenteNombre = residente.Nombre + " " + residente.Apellido;

            if (!string.IsNullOrEmpty(modelo.Parentesco) && !VinculoFamiliarViewModel.Parentescos.Contains(modelo.Parentesco))
            {
                ModelState.AddModelError(nameof(modelo.Parentesco), "Seleccione un parentesco de la lista.");
            }

            var usuario = await _context.Usuarios.Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == modelo.IdUsuario);
            if (modelo.IdUsuario > 0 && (usuario == null || !usuario.Estado || usuario.IdRolNavigation.NombreRol != RolesSistema.Familiar))
            {
                ModelState.AddModelError(nameof(modelo.IdUsuario), "El usuario debe estar activo y tener el rol Familiar.");
            }
            else if (await _context.FamiliarResidentes.AnyAsync(f => f.IdResidente == modelo.IdResidente && f.IdUsuario == modelo.IdUsuario))
            {
                ModelState.AddModelError(nameof(modelo.IdUsuario), "Ese familiar ya está vinculado a este residente.");
            }

            if (!ModelState.IsValid)
            {
                await CargarUsuariosAsync(modelo.IdResidente, modelo.IdUsuario);
                return View(modelo);
            }

            // Teléfono con el guion en medio (7000-1111)
            string digitos = new string(modelo.Telefono.Where(char.IsDigit).ToArray());
            _context.FamiliarResidentes.Add(new FamiliarResidente
            {
                IdResidente = modelo.IdResidente,
                IdUsuario = modelo.IdUsuario,
                Parentesco = modelo.Parentesco,
                TelefonoContacto = digitos[..4] + "-" + digitos[4..]
            });
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = usuario!.Correo + " quedó vinculado a " + modelo.ResidenteNombre + ".";
            return RedirectToAction("Details", "Residentes", new { id = modelo.IdResidente });
        }

        // POST: VinculosFamiliares/Desvincular/3
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desvincular(int id)
        {
            var vinculo = await _context.FamiliarResidentes
                .Include(f => f.IdUsuarioNavigation)
                .FirstOrDefaultAsync(f => f.IdFamiliarResidente == id);
            if (vinculo == null) return NotFound();

            _context.FamiliarResidentes.Remove(vinculo);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = vinculo.IdUsuarioNavigation.Correo + " ya no está vinculado a este residente.";
            return RedirectToAction("Details", "Residentes", new { id = vinculo.IdResidente });
        }

        // Usuarios activos con rol Familiar que todavía no están vinculados a este residente
        private async Task CargarUsuariosAsync(int idResidente, int? seleccionado)
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.Estado && u.IdRolNavigation.NombreRol == RolesSistema.Familiar
                         && !u.FamiliarResidentes.Any(f => f.IdResidente == idResidente))
                .OrderBy(u => u.Correo)
                .ToListAsync();

            ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo", seleccionado);
        }
    }
}
