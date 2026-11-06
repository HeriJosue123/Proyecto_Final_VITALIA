using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using VITALIA.Data;
using VITALIA.Models.Rrhh;

namespace VITALIA.Areas.RRHH.Controllers
{
    [Area("RRHH")]
    [Authorize(Roles = "Administrador")]
    public class NominaController : Controller
    {
        private readonly VitaliaDbContext _context;

        public NominaController(VitaliaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var nominas = await _context.Nominas
                .Include(n => n.IdEmpleadoNavigation)
                .OrderByDescending(n => n.Anio)
                .ThenByDescending(n => n.Mes)
                .ToListAsync();

            return View(nominas);
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
        public async Task<IActionResult> Crear(Nomina model)
        {
            if (model.Deducciones > model.SalarioBruto)
            {
                ModelState.AddModelError("Deducciones", "Las deducciones no pueden superar el salario bruto.");
            }

            if (await _context.Nominas.AnyAsync(n => n.IdEmpleado == model.IdEmpleado && n.Mes == model.Mes && n.Anio == model.Anio))
            {
                ModelState.AddModelError("IdEmpleado", "Ya existe un registro de nómina para este empleado en el período seleccionado.");
            }

            if (ModelState.IsValid)
            {
                model.SalarioNeto = model.SalarioBruto - model.Deducciones;
                _context.Nominas.Add(model);
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
            var nomina = await _context.Nominas.FindAsync(id);
            if (nomina != null)
            {
                nomina.Estado = estado;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DescargarReciboPdf(int id)
        {
            var nomina = await _context.Nominas
                .Include(n => n.IdEmpleadoNavigation)
                .FirstOrDefaultAsync(n => n.IdNomina == id);

            if (nomina == null) return NotFound();

            return new ViewAsPdf("ReciboPdf", nomina)
            {
                FileName = $"Recibo_Nomina_{ParaNombreDeArchivo(nomina.IdEmpleadoNavigation?.Apellido)}_{nomina.Mes}_{nomina.Anio}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
            };
        }

        /// <summary>
        /// El nombre del archivo viaja en un encabezado HTTP, que solo admite caracteres ASCII: una tilde o una ñ
        /// (por ejemplo "Mejía") hace que el servidor falle con error 500. Se quitan las tildes y lo demás se cambia por guion bajo.
        /// </summary>
        private static string ParaNombreDeArchivo(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "Empleado";
            var sinTildes = new string(texto.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray());
            var limpio = Regex.Replace(sinTildes, "[^A-Za-z0-9]+", "_").Trim('_');
            return limpio.Length > 0 ? limpio : "Empleado";
        }
    }
}