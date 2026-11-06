using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
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

        // --- MÉTODO PARA GENERAR EL COMPROBANTE PDF CON ROTATIVA ---
        [HttpGet]
        public async Task<IActionResult> DescargarComprobantePdf(int id)
        {
            var persona = await _context.PersonalExterno.FirstOrDefaultAsync(p => p.IdPersonalExterno == id && p.Activo);

            if (persona == null) return NotFound();

            return new ViewAsPdf("ComprobantePdf", persona)
            {
                FileName = $"Comprobante_{ParaNombreDeArchivo(persona.Apellido)}_{ParaNombreDeArchivo(persona.Nombre)}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.Letter,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
            };
        }

        private static string ParaNombreDeArchivo(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "Externo";
            var sinTildes = new string(texto.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());
            var limpio = Regex.Replace(sinTildes, "[^A-Za-z0-9]+", "_").Trim('_');
            return limpio.Length > 0 ? limpio : "Externo";
        }
    }
}