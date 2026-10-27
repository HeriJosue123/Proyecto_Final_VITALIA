using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers;

/// <summary>
/// Módulo de Alertas y Tareas (RF-014, RF-015, RF-016 / RN-006 / CA-007).
/// Tablero por estado: Pendiente -> En Proceso -> Atendida.
/// </summary>
public class AlertasController : Controller
{
    private readonly VitaliaDbContext _context;
    private readonly ILogger<AlertasController> _logger;

    public AlertasController(VitaliaDbContext context, ILogger<AlertasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ---------------------------------------------------------------
    // GET /Alertas  -> tablero con filtros por residente y prioridad (RF-016)
    // ---------------------------------------------------------------
    public async Task<IActionResult> Index(int? idResidente, string? prioridad)
    {
        var query = _context.Alerta
            .AsNoTracking()
            .Include(a => a.IdResidenteNavigation)
            .Include(a => a.IdUsuarioCreadorNavigation)
            .Include(a => a.EmpleadoResponsable)
            .AsQueryable();

        if (idResidente.HasValue && idResidente.Value > 0)
        {
            query = query.Where(a => a.IdResidente == idResidente.Value);
        }

        if (!string.IsNullOrWhiteSpace(prioridad) && AlertaPrioridades.Todas.Contains(prioridad))
        {
            query = query.Where(a => a.Prioridad == prioridad);
        }
        else
        {
            prioridad = null;
        }

        var alertas = await query.ToListAsync();

        // Sello de cierre de las alertas Atendidas: fecha/hora y responsable (CA-007)
        var idsAtendidas = alertas
            .Where(a => a.EstadoActual == AlertaEstados.Atendida)
            .Select(a => a.IdAlerta)
            .ToList();

        var cierres = await _context.HistorialEstadoAlerta
            .AsNoTracking()
            .Include(h => h.IdUsuarioNavigation)
            .Where(h => idsAtendidas.Contains(h.IdAlerta) && h.EstadoNuevo == AlertaEstados.Atendida)
            .ToListAsync();

        var cierrePorAlerta = cierres
            .GroupBy(h => h.IdAlerta)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.FechaHora).First());

        var tarjetas = alertas.Select(a =>
        {
            cierrePorAlerta.TryGetValue(a.IdAlerta, out var cierre);
            return new AlertaTarjetaViewModel
            {
                IdAlerta = a.IdAlerta,
                Titulo = a.Titulo,
                Descripcion = a.Descripcion,
                Prioridad = a.Prioridad,
                Estado = a.EstadoActual,
                Residente = $"{a.IdResidenteNavigation.Nombre} {a.IdResidenteNavigation.Apellido}",
                CodigoResidente = a.IdResidenteNavigation.CodigoUnico,
                FechaProgramada = a.FechaProgramada,
                CreadaPor = a.IdUsuarioCreadorNavigation.Correo,
                Responsable = a.EmpleadoResponsable == null
                    ? null
                    : $"{a.EmpleadoResponsable.Nombre} {a.EmpleadoResponsable.Apellido}",
                FechaCierre = cierre?.FechaHora,
                CerradaPor = cierre?.IdUsuarioNavigation.Correo
            };
        }).ToList();

        var modelo = new AlertasTableroViewModel
        {
            // Urgente primero y, dentro de cada prioridad, la más próxima primero
            Pendientes = tarjetas
                .Where(t => t.Estado == AlertaEstados.Pendiente)
                .OrderBy(t => AlertaPrioridades.Orden(t.Prioridad)).ThenBy(t => t.FechaProgramada).ToList(),
            EnProceso = tarjetas
                .Where(t => t.Estado == AlertaEstados.EnProceso)
                .OrderBy(t => AlertaPrioridades.Orden(t.Prioridad)).ThenBy(t => t.FechaProgramada).ToList(),
            Atendidas = tarjetas
                .Where(t => t.Estado == AlertaEstados.Atendida)
                .OrderByDescending(t => t.FechaCierre ?? t.FechaProgramada).ToList(),
            FiltroResidente = idResidente,
            FiltroPrioridad = prioridad
        };

        await CargarListasAsync(modelo);
        return View(modelo);
    }

    // ---------------------------------------------------------------
    // POST /Alertas/Crear  -> nueva alerta, siempre en estado Pendiente (RF-014)
    // ---------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        int idResidente,
        string? titulo,
        string? descripcion,
        string? prioridad,
        DateTime? fechaProgramada,
        int? idEmpleadoResponsable)
    {
        titulo = titulo?.Trim();
        descripcion = descripcion?.Trim();

        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(titulo))
            errores.Add("El título es obligatorio.");
        else if (titulo.Length > 150)
            errores.Add("El título no puede superar los 150 caracteres.");

        if (string.IsNullOrWhiteSpace(descripcion))
            errores.Add("La descripción es obligatoria.");

        if (string.IsNullOrWhiteSpace(prioridad) || !AlertaPrioridades.Todas.Contains(prioridad))
            errores.Add("Seleccione una prioridad: Urgente, Programada o Rutina.");

        if (fechaProgramada is null)
            errores.Add("La fecha y hora programada es obligatoria.");

        var residenteValido = await _context.Residentes
            .AnyAsync(r => r.IdResidente == idResidente && r.Estado);
        if (!residenteValido)
            errores.Add("Seleccione un residente activo.");

        if (idEmpleadoResponsable.HasValue)
        {
            var empleadoValido = await _context.Empleados
                .AnyAsync(e => e.IdEmpleado == idEmpleadoResponsable.Value && e.Estado);
            if (!empleadoValido)
                errores.Add("El responsable seleccionado no existe o está inactivo.");
        }

        if (errores.Count > 0)
        {
            TempData["AlertaErrores"] = string.Join("|", errores);
            return RedirectToAction(nameof(Index));
        }

        var usuario = await ObtenerUsuarioActualAsync();
        if (usuario == null)
        {
            TempData["MensajeError"] = "No hay un usuario activo en el sistema para registrar la alerta.";
            return RedirectToAction(nameof(Index));
        }

        var alerta = new Alertum
        {
            IdResidente = idResidente,
            IdUsuarioCreador = usuario.IdUsuario,
            Titulo = titulo!,
            Descripcion = descripcion!,
            Prioridad = prioridad!,
            FechaProgramada = fechaProgramada!.Value,
            EstadoActual = AlertaEstados.Pendiente,
            IdEmpleadoResponsable = idEmpleadoResponsable
        };

        try
        {
            _context.Alerta.Add(alerta);
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"Alerta «{alerta.Titulo}» creada en estado Pendiente.";
        }
        catch (Exception ex)
        {
            // RNF-006: mensaje controlado al usuario, detalle técnico solo en el log
            _logger.LogError(ex, "Error al crear la alerta.");
            TempData["MensajeError"] = "No se pudo guardar la alerta. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // POST /Alertas/CambiarEstado -> avanza UN paso: Pendiente -> En Proceso -> Atendida
    // Registra usuario y fecha/hora del servidor en HISTORIAL_ESTADO_ALERTA (RF-015, CA-007)
    // ---------------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string? estadoEsperado)
    {
        var alerta = await _context.Alerta.FirstOrDefaultAsync(a => a.IdAlerta == id);
        if (alerta == null)
        {
            return NotFound();
        }

        // Evita doble clic / pestañas desactualizadas: el estado debe ser el que el usuario veía
        if (!string.Equals(alerta.EstadoActual, estadoEsperado, StringComparison.Ordinal))
        {
            TempData["MensajeError"] = $"La alerta «{alerta.Titulo}» ya cambió de estado. Se actualizó el tablero.";
            return RedirectToAction(nameof(Index));
        }

        var siguiente = AlertaEstados.Siguiente(alerta.EstadoActual);
        if (siguiente == null)
        {
            TempData["MensajeError"] = "Una alerta Atendida no puede volver a abrirse (RN-006).";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await ObtenerUsuarioActualAsync();
        if (usuario == null)
        {
            TempData["MensajeError"] = "No hay un usuario activo en el sistema para registrar el cambio.";
            return RedirectToAction(nameof(Index));
        }

        var anterior = alerta.EstadoActual;
        alerta.EstadoActual = siguiente;

        _context.HistorialEstadoAlerta.Add(new HistorialEstadoAlertum
        {
            IdAlerta = alerta.IdAlerta,
            IdUsuario = usuario.IdUsuario,
            EstadoAnterior = anterior,
            EstadoNuevo = siguiente,
            FechaHora = DateTime.Now   // sello del servidor, no editable desde la interfaz
        });

        try
        {
            // Un solo SaveChanges = una sola transacción: o se guardan ambos cambios o ninguno
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"Alerta «{alerta.Titulo}»: {anterior} → {siguiente}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar el estado de la alerta {IdAlerta}.", id);
            TempData["MensajeError"] = "No se pudo actualizar el estado. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // Auxiliares
    // ---------------------------------------------------------------

    /// <summary>
    /// Todavía no hay inicio de sesión (RF-001 pendiente). Mientras tanto se usa el primer
    /// Administrador activo; si no existe, el primer usuario activo. Reemplazar por el usuario
    /// autenticado cuando se integre el login.
    /// </summary>
    private async Task<Usuario?> ObtenerUsuarioActualAsync()
    {
        var admin = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Estado && u.IdRolNavigation.NombreRol == "Administrador")
            .OrderBy(u => u.IdUsuario)
            .FirstOrDefaultAsync();

        if (admin != null) return admin;

        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Estado)
            .OrderBy(u => u.IdUsuario)
            .FirstOrDefaultAsync();
    }

    private async Task CargarListasAsync(AlertasTableroViewModel modelo)
    {
        modelo.Residentes = await _context.Residentes
            .AsNoTracking()
            .Where(r => r.Estado)
            .OrderBy(r => r.Apellido).ThenBy(r => r.Nombre)
            .Select(r => new SelectListItem
            {
                Value = r.IdResidente.ToString(),
                Text = r.CodigoUnico + " · " + r.Nombre + " " + r.Apellido
            })
            .ToListAsync();

        modelo.Empleados = await _context.Empleados
            .AsNoTracking()
            .Where(e => e.Estado)
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .Select(e => new SelectListItem
            {
                Value = e.IdEmpleado.ToString(),
                Text = e.Nombre + " " + e.Apellido + " (" + e.Cargo + ")"
            })
            .ToListAsync();
    }
}
