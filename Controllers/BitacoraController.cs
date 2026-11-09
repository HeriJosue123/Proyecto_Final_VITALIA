using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    // 1. Permitimos la entrada general al Farmacéutico junto con el personal clínico
    [Authorize(Roles = "Administrador, Medico, Médico, Enfermero, Farmacéutico, Farmaceutico")]
    public class BitacoraController : Controller
    {
        private readonly VitaliaDbContext _context;

        public BitacoraController(VitaliaDbContext context)
        {
            _context = context;
        }

        // 1. VISTA PRINCIPAL CON FILTROS Y BÚSQUEDA
        public async Task<IActionResult> Index(int? idResidente, int? idTipoAtencion, DateTime? fechaInicio, DateTime? fechaFin, string busqueda)
        {
            var query = _context.AtencionDiaria
                .AsNoTracking()
                .Include(a => a.IdEmpleadoNavigation)
                .Include(a => a.IdTipoAtencionNavigation) // Incluido para poder filtrar por categoría
                .AsQueryable();

            // 2. REGLA PARA FARMACÉUTICO: Forzar filtro exclusivo de Medicamentos
            bool esFarmaceutico = User.IsInRole("Farmacéutico") || User.IsInRole("Farmaceutico");
            if (esFarmaceutico)
            {
                // Filtra únicamente las atenciones donde el nombre de la categoría sea "Medicamentos"
                query = query.Where(a => a.IdTipoAtencionNavigation != null &&
                                         a.IdTipoAtencionNavigation.NombreCategoria.ToLower().Contains("medicamento"));
            }

            if (idResidente.HasValue && idResidente.Value > 0)
            {
                query = query.Where(a => a.IdResidente == idResidente.Value);
            }

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(a => a.Observaciones.Contains(busqueda) ||
                                         a.IdResidente.ToString().Contains(busqueda) ||
                                         a.IdAtencion.ToString().Contains(busqueda));
            }

            if (idTipoAtencion.HasValue && idTipoAtencion.Value > 0)
            {
                query = query.Where(a => a.IdTipoAtencion == idTipoAtencion.Value);
            }

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

            // Cargar lista dinámica de residentes
            ViewBag.Residentes = await _context.Residentes
                .AsNoTracking()
                .Where(r => r.Estado)
                .OrderBy(r => r.Apellido).ThenBy(r => r.Nombre)
                .Select(r => new SelectListItem
                {
                    Value = r.IdResidente.ToString(),
                    Text = $"{r.CodigoUnico} · {r.Nombre} {r.Apellido}"
                })
                .ToListAsync();

            // Cargar lista dinámica de empleados
            ViewBag.Empleados = await _context.Empleados
                .AsNoTracking()
                .Where(e => e.Estado)
                .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.IdEmpleado.ToString(),
                    Text = $"{e.Nombre} {e.Apellido} ({e.Cargo})"
                })
                .ToListAsync();

            // Cargar lista de tipos de atención reales desde la BD
            ViewBag.TiposAtencion = await _context.TipoAtencions
                .AsNoTracking()
                .OrderBy(t => t.NombreCategoria)
                .Select(t => new SelectListItem
                {
                    Value = t.IdTipoAtencion.ToString(),
                    Text = t.NombreCategoria
                })
                .ToListAsync();

            ViewBag.IdResidenteSeleccionado = idResidente;
            ViewBag.IdTipoAtencionSeleccionado = idTipoAtencion;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.BusquedaActual = busqueda;

            return View(atenciones);
        }

        // 2. REGISTRAR NUEVO TIPO DE ATENCIÓN EN LA BD
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Medico, Médico, Enfermero")]
        public async Task<IActionResult> CrearTipoAtencion(string nombre_categoria, string? descripcion)
        {
            if (!string.IsNullOrWhiteSpace(nombre_categoria))
            {
                var existe = await _context.TipoAtencions
                    .AnyAsync(t => t.NombreCategoria.ToLower() == nombre_categoria.Trim().ToLower());

                if (!existe)
                {
                    var nuevoTipo = new TipoAtencion
                    {
                        NombreCategoria = nombre_categoria.Trim(),
                        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim()
                    };

                    _context.TipoAtencions.Add(nuevoTipo);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 3. CREAR NUEVA ATENCIÓN EN LA BITÁCORA
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Medico, Médico, Enfermero")]
        public async Task<IActionResult> Registrar(int id_residente, int id_empleado, int id_tipo_atencion, string observaciones)
        {
            if (id_residente > 0 && id_empleado > 0 && id_tipo_atencion > 0 && !string.IsNullOrWhiteSpace(observaciones))
            {
                DateTime ahora = DateTime.Now;

                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO dbo.ATENCION_DIARIA (id_residente, id_tipo_atencion, observaciones, fecha_hora, id_empleado, estado_cumplimiento) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                    id_residente, id_tipo_atencion, observaciones.Trim(), ahora, id_empleado, "Completado");
            }

            return RedirectToAction(nameof(Index));
        }

        // 4. EDITAR ATENCIÓN EXISTENTE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Medico, Médico, Enfermero")]
        public async Task<IActionResult> Editar(int id_atencion, int id_tipo_atencion, string observaciones)
        {
            if (id_atencion > 0 && id_tipo_atencion > 0 && !string.IsNullOrWhiteSpace(observaciones))
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE dbo.ATENCION_DIARIA SET id_tipo_atencion = {0}, observaciones = {1} WHERE id_atencion = {2}",
                    id_tipo_atencion, observaciones.Trim(), id_atencion);
            }

            return RedirectToAction(nameof(Index));
        }

        // 5. ELIMINAR ATENCIÓN
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Medico, Médico, Enfermero")]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (id > 0)
            {
                int filasAfectadas = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM dbo.ATENCION_DIARIA WHERE id_atencion = {0}", id);

                if (filasAfectadas > 0)
                {
                    TempData["MensajeExito"] = "La atención se ha eliminado correctamente.";
                }
                else
                {
                    TempData["MensajeError"] = "No se pudo encontrar el registro que intentas eliminar.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. EXPORTAR PDF REAL CON ROTATIVA
        public async Task<IActionResult> ExportarPdf(int? idResidente, DateTime? fechaInicio, DateTime? fechaFin, int? idTipoAtencion, string busqueda)
        {
            var query = _context.AtencionDiaria
                .AsNoTracking()
                .Include(a => a.IdResidenteNavigation)
                .Include(a => a.IdEmpleadoNavigation)
                .Include(a => a.IdTipoAtencionNavigation)
                .AsQueryable();

            // REGLA PARA FARMACÉUTICO EN EL PDF: Forzar también el filtro exclusivo
            bool esFarmaceutico = User.IsInRole("Farmacéutico") || User.IsInRole("Farmaceutico");
            if (esFarmaceutico)
            {
                query = query.Where(a => a.IdTipoAtencionNavigation != null &&
                                         a.IdTipoAtencionNavigation.NombreCategoria.ToLower().Contains("medicamento"));
            }

            if (idResidente.HasValue && idResidente.Value > 0)
            {
                query = query.Where(a => a.IdResidente == idResidente.Value);
            }

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(a => a.Observaciones.Contains(busqueda) ||
                                         a.IdResidente.ToString().Contains(busqueda) ||
                                         a.IdAtencion.ToString().Contains(busqueda));
            }

            if (idTipoAtencion.HasValue && idTipoAtencion.Value > 0)
            {
                query = query.Where(a => a.IdTipoAtencion == idTipoAtencion.Value);
            }

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

            return new Rotativa.AspNetCore.ViewAsPdf("PdfExportar", atenciones)
            {
                FileName = $"Bitacora_Atenciones_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape
            };
        }
    }
}