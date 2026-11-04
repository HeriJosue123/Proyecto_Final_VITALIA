using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models.Rrhh;

namespace VITALIA.Areas.RRHH.Controllers
{
    [Area("RRHH")]
    [Authorize(Roles = "Administrador")]
    public class PersonalExternoController : Controller
    {
        private readonly VitaliaDbContext _context;

        public PersonalExternoController(VitaliaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lista = await _context.PersonalExterno
                .Where(p => p.Activo)
                .OrderBy(p => p.AreaServicio)
                .ToListAsync();

            return View(lista);
        }

        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PersonalExterno model)
        {
            if (await _context.PersonalExterno.AnyAsync(p => p.DocumentoIdentidad == model.DocumentoIdentidad && p.Activo))
            {
                ModelState.AddModelError("DocumentoIdentidad", "Ya existe un personal externo activo registrado con este DUI.");
            }

            if (ModelState.IsValid)
            {
                _context.PersonalExterno.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoPago(int id, string estado)
        {
            var persona = await _context.PersonalExterno.FindAsync(id);
            if (persona != null)
            {
                persona.EstadoPago = estado;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var persona = await _context.PersonalExterno.FindAsync(id);
            if (persona != null)
            {
                persona.Activo = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}