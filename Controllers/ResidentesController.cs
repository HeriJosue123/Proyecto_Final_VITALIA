using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    public class ResidentesController : Controller
    {
        private readonly VitaliaDbContext _context;

        public ResidentesController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Residentes
        public async Task<IActionResult> Index()
        {
            return View(await _context.Residentes.ToListAsync());
        }

        // GET: Residentes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var residente = await _context.Residentes
                .FirstOrDefaultAsync(m => m.IdResidente == id);
            if (residente == null) return NotFound();

            return View(residente);
        }

        // GET: Residentes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Residentes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdResidente,CodigoUnico,Nombre,Apellido,FechaNacimiento,NumHabitacion,TipoSangre,Alergias,ContactoEmergencia,Observaciones,Estado")] Residente residente)
        {
            if (ModelState.IsValid)
            {
                _context.Add(residente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Edit(int id, [Bind("IdResidente,CodigoUnico,Nombre,Apellido,FechaNacimiento,NumHabitacion,TipoSangre,Alergias,ContactoEmergencia,Observaciones,Estado")] Residente residente)
        {
            if (id != residente.IdResidente) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(residente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResidenteExists(residente.IdResidente)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
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
            if (residente != null)
            {
                // Baja lógica en lugar de borrado físico (RF-005)
                residente.Estado = false;
                _context.Update(residente);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ResidenteExists(int id)
        {
            return _context.Residentes.Any(e => e.IdResidente == id);
        }
    }
}