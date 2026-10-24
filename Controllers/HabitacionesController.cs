using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers;

/// <summary>
/// Controlador para la gestión y administración de Habitaciones en VITALIA.
/// Responsable del CRUD, control de capacidad, plazas disponibles y estados.
/// </summary>
public class HabitacionesController : Controller
{
    private readonly VitaliaDbContext _context;

    // Estados predefinidos estándar sugeridos
    private static readonly List<string> EstadosEstandar = new()
    {
        "Disponible",
        "Ocupada",
        "Mantenimiento",
        "Inactiva"
    };

    public HabitacionesController(VitaliaDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // A. LISTADO DE HABITACIONES (CON BÚSQUEDA Y FILTROS)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, string? estado, string? orden)
    {
        // Consulta base proyectada directamente en SQL Server (LINQ) sin cargar toda la tabla
        var query = _context.Habitaciones.AsNoTracking();

        // Filtro por término de búsqueda (Número de habitación)
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim();
            query = query.Where(h => h.Numero.Contains(termino));
        }

        // Proyección eficiente que contabiliza residentes asociados mediante subconsulta SQL
        var items = await query.Select(h => new HabitacionItemViewModel
        {
            IdHabitacion = h.IdHabitacion,
            Numero = h.Numero,
            Capacidad = h.Capacidad,
            Estado = h.Estado,
            ResidentesAsignados = h.Residentes.Count()
        }).ToListAsync();

        // Si hay filtro por estado, aplicar sobre el estado coherente para garantizar consistencia operativa
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string estadoFiltro = estado.Trim();
            items = items.Where(h => h.EstadoCoherente.Equals(estadoFiltro, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Ordenamiento consistente
        items = orden switch
        {
            "numero_desc" => items.OrderByDescending(h => h.Numero).ToList(),
            "capacidad_asc" => items.OrderBy(h => h.Capacidad).ThenBy(h => h.Numero).ToList(),
            "capacidad_desc" => items.OrderByDescending(h => h.Capacidad).ThenBy(h => h.Numero).ToList(),
            "estado_asc" => items.OrderBy(h => h.EstadoCoherente).ThenBy(h => h.Numero).ToList(),
            _ => items.OrderBy(h => h.Numero).ToList() // Default: orden por número asc
        };

        // Lista fija de estados estándar disponibles para el filtro
        var listaEstados = EstadosEstandar.ToList();

        // Calcular métricas del listado actual
        var viewModel = new HabitacionesIndexViewModel
        {
            Habitaciones = items,
            Busqueda = busqueda,
            FiltroEstado = estado,
            OrdenActual = orden,
            EstadosDisponibles = listaEstados,
            TotalHabitaciones = items.Count,
            TotalCapacidad = items.Sum(h => h.Capacidad),
            TotalResidentesAsignados = items.Sum(h => h.ResidentesAsignados),
            TotalPlazasLibres = items.Sum(h => h.PlazasDisponibles)
        };

        return View(viewModel);
    }

    // ==========================================
    // B. REGISTRAR HABITACIÓN
    // ==========================================
    [HttpGet]
    public IActionResult Create()
    {
        // Una habitación nueva tiene 0 residentes, no puede crearse como 'Ocupada'
        ViewBag.EstadosDisponibles = new List<string> { "Disponible", "Mantenimiento", "Inactiva" };
        var modeloInicial = new Habitacion
        {
            Capacidad = 1,
            Estado = "Disponible"
        };
        return View(modeloInicial);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Habitacion habitacion)
    {
        // Limpieza de espacios
        habitacion.Numero = habitacion.Numero?.Trim() ?? string.Empty;
        habitacion.Estado = habitacion.Estado?.Trim() ?? "Disponible";

        // Validación 1: Capacidad mayor a cero
        if (habitacion.Capacidad <= 0)
        {
            ModelState.AddModelError(nameof(habitacion.Capacidad), "La capacidad de la habitación debe ser mayor que cero.");
        }

        // Validación 2: Número único (respetando reglas de negocio)
        if (!string.IsNullOrEmpty(habitacion.Numero))
        {
            bool numeroDuplicado = await _context.Habitaciones
                .AnyAsync(h => h.Numero.ToLower() == habitacion.Numero.ToLower());

            if (numeroDuplicado)
            {
                ModelState.AddModelError(nameof(habitacion.Numero), $"Ya existe una habitación registrada con el número '{habitacion.Numero}'.");
            }
        }

        // Validación 3: Una habitación nueva con 0 residentes no puede registrarse como Ocupada
        if (string.Equals(habitacion.Estado, "Ocupada", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(habitacion.Estado), 
                "Una habitación nueva no puede registrarse como 'Ocupada' porque no tiene residentes asignados. El estado inicial debe ser 'Disponible', 'Mantenimiento' o 'Inactiva'.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(habitacion);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"Habitación '{habitacion.Numero}' registrada exitosamente con capacidad para {habitacion.Capacidad} residente(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar en la base de datos: " + ex.GetBaseException().Message);
            }
        }

        ViewBag.EstadosDisponibles = new List<string> { "Disponible", "Mantenimiento", "Inactiva" };
        return View(habitacion);
    }

    // ==========================================
    // C. EDITAR HABITACIÓN
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var habitacion = await _context.Habitaciones
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.IdHabitacion == id);

        if (habitacion == null)
        {
            return NotFound();
        }

        ViewBag.ResidentesAsignados = habitacion.Residentes.Count;
        ViewBag.EstadosDisponibles = EstadosEstandar;
        return View(habitacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Habitacion habitacion)
    {
        if (id != habitacion.IdHabitacion)
        {
            return NotFound();
        }

        habitacion.Numero = habitacion.Numero?.Trim() ?? string.Empty;
        habitacion.Estado = habitacion.Estado?.Trim() ?? "Disponible";

        // Validación: Capacidad mayor a cero
        if (habitacion.Capacidad <= 0)
        {
            ModelState.AddModelError(nameof(habitacion.Capacidad), "La capacidad de la habitación debe ser mayor que cero.");
        }

        // Obtener residentes asignados actualmente para validar capacidad
        int residentesActuales = await _context.Residentes.CountAsync(r => r.IdHabitacion == id);

        if (habitacion.Capacidad < residentesActuales)
        {
            ModelState.AddModelError(nameof(habitacion.Capacidad), 
                $"La nueva capacidad ({habitacion.Capacidad}) no puede ser menor a la cantidad de residentes actualmente asignados ({residentesActuales}).");
        }

        // Validaciones estrictas de estado según las reglas de negocio de ocupación
        if (residentesActuales == 0 && string.Equals(habitacion.Estado, "Ocupada", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(habitacion.Estado), 
                "No se permite marcar como 'Ocupada' una habitación que tiene 0 residentes asignados.");
        }
        else if (residentesActuales >= habitacion.Capacidad && string.Equals(habitacion.Estado, "Disponible", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(habitacion.Estado), 
                $"No se permite marcar como 'Disponible' una habitación que ha alcanzado su capacidad máxima ({residentesActuales}/{habitacion.Capacidad}). Debe liberar plazas antes de habilitarla.");
        }
        else if (residentesActuales > 0 && residentesActuales < habitacion.Capacidad && string.Equals(habitacion.Estado, "Ocupada", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(habitacion.Estado), 
                $"No se permite marcar como 'Ocupada' una habitación que aún cuenta con {habitacion.Capacidad - residentesActuales} plaza(s) disponible(s).");
        }
        else if (residentesActuales > 0 && (string.Equals(habitacion.Estado, "Mantenimiento", StringComparison.OrdinalIgnoreCase) || string.Equals(habitacion.Estado, "Inactiva", StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(nameof(habitacion.Estado), 
                $"No se puede cambiar a '{habitacion.Estado}' una habitación con residentes asignados ({residentesActuales}). Reubique a los residentes antes de inactivar la habitación.");
        }

        // Validación: Número no duplicado con otra habitación
        bool numeroDuplicado = await _context.Habitaciones
            .AnyAsync(h => h.Numero.ToLower() == habitacion.Numero.ToLower() && h.IdHabitacion != id);

        if (numeroDuplicado)
        {
            ModelState.AddModelError(nameof(habitacion.Numero), $"El número '{habitacion.Numero}' ya está en uso por otra habitación.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                var habExistente = await _context.Habitaciones.FindAsync(id);
                if (habExistente == null)
                {
                    return NotFound();
                }

                // Modificar únicamente los campos autorizados sin tocar entidades relacionadas
                habExistente.Numero = habitacion.Numero;
                habExistente.Capacidad = habitacion.Capacidad;
                habExistente.Estado = habitacion.Estado;

                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"Habitación '{habExistente.Numero}' actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Habitaciones.AnyAsync(e => e.IdHabitacion == id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Error al actualizar la base de datos: " + ex.GetBaseException().Message);
            }
        }

        ViewBag.ResidentesAsignados = residentesActuales;
        ViewBag.EstadosDisponibles = EstadosEstandar;
        return View(habitacion);
    }

    // ==========================================
    // D. CONSULTAR DETALLES
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var habitacion = await _context.Habitaciones
            .Include(h => h.Residentes)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.IdHabitacion == id);

        if (habitacion == null)
        {
            return NotFound();
        }

        return View(habitacion);
    }

    // ==========================================
    // E. CAMBIAR ESTADO (OPERACIÓN CONTROLADA)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> CambiarEstado(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var habitacion = await _context.Habitaciones
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.IdHabitacion == id);

        if (habitacion == null)
        {
            return NotFound();
        }

        var model = new CambiarEstadoViewModel
        {
            IdHabitacion = habitacion.IdHabitacion,
            Numero = habitacion.Numero,
            Capacidad = habitacion.Capacidad,
            EstadoActual = habitacion.Estado,
            ResidentesAsignados = habitacion.Residentes.Count,
            NuevoEstado = habitacion.EstadoCoherente
        };

        ViewBag.EstadosDisponibles = EstadosEstandar;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(CambiarEstadoViewModel model)
    {
        var habitacion = await _context.Habitaciones
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.IdHabitacion == model.IdHabitacion);

        if (habitacion == null)
        {
            return NotFound();
        }

        model.Numero = habitacion.Numero;
        model.Capacidad = habitacion.Capacidad;
        model.EstadoActual = habitacion.Estado;
        model.ResidentesAsignados = habitacion.Residentes.Count;

        if (string.IsNullOrWhiteSpace(model.NuevoEstado))
        {
            ModelState.AddModelError(nameof(model.NuevoEstado), "Debe especificar un estado válido.");
        }
        else
        {
            string nuevo = model.NuevoEstado.Trim();

            // Validación estricta con las reglas de negocio de ocupación
            if (!model.EsEstadoPermitido(nuevo, out string? motivoRechazo))
            {
                ModelState.AddModelError(nameof(model.NuevoEstado), motivoRechazo!);
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                habitacion.Estado = model.NuevoEstado.Trim();
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"El estado de la habitación '{habitacion.Numero}' se actualizó de '{model.EstadoActual}' a '{habitacion.Estado}'.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Error al actualizar estado en la base de datos: " + ex.GetBaseException().Message);
            }
        }

        ViewBag.EstadosDisponibles = EstadosEstandar;
        return View(model);
    }

    // ==========================================
    // G. ELIMINACIÓN SEGURA (VALIDA QUE NO TENGA RESIDENTES)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var habitacion = await _context.Habitaciones
            .Include(h => h.Residentes)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.IdHabitacion == id);

        if (habitacion == null)
        {
            return NotFound();
        }

        return View(habitacion);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Regla estricta: No eliminar habitaciones que tengan residentes asignados
        int residentesAsignados = await _context.Residentes.CountAsync(r => r.IdHabitacion == id);

        if (residentesAsignados > 0)
        {
            TempData["MensajeError"] = $"Operación rechazada: La habitación no puede eliminarse porque tiene {residentesAsignados} residente(s) asignado(s).";
            return RedirectToAction(nameof(Details), new { id });
        }

        var habitacion = await _context.Habitaciones.FindAsync(id);
        if (habitacion == null)
        {
            return NotFound();
        }

        try
        {
            _context.Habitaciones.Remove(habitacion);
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"La habitación '{habitacion.Numero}' ha sido eliminada exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            TempData["MensajeError"] = "No se pudo eliminar la habitación debido a restricciones de integridad: " + ex.GetBaseException().Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
