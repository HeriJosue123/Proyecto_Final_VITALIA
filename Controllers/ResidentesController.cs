using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers
{
    public class ResidentesController : Controller
    {
        private readonly VitaliaDbContext _context;

        // Valores admitidos para el tipo de sangre (vacío = no registrado)
        public static readonly string[] TiposSangre = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

        // Campos editables del formulario (Estado no se recibe del formulario: se gestiona con la baja lógica)
        private const string CamposFormulario = "CodigoUnico,Nombre,Apellido,FechaNacimiento,NumHabitacion,TipoSangre,Alergias,ContactoEmergencia,Observaciones";

        public ResidentesController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Residentes
        public async Task<IActionResult> Index(string? busqueda, string? estado, string? habitacion, int? seleccionado)
        {
            estado = estado?.ToLowerInvariant() switch
            {
                "inactivos" => "inactivos",
                "todos" => "todos",
                _ => "activos"
            };

            var query = _context.Residentes.AsNoTracking().AsQueryable();

            if (estado == "activos") query = query.Where(r => r.Estado);
            else if (estado == "inactivos") query = query.Where(r => !r.Estado);

            // Búsqueda por nombre, apellido, nombre completo o código (sin distinguir mayúsculas)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string termino = busqueda.Trim().ToLower();
                query = query.Where(r =>
                    r.Nombre.ToLower().Contains(termino) ||
                    r.Apellido.ToLower().Contains(termino) ||
                    (r.Nombre + " " + r.Apellido).ToLower().Contains(termino) ||
                    r.CodigoUnico.ToLower().Contains(termino));
            }

            if (!string.IsNullOrWhiteSpace(habitacion))
            {
                query = query.Where(r => r.NumHabitacion == habitacion);
            }

            var residentes = await query.OrderBy(r => r.CodigoUnico).ToListAsync();

            // El panel lateral muestra el residente elegido o, si no hay selección, el primero del listado
            int? idPanel = seleccionado ?? residentes.FirstOrDefault()?.IdResidente;
            Residente? panel = idPanel == null ? null : await ObtenerFichaAsync(idPanel.Value);

            var viewModel = new ResidentesListadoViewModel
            {
                Residentes = residentes,
                Busqueda = busqueda,
                Estado = estado,
                Habitacion = habitacion,
                Habitaciones = await _context.Residentes.Select(r => r.NumHabitacion).Distinct().OrderBy(h => h).ToListAsync(),
                Seleccionado = panel,
                TotalActivos = await _context.Residentes.CountAsync(r => r.Estado),
                TotalInactivos = await _context.Residentes.CountAsync(r => !r.Estado)
            };

            return View(viewModel);
        }

        // GET: Residentes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var residente = await ObtenerFichaAsync(id.Value);
            if (residente == null) return NotFound();

            return View(residente);
        }

        // GET: Residentes/Create
        public IActionResult Create()
        {
            return View(new Residente { Estado = true });
        }

        // POST: Residentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(CamposFormulario)] Residente residente)
        {
            NormalizarDatos(residente);
            await ValidarReglasAsync(residente, idActual: null);

            if (ModelState.IsValid)
            {
                residente.Estado = true;
                _context.Add(residente);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"Residente {residente.CodigoUnico} — {residente.Nombre} {residente.Apellido} registrado correctamente.";
                return RedirectToAction(nameof(Index), new { seleccionado = residente.IdResidente });
            }
            return View(residente);
        }

        // GET: Residentes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var residente = await _context.Residentes.FindAsync(id);
            if (residente == null) return NotFound();
            return View(residente);
        }

        // POST: Residentes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdResidente," + CamposFormulario)] Residente residente)
        {
            if (id != residente.IdResidente) return NotFound();

            var existente = await _context.Residentes.FindAsync(id);
            if (existente == null) return NotFound();

            NormalizarDatos(residente);
            await ValidarReglasAsync(residente, idActual: id);

            if (ModelState.IsValid)
            {
                // Se copian solo los campos editables; el Estado se conserva
                existente.CodigoUnico = residente.CodigoUnico;
                existente.Nombre = residente.Nombre;
                existente.Apellido = residente.Apellido;
                existente.FechaNacimiento = residente.FechaNacimiento;
                existente.NumHabitacion = residente.NumHabitacion;
                existente.TipoSangre = residente.TipoSangre;
                existente.Alergias = residente.Alergias;
                existente.ContactoEmergencia = residente.ContactoEmergencia;
                existente.Observaciones = residente.Observaciones;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResidenteExists(id)) return NotFound();
                    else throw;
                }

                TempData["MensajeExito"] = $"Datos del residente {existente.CodigoUnico} actualizados correctamente.";
                return RedirectToAction(nameof(Index), new { estado = "todos", seleccionado = id });
            }

            residente.Estado = existente.Estado;
            return View(residente);
        }

        // GET: Residentes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var residente = await _context.Residentes
                .FirstOrDefaultAsync(m => m.IdResidente == id);
            if (residente == null) return NotFound();

            return View(residente);
        }

        // POST: Residentes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var residente = await _context.Residentes.FindAsync(id);
            if (residente == null) return NotFound();

            if (!residente.Estado)
            {
                TempData["MensajeError"] = $"El residente {residente.CodigoUnico} ya se encuentra dado de baja.";
                return RedirectToAction(nameof(Index), new { estado = "inactivos", seleccionado = id });
            }

            // Baja lógica en lugar de borrado físico: se conserva el historial
            residente.Estado = false;
            _context.Update(residente);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"El residente {residente.CodigoUnico} — {residente.Nombre} {residente.Apellido} fue dado de baja. Su expediente se conserva como inactivo.";
            return RedirectToAction(nameof(Index), new { estado = "inactivos", seleccionado = id });
        }

        private bool ResidenteExists(int id)
        {
            return _context.Residentes.Any(e => e.IdResidente == id);
        }

        private Task<Residente?> ObtenerFichaAsync(int id)
        {
            return _context.Residentes
                .AsNoTracking()
                .Include(r => r.FamiliarResidentes).ThenInclude(f => f.IdUsuarioNavigation)
                .Include(r => r.AsignacionCuidados).ThenInclude(a => a.IdEmpleadoNavigation)
                .FirstOrDefaultAsync(r => r.IdResidente == id);
        }

        private static void NormalizarDatos(Residente residente)
        {
            residente.CodigoUnico = residente.CodigoUnico?.Trim().ToUpperInvariant()!;
            residente.Nombre = residente.Nombre?.Trim()!;
            residente.Apellido = residente.Apellido?.Trim()!;
            residente.NumHabitacion = residente.NumHabitacion?.Trim()!;
            residente.ContactoEmergencia = residente.ContactoEmergencia?.Trim()!;
            residente.TipoSangre = string.IsNullOrWhiteSpace(residente.TipoSangre) ? null : residente.TipoSangre.Trim().ToUpperInvariant();
            residente.Alergias = string.IsNullOrWhiteSpace(residente.Alergias) ? null : residente.Alergias.Trim();
            residente.Observaciones = string.IsNullOrWhiteSpace(residente.Observaciones) ? null : residente.Observaciones.Trim();
        }

        /// <summary>
        /// Reglas que no se pueden expresar con atributos: formato de fecha en español,
        /// rango de la fecha de nacimiento, tipo de sangre válido y código único.
        /// </summary>
        private async Task ValidarReglasAsync(Residente residente, int? idActual)
        {
            // Los errores de conversión de fecha llegan en inglés desde el model binding
            if (ModelState.TryGetValue(nameof(Residente.FechaNacimiento), out var entradaFecha) && entradaFecha.Errors.Count > 0)
            {
                bool vacia = string.IsNullOrWhiteSpace(entradaFecha.AttemptedValue);
                entradaFecha.Errors.Clear();
                entradaFecha.Errors.Add(vacia
                    ? "La fecha de nacimiento es obligatoria."
                    : "La fecha de nacimiento no tiene un formato válido.");
            }
            else
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                if (residente.FechaNacimiento >= hoy)
                {
                    ModelState.AddModelError(nameof(Residente.FechaNacimiento), "La fecha de nacimiento debe ser anterior a la fecha actual.");
                }
                else if (residente.FechaNacimiento < hoy.AddYears(-130))
                {
                    ModelState.AddModelError(nameof(Residente.FechaNacimiento), "La fecha de nacimiento no es válida (más de 130 años).");
                }
            }

            if (residente.TipoSangre != null && !TiposSangre.Contains(residente.TipoSangre))
            {
                ModelState.AddModelError(nameof(Residente.TipoSangre), "Seleccione un tipo de sangre válido.");
            }

            // El código del residente es irrepetible (incluye residentes dados de baja)
            if (!string.IsNullOrEmpty(residente.CodigoUnico))
            {
                bool duplicado = await _context.Residentes.AnyAsync(r =>
                    r.CodigoUnico.ToUpper() == residente.CodigoUnico &&
                    (idActual == null || r.IdResidente != idActual));

                if (duplicado)
                {
                    ModelState.AddModelError(nameof(Residente.CodigoUnico), $"Ya existe un residente con el código {residente.CodigoUnico}.");
                }
            }
        }
    }
}
