using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers;

/// <summary>
/// Controlador para la gestión y administración integral de Residentes en VITALIA.
/// Maneja el ciclo de vida del residente, asignación de habitaciones y validación de integridad referencial.
/// </summary>
public class ResidentesController : Controller
{
    private readonly VitaliaDbContext _context;

    public ResidentesController(VitaliaDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // A. LISTADO DE RESIDENTES
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, string? orden)
    {
        var query = _context.Residentes
            .Include(r => r.Habitacion)
            .AsNoTracking();

        // Búsqueda por Nombre o Apellido (LINQ en SQL Server)
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim();
            query = query.Where(r => r.Nombre.Contains(termino) || r.Apellido.Contains(termino));
        }

        // Ordenamiento por LINQ en base de datos
        query = orden switch
        {
            "nombre_desc" => query.OrderByDescending(r => r.Nombre).ThenByDescending(r => r.Apellido),
            "apellido_asc" => query.OrderBy(r => r.Apellido).ThenBy(r => r.Nombre),
            "ingreso_asc" => query.OrderBy(r => r.FechaIngreso),
            "ingreso_desc" => query.OrderByDescending(r => r.FechaIngreso),
            "habitacion_asc" => query.OrderBy(r => r.Habitacion != null ? r.Habitacion.Numero : string.Empty),
            _ => query.OrderBy(r => r.Nombre).ThenBy(r => r.Apellido) // Default: Nombre asc
        };

        var items = await query.Select(r => new ResidenteItemViewModel
        {
            IdResidente = r.IdResidente,
            Nombre = r.Nombre,
            Apellido = r.Apellido,
            FechaNacimiento = r.FechaNacimiento,
            Sexo = r.Sexo,
            Telefono = r.Telefono,
            Direccion = r.Direccion,
            FechaIngreso = r.FechaIngreso,
            IdHabitacion = r.IdHabitacion,
            NumeroHabitacion = r.Habitacion != null ? r.Habitacion.Numero : "Sin asignar",
            EstadoHabitacion = r.Habitacion != null ? r.Habitacion.Estado : "Desconocido"
        }).ToListAsync();

        var viewModel = new ResidentesIndexViewModel
        {
            Residentes = items,
            Busqueda = busqueda,
            OrdenActual = orden,
            TotalResidentes = items.Count,
            TotalHombres = items.Count(r => r.Sexo.Equals("Masculino", StringComparison.OrdinalIgnoreCase)),
            TotalMujeres = items.Count(r => r.Sexo.Equals("Femenino", StringComparison.OrdinalIgnoreCase))
        };

        return View(viewModel);
    }

    // ==========================================
    // B. REGISTRAR RESIDENTE
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ResidenteFormViewModel
        {
            FechaNacimiento = DateTime.Today.AddYears(-70),
            FechaIngreso = DateTime.Today,
            HabitacionesDisponibles = await ObtenerOpcionesHabitacionesAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResidenteFormViewModel model)
    {
        // Limpieza de campos de texto
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Apellido = model.Apellido?.Trim() ?? string.Empty;
        model.Sexo = model.Sexo?.Trim() ?? string.Empty;
        model.Telefono = string.IsNullOrWhiteSpace(model.Telefono) ? null : model.Telefono.Trim();
        model.Direccion = string.IsNullOrWhiteSpace(model.Direccion) ? null : model.Direccion.Trim();

        // Validaciones de negocio para fechas
        ValidarFechas(model.FechaNacimiento, model.FechaIngreso);

        // Validación de asignación de habitación en servidor
        var habitacion = await _context.Habitaciones
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.IdHabitacion == model.IdHabitacion);

        if (habitacion == null)
        {
            ModelState.AddModelError(nameof(model.IdHabitacion), "La habitación seleccionada no existe en el sistema.");
        }
        else
        {
            if (!habitacion.EstadoCoherente.Equals("Disponible", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.IdHabitacion), 
                    $"La habitación '{habitacion.Numero}' no está disponible para asignaciones (Estado operativo: '{habitacion.EstadoCoherente}').");
            }

            int residentesAsignados = habitacion.Residentes.Count;
            if (residentesAsignados >= habitacion.Capacidad)
            {
                ModelState.AddModelError(nameof(model.IdHabitacion), 
                    $"La habitación '{habitacion.Numero}' ya no cuenta con plazas disponibles (Capacidad: {habitacion.Capacidad}, Ocupadas: {residentesAsignados}).");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                var residente = new Residente
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    FechaNacimiento = model.FechaNacimiento.Date,
                    Sexo = model.Sexo,
                    Telefono = model.Telefono,
                    Direccion = model.Direccion,
                    FechaIngreso = model.FechaIngreso.Date,
                    IdHabitacion = model.IdHabitacion
                };

                _context.Residentes.Add(residente);

                // Sincronizar estado de la habitación según su nueva ocupación real
                int nuevosResidentes = habitacion!.Residentes.Count + 1;
                if (nuevosResidentes >= habitacion.Capacidad)
                {
                    habitacion.Estado = "Ocupada";
                }
                else
                {
                    habitacion.Estado = "Disponible";
                }

                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"Residente '{residente.Nombre} {residente.Apellido}' registrado exitosamente y asignado a la habitación {habitacion!.Numero}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar el residente en la base de datos: " + ex.GetBaseException().Message);
            }
        }

        model.HabitacionesDisponibles = await ObtenerOpcionesHabitacionesAsync();
        return View(model);
    }

    // ==========================================
    // C. CONSULTAR DETALLES
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var residente = await _context.Residentes
            .Include(r => r.Habitacion)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdResidente == id);

        if (residente == null)
        {
            return NotFound();
        }

        int plazasOcupadas = await _context.Residentes.CountAsync(r => r.IdHabitacion == residente.IdHabitacion);
        int capacidad = residente.Habitacion?.Capacidad ?? 0;

        var viewModel = new ResidenteDetalleViewModel
        {
            Residente = residente,
            PlazasOcupadasHabitacion = plazasOcupadas,
            PlazasLibresHabitacion = Math.Max(0, capacidad - plazasOcupadas),
            DependenciasBloqueantes = await VerificarDependenciasReferencialesAsync(residente.IdResidente)
        };

        return View(viewModel);
    }

    // ==========================================
    // D. EDITAR RESIDENTE
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var residente = await _context.Residentes
            .Include(r => r.Habitacion)
            .FirstOrDefaultAsync(r => r.IdResidente == id);

        if (residente == null)
        {
            return NotFound();
        }

        var model = new ResidenteFormViewModel
        {
            IdResidente = residente.IdResidente,
            Nombre = residente.Nombre,
            Apellido = residente.Apellido,
            FechaNacimiento = residente.FechaNacimiento,
            Sexo = residente.Sexo,
            Telefono = residente.Telefono,
            Direccion = residente.Direccion,
            FechaIngreso = residente.FechaIngreso,
            IdHabitacion = residente.IdHabitacion,
            HabitacionesDisponibles = await ObtenerOpcionesHabitacionesParaEdicionAsync(residente.IdResidente, residente.IdHabitacion)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResidenteFormViewModel model)
    {
        if (id != model.IdResidente)
        {
            return NotFound();
        }

        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Apellido = model.Apellido?.Trim() ?? string.Empty;
        model.Sexo = model.Sexo?.Trim() ?? string.Empty;
        model.Telefono = string.IsNullOrWhiteSpace(model.Telefono) ? null : model.Telefono.Trim();
        model.Direccion = string.IsNullOrWhiteSpace(model.Direccion) ? null : model.Direccion.Trim();

        ValidarFechas(model.FechaNacimiento, model.FechaIngreso);

        var residenteExistente = await _context.Residentes
            .Include(r => r.Habitacion)
            .FirstOrDefaultAsync(r => r.IdResidente == id);

        if (residenteExistente == null)
        {
            return NotFound();
        }

        // Validación de habitación seleccionada
        var habitacionDestino = await _context.Habitaciones
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.IdHabitacion == model.IdHabitacion);

        bool cambiaDeHabitacion = residenteExistente.IdHabitacion != model.IdHabitacion;

        if (habitacionDestino == null)
        {
            ModelState.AddModelError(nameof(model.IdHabitacion), "La habitación seleccionada no existe.");
        }
        else
        {
            // Al editar: no se cuenta al residente actual como plaza adicional si continúa en la misma habitación
            int ocupantesSinResidenteActual = habitacionDestino.Residentes.Count(r => r.IdResidente != id);

            if (ocupantesSinResidenteActual >= habitacionDestino.Capacidad)
            {
                ModelState.AddModelError(nameof(model.IdHabitacion), 
                    $"La habitación '{habitacionDestino.Numero}' no tiene plazas disponibles (Capacidad: {habitacionDestino.Capacidad}).");
            }

            // Si cambia a una habitación diferente, verificar que su estado operativo sea Disponible
            if (cambiaDeHabitacion && !habitacionDestino.EstadoCoherente.Equals("Disponible", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.IdHabitacion), 
                    $"La habitación '{habitacionDestino.Numero}' no está disponible para traslados (Estado operativo: '{habitacionDestino.EstadoCoherente}').");
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (cambiaDeHabitacion)
                {
                    // 1. Habitación origen: al salir el residente, verificar plazas restantes y sincronizar
                    int idHabitacionOrigen = residenteExistente.IdHabitacion;
                    var habitacionOrigen = await _context.Habitaciones
                        .Include(h => h.Residentes)
                        .FirstOrDefaultAsync(h => h.IdHabitacion == idHabitacionOrigen);

                    if (habitacionOrigen != null)
                    {
                        int restantesOrigen = habitacionOrigen.Residentes.Count(r => r.IdResidente != id);
                        if (restantesOrigen < habitacionOrigen.Capacidad)
                        {
                            if (habitacionOrigen.Estado != "Mantenimiento" && habitacionOrigen.Estado != "Inactiva")
                            {
                                habitacionOrigen.Estado = "Disponible";
                            }
                        }
                        else
                        {
                            habitacionOrigen.Estado = "Ocupada";
                        }
                    }

                    // 2. Habitación destino: al ingresar el residente, verificar si se llena y sincronizar
                    int nuevosEnDestino = habitacionDestino!.Residentes.Count(r => r.IdResidente != id) + 1;
                    if (nuevosEnDestino >= habitacionDestino.Capacidad)
                    {
                        habitacionDestino.Estado = "Ocupada";
                    }
                    else
                    {
                        habitacionDestino.Estado = "Disponible";
                    }
                }
                else
                {
                    // Misma habitación: sincronizar estado según la capacidad
                    int ocupantes = habitacionDestino!.Residentes.Count;
                    if (ocupantes >= habitacionDestino.Capacidad)
                    {
                        habitacionDestino.Estado = "Ocupada";
                    }
                    else if (habitacionDestino.Estado != "Mantenimiento" && habitacionDestino.Estado != "Inactiva")
                    {
                        habitacionDestino.Estado = "Disponible";
                    }
                }

                residenteExistente.Nombre = model.Nombre;
                residenteExistente.Apellido = model.Apellido;
                residenteExistente.FechaNacimiento = model.FechaNacimiento.Date;
                residenteExistente.Sexo = model.Sexo;
                residenteExistente.Telefono = model.Telefono;
                residenteExistente.Direccion = model.Direccion;
                residenteExistente.FechaIngreso = model.FechaIngreso.Date;
                residenteExistente.IdHabitacion = model.IdHabitacion;

                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"Datos del residente '{residenteExistente.Nombre} {residenteExistente.Apellido}' actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Residentes.AnyAsync(e => e.IdResidente == id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Error al actualizar en la base de datos: " + ex.GetBaseException().Message);
            }
        }

        model.HabitacionesDisponibles = await ObtenerOpcionesHabitacionesParaEdicionAsync(id, residenteExistente.IdHabitacion);
        return View(model);
    }

    // ==========================================
    // E. ELIMINACIÓN CON CONTROL DE INTEGRIDAD
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var residente = await _context.Residentes
            .Include(r => r.Habitacion)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdResidente == id);

        if (residente == null)
        {
            return NotFound();
        }

        var dependencias = await VerificarDependenciasReferencialesAsync(residente.IdResidente);

        var viewModel = new ResidenteDetalleViewModel
        {
            Residente = residente,
            DependenciasBloqueantes = dependencias
        };

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var residente = await _context.Residentes.FindAsync(id);
        if (residente == null)
        {
            return NotFound();
        }

        // Inspeccionar claves foráneas de tablas dependientes antes de proceder
        var dependencias = await VerificarDependenciasReferencialesAsync(id);

        if (dependencias.Count > 0)
        {
            string motivos = string.Join(", ", dependencias);
            TempData["MensajeError"] = $"Operación rechazada por integridad referencial: No se puede eliminar a '{residente.Nombre} {residente.Apellido}' porque tiene registros activos asociados: {motivos}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            int idHabitacion = residente.IdHabitacion;
            var habitacion = await _context.Habitaciones
                .Include(h => h.Residentes)
                .FirstOrDefaultAsync(h => h.IdHabitacion == idHabitacion);

            _context.Residentes.Remove(residente);

            // Si al retirarse el residente se libera plaza (o queda vacía), sincronizar a Disponible si no es Mantenimiento/Inactiva
            if (habitacion != null)
            {
                int restantes = habitacion.Residentes.Count(r => r.IdResidente != id);
                if (restantes < habitacion.Capacidad)
                {
                    if (habitacion.Estado != "Mantenimiento" && habitacion.Estado != "Inactiva")
                    {
                        habitacion.Estado = "Disponible";
                    }
                }
                else
                {
                    habitacion.Estado = "Ocupada";
                }
            }

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"El residente '{residente.Nombre} {residente.Apellido}' fue eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            TempData["MensajeError"] = "No se pudo eliminar el residente debido a restricciones de base de datos: " + ex.GetBaseException().Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // ==========================================
    // MÉTODOS AUXILIARES PRIVADOS
    // ==========================================

    private void ValidarFechas(DateTime fechaNacimiento, DateTime fechaIngreso)
    {
        if (fechaNacimiento.Date >= DateTime.Today)
        {
            ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento debe ser anterior a la fecha actual.");
        }
        else if (fechaNacimiento.Date < DateTime.Today.AddYears(-130))
        {
            ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento ingresada no es válida.");
        }

        if (fechaIngreso.Date < fechaNacimiento.Date)
        {
            ModelState.AddModelError("FechaIngreso", "La fecha de ingreso no puede ser anterior a la fecha de nacimiento.");
        }
    }

    private async Task<List<HabitacionOpcionViewModel>> ObtenerOpcionesHabitacionesAsync()
    {
        return await _context.Habitaciones
            .Include(h => h.Residentes)
            .AsNoTracking()
            .OrderBy(h => h.Numero)
            .Select(h => new HabitacionOpcionViewModel
            {
                IdHabitacion = h.IdHabitacion,
                Numero = h.Numero,
                Capacidad = h.Capacidad,
                Estado = h.Estado,
                ResidentesAsignados = h.Residentes.Count()
            })
            .ToListAsync();
    }

    private async Task<List<HabitacionOpcionViewModel>> ObtenerOpcionesHabitacionesParaEdicionAsync(int idResidente, int idHabitacionActual)
    {
        var habitaciones = await _context.Habitaciones
            .Include(h => h.Residentes)
            .AsNoTracking()
            .OrderBy(h => h.Numero)
            .ToListAsync();

        return habitaciones.Select(h =>
        {
            // Para la habitación actual del residente, no se le cuenta a él mismo como ocupante adicional
            int ocupantes = h.Residentes.Count(r => r.IdResidente != idResidente);
            return new HabitacionOpcionViewModel
            {
                IdHabitacion = h.IdHabitacion,
                Numero = h.Numero,
                Capacidad = h.Capacidad,
                Estado = h.Estado,
                ResidentesAsignados = ocupantes
            };
        }).ToList();
    }

    /// <summary>
    /// Inspecciona directamente las 5 tablas dependientes de Residentes en SQL Server
    /// para comprobar si existen registros asociados antes de permitir una eliminación.
    /// </summary>
    private async Task<List<string>> VerificarDependenciasReferencialesAsync(int idResidente)
    {
        var dependencias = new List<string>();
        var conn = _context.Database.GetDbConnection();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 'Familiares asociados' AS Tipo, COUNT(*) AS Cantidad FROM dbo.Familiares WHERE IdResidente = @id
            UNION ALL
            SELECT 'Medicamentos prescritos' AS Tipo, COUNT(*) AS Cantidad FROM dbo.Medicamentos WHERE IdResidente = @id
            UNION ALL
            SELECT 'Participación en actividades' AS Tipo, COUNT(*) AS Cantidad FROM dbo.ParticipacionActividades WHERE IdResidente = @id
            UNION ALL
            SELECT 'Incidentes registrados' AS Tipo, COUNT(*) AS Cantidad FROM dbo.Incidentes WHERE IdResidente = @id
            UNION ALL
            SELECT 'Visitas registradas' AS Tipo, COUNT(*) AS Cantidad FROM dbo.Visitas WHERE IdResidente = @id;
        ";

        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = idResidente;
        cmd.Parameters.Add(param);

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string tipo = reader.GetString(0);
            int cantidad = reader.GetInt32(1);
            if (cantidad > 0)
            {
                dependencias.Add($"{cantidad} {tipo.ToLower()}");
            }
        }

        return dependencias;
    }
}
