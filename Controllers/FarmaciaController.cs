using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    // Restringe el acceso global a Administrador, Médico (con/sin tilde) y Enfermero. Bloquea a Familiar.
    [Authorize(Roles = "Administrador, Medico, Médico, Enfermero")]
    public class FarmaciaController : Controller
    {
        private readonly VitaliaDbContext _context;

        public FarmaciaController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Farmacia (Ver catálogo: Admin, Médico, Enfermero)
        public async Task<IActionResult> Index()
        {
            var medicamentos = await _context.Medicamentos
                .Include(m => m.LoteMedicamentos)
                .ToListAsync();
            return View(medicamentos);
        }

        // GET: Farmacia/Details/5 (Ver detalle: Admin, Médico, Enfermero)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var medicamento = await _context.Medicamentos
                .Include(m => m.LoteMedicamentos)
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null) return NotFound();

            return View(medicamento);
        }

        // GET: Farmacia/Create (Agregar: Solo Admin y Médico)
        [Authorize(Roles = "Administrador, Medico, Médico")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Farmacia/Create (Agregar: Solo Admin y Médico)
        [Authorize(Roles = "Administrador, Medico, Médico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicamento medicamento, string numeroLote, int? stockInicial, DateTime? fechaVencimiento)
        {
            // Validaciones obligatorias del Lote Inicial
            if (string.IsNullOrWhiteSpace(numeroLote))
            {
                ModelState.AddModelError("numeroLote", "El N° de Lote es obligatorio.");
            }
            if (!stockInicial.HasValue || stockInicial.Value <= 0)
            {
                ModelState.AddModelError("stockInicial", "Ingrese un stock inicial mayor a 0.");
            }
            if (!fechaVencimiento.HasValue)
            {
                ModelState.AddModelError("fechaVencimiento", "La fecha de vencimiento es obligatoria.");
            }

            if (ModelState.IsValid)
            {
                // 1. Guardar Medicamento
                _context.Add(medicamento);
                await _context.SaveChangesAsync();

                // 2. Guardar Lote Inicial
                var lote = new LoteMedicamento
                {
                    IdMedicamento = medicamento.IdMedicamento,
                    NumeroLote = numeroLote.Trim(),
                    Stock = stockInicial!.Value,
                    FechaVencimiento = fechaVencimiento!.Value
                };

                _context.LoteMedicamentos.Add(lote);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Medicamento y lote inicial registrados con éxito.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.NumeroLote = numeroLote;
            ViewBag.StockInicial = stockInicial;
            ViewBag.FechaVencimiento = fechaVencimiento?.ToString("yyyy-MM-dd");

            return View(medicamento);
        }

        // GET: Farmacia/Edit/5 (Editar: Solo Admin y Médico)
        [Authorize(Roles = "Administrador, Medico, Médico")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var medicamento = await _context.Medicamentos
                .Include(m => m.LoteMedicamentos)
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null) return NotFound();

            return View(medicamento);
        }

        // POST: Farmacia/Edit/5 (Editar: Solo Admin y Médico)
        [Authorize(Roles = "Administrador, Medico, Médico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicamento medicamento, string numeroLote, int? stockInicial, DateTime? fechaVencimiento)
        {
            if (id != medicamento.IdMedicamento) return NotFound();

            // Validaciones obligatorias del Lote
            if (string.IsNullOrWhiteSpace(numeroLote))
            {
                ModelState.AddModelError("numeroLote", "El N° de Lote es obligatorio.");
            }
            if (!stockInicial.HasValue || stockInicial.Value < 0)
            {
                ModelState.AddModelError("stockInicial", "Ingrese un stock válido (0 o mayor).");
            }
            if (!fechaVencimiento.HasValue)
            {
                ModelState.AddModelError("fechaVencimiento", "La fecha de vencimiento es obligatoria.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Actualizar Medicamento
                    _context.Update(medicamento);

                    // 2. Actualizar o crear Lote
                    var loteExistente = await _context.LoteMedicamentos
                        .FirstOrDefaultAsync(l => l.IdMedicamento == id);

                    if (loteExistente != null)
                    {
                        loteExistente.NumeroLote = numeroLote.Trim();
                        loteExistente.Stock = stockInicial!.Value;
                        loteExistente.FechaVencimiento = fechaVencimiento!.Value;
                        _context.Update(loteExistente);
                    }
                    else
                    {
                        var nuevoLote = new LoteMedicamento
                        {
                            IdMedicamento = medicamento.IdMedicamento,
                            NumeroLote = numeroLote.Trim(),
                            Stock = stockInicial!.Value,
                            FechaVencimiento = fechaVencimiento!.Value
                        };
                        _context.LoteMedicamentos.Add(nuevoLote);
                    }

                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Medicamento e inventario actualizados correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Medicamentos.Any(e => e.IdMedicamento == medicamento.IdMedicamento))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(medicamento);
        }

        // GET: Farmacia/Delete/5 (Eliminar: Solo Administrador)
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medicamento = await _context.Medicamentos
                .Include(m => m.LoteMedicamentos).ThenInclude(l => l.EntregaMedicamentos)
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento == null) return NotFound();

            // Para avisar en la pantalla qué se borra junto con el medicamento
            ViewBag.Lotes = medicamento.LoteMedicamentos.Count;
            ViewBag.Entregas = medicamento.LoteMedicamentos.Sum(l => l.EntregaMedicamentos.Count);

            return View(medicamento);
        }

        // POST: Farmacia/Delete/5 (Eliminar: Solo Administrador)
        [Authorize(Roles = "Administrador")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicamento = await _context.Medicamentos
                .Include(m => m.LoteMedicamentos).ThenInclude(l => l.EntregaMedicamentos)
                .FirstOrDefaultAsync(m => m.IdMedicamento == id);

            if (medicamento != null)
            {
                // Si ya se entregó a residentes, borrarlo haría perder ese historial
                int entregas = medicamento.LoteMedicamentos.Sum(l => l.EntregaMedicamentos.Count);
                if (entregas > 0)
                {
                    TempData["Error"] = $"No se puede eliminar «{medicamento.Nombre}» porque ya tiene {entregas} entrega(s) a residentes y se perdería ese historial. Si ya no se usa, márquelo como Inactivo desde Editar.";
                    return RedirectToAction(nameof(Index));
                }

                // Sin entregas: se borran también sus lotes (la base no deja borrar el medicamento si tiene lotes)
                int lotes = medicamento.LoteMedicamentos.Count;
                _context.LoteMedicamentos.RemoveRange(medicamento.LoteMedicamentos);
                _context.Medicamentos.Remove(medicamento);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = lotes > 0
                    ? $"Medicamento eliminado correctamente junto con sus {lotes} lote(s)."
                    : "Medicamento eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}