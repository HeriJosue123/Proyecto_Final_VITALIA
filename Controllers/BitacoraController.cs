using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    public class BitacoraController : Controller
    {
        private readonly VitaliaDbContext _context;

        public BitacoraController(VitaliaDbContext context)
        {
            _context = context;
        }

        // 1. VISTA PRINCIPAL CON FILTROS Y BÚSQUEDA
        public async Task<IActionResult> Index(int? idTipoAtencion, DateTime? fechaInicio, DateTime? fechaFin, string busqueda)
        {
            var query = _context.AtencionDiaria.AsNoTracking().AsQueryable();

            // Búsqueda por texto (Observaciones o IDs)
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(a => a.Observaciones.Contains(busqueda) ||
                                         a.IdResidente.ToString().Contains(busqueda) ||
                                         a.IdAtencion.ToString().Contains(busqueda));
            }

            // Filtro por Tipo de Atención
            if (idTipoAtencion.HasValue && idTipoAtencion.Value > 0)
            {
                query = query.Where(a => a.IdTipoAtencion == idTipoAtencion.Value);
            }

            // Filtro por Fechas
            if (fechaInicio.HasValue)
            {
                query = query.Where(a => a.FechaHora >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                var finDelDia = fechaFin.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.FechaHora <= finDelDia);
            }

            var atenciones = await query
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();

            ViewBag.IdTipoAtencionSeleccionado = idTipoAtencion;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.BusquedaActual = busqueda;

            return View(atenciones);
        }

        // 2. CREAR NUEVA ATENCIÓN (SQL DIRECTO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(int id_residente, int id_tipo_atencion, string observaciones)
        {
            if (!string.IsNullOrEmpty(observaciones))
            {
                int idResidenteFinal = id_residente <= 0 ? 1 : id_residente;
                int idTipoFinal = id_tipo_atencion <= 0 ? 1 : id_tipo_atencion;
                DateTime ahora = DateTime.Now;

                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO dbo.ATENCION_DIARIA (id_residente, id_tipo_atencion, observaciones, fecha_hora, id_empleado, estado_cumplimiento) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                    idResidenteFinal, idTipoFinal, observaciones, ahora, 1, "Completado");
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. EDITAR ATENCIÓN EXISTENTE (SQL DIRECTO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id_atencion, int id_tipo_atencion, string observaciones)
        {
            if (id_atencion > 0 && !string.IsNullOrEmpty(observaciones))
            {
                int idTipoFinal = id_tipo_atencion <= 0 ? 1 : id_tipo_atencion;

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE dbo.ATENCION_DIARIA SET id_tipo_atencion = {0}, observaciones = {1} WHERE id_atencion = {2}",
                    idTipoFinal, observaciones, id_atencion);
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. ELIMINAR ATENCIÓN (SQL DIRECTO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (id > 0)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM dbo.ATENCION_DIARIA WHERE id_atencion = {0}",
                    id);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}