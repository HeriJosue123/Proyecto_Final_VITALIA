using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    public class TipoAtencionController : Controller
    {
        private readonly VitaliaDbContext _context;

        public TipoAtencionController(VitaliaDbContext context)
        {
            _context = context;
        }

        // 1. LISTAR / BÚSQUEDA
        public async Task<IActionResult> Index(string busqueda)
        {
            var query = _context.TipoAtencions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var term = busqueda.Trim().ToLower();
                query = query.Where(t => t.NombreCategoria.ToLower().Contains(term) ||
                                         (t.Descripcion != null && t.Descripcion.ToLower().Contains(term)));
            }

            var tipos = await query.OrderBy(t => t.NombreCategoria).ToListAsync();
            ViewBag.BusquedaActual = busqueda;

            return View(tipos);
        }

        // 2. CREAR (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 2. CREAR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombre_categoria, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre_categoria))
            {
                ModelState.AddModelError("NombreCategoria", "El nombre de la categoría es obligatorio.");
                return View();
            }

            var existe = await _context.TipoAtencions
                .AnyAsync(t => t.NombreCategoria.ToLower() == nombre_categoria.Trim().ToLower());

            if (existe)
            {
                TempData["Error"] = "Ya existe una categoría con ese nombre.";
                return View();
            }

            var nuevoTipo = new TipoAtencion
            {
                NombreCategoria = nombre_categoria.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim()
            };

            _context.TipoAtencions.Add(nuevoTipo);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de atención registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // 3. EDITAR (GET) - Abre la vista Edit.cshtml con los datos
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.TipoAtencions.FindAsync(id);
            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // 3. EDITAR (POST) - Procesa los cambios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id_tipo_atencion, string nombre_categoria, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre_categoria))
            {
                TempData["Error"] = "El nombre de la categoría es obligatorio.";
                var tipoError = await _context.TipoAtencions.FindAsync(id_tipo_atencion);
                return View(tipoError);
            }

            var tipo = await _context.TipoAtencions.FindAsync(id_tipo_atencion);
            if (tipo == null) return NotFound();

            tipo.NombreCategoria = nombre_categoria.Trim();
            tipo.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

            _context.TipoAtencions.Update(tipo);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de atención actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // 4. DETALLES (GET)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.TipoAtencions.AsNoTracking().FirstOrDefaultAsync(m => m.IdTipoAtencion == id);
            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // 5. ELIMINAR (GET) - Muestra la confirmación de borrado
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tipo = await _context.TipoAtencions.AsNoTracking().FirstOrDefaultAsync(m => m.IdTipoAtencion == id);
            if (tipo == null) return NotFound();

            return View(tipo);
        }

        // 5. ELIMINAR (POST) - Ejecuta el borrado
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipo = await _context.TipoAtencions.FindAsync(id);
            if (tipo != null)
            {
                bool enUso = await _context.AtencionDiaria.AnyAsync(a => a.IdTipoAtencion == id);
                if (enUso)
                {
                    TempData["Error"] = "No se puede eliminar la categoría porque ya tiene registros asociados en la bitácora.";
                    return RedirectToAction(nameof(Index));
                }

                _context.TipoAtencions.Remove(tipo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Categoría eliminada correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}