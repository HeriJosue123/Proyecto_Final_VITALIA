using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers;

public class AlertasController : Controller
{
    private readonly VitaliaDbContext _context;
    private readonly ILogger<AlertasController> _logger;

    public AlertasController(VitaliaDbContext context, ILogger<AlertasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Alertas
    public async Task<IActionResult> Index(int? idResidente, string? prioridad)
    {
        var query = _context.Alerta
            .Include(a => a.IdResidenteNavigation)
            .Include(a => a.IdUsuarioCreadorNavigation)
            .Include(a => a.IdEmpleadoResponsableNavigation)
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
                Responsable = a.IdEmpleadoResponsableNavigation == null
                    ? null
                    : $"{a.IdEmpleadoResponsableNavigation.Nombre} {a.IdEmpleadoResponsableNavigation.Apellido}",
                FechaCierre = cierre?.FechaHora,
                CerradaPor = cierre?.IdUsuarioNavigation.Correo
            };
        }).ToList();

        var modelo = new AlertasTableroViewModel
        {
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

    // GET: Alertas/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var detalle = await ConstruirDetalleAsync(id.Value);
        if (detalle == null) return NotFound();

        return View(detalle);
    }

    // GET: Alertas/Create
    public async Task<IActionResult> Create()
    {
        var modelo = new AlertaFormViewModel
        {
            Prioridad = AlertaPrioridades.Rutina,
            FechaProgramada = DateTime.Now
        };

        await CargarListasFormularioAsync(modelo);
        return View(modelo);
    }

    // POST: Alertas/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AlertaFormViewModel modelo)
    {
        modelo.Titulo = modelo.Titulo?.Trim() ?? string.Empty;
        modelo.Descripcion = modelo.Descripcion?.Trim() ?? string.Empty;

        await ValidarReglasAsync(modelo, esEdicion: false, empleadoOriginal: null);

        if (!ModelState.IsValid)
        {
            await CargarListasFormularioAsync(modelo);
            return View(modelo);
        }

        var usuario = await ObtenerUsuarioActualAsync();
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "No hay un usuario activo en el sistema para registrar la alerta.");
            await CargarListasFormularioAsync(modelo);
            return View(modelo);
        }

        var alerta = new Alerta
        {
            IdResidente = modelo.IdResidente!.Value,
            IdUsuarioCreador = usuario.IdUsuario,
            Titulo = modelo.Titulo,
            Descripcion = modelo.Descripcion,
            Prioridad = modelo.Prioridad,
            FechaProgramada = modelo.FechaProgramada!.Value,
            EstadoActual = AlertaEstados.Pendiente,
            IdEmpleadoResponsable = modelo.IdEmpleadoResponsable
        };

        try
        {
            _context.Alerta.Add(alerta);
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"Alerta «{alerta.Titulo}» creada en estado Pendiente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear la alerta.");
            ModelState.AddModelError(string.Empty, "No se pudo guardar la alerta. Intente nuevamente.");
            await CargarListasFormularioAsync(modelo);
            return View(modelo);
        }
    }

    // GET: Alertas/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var alerta = await _context.Alerta
            .AsNoTracking()
            .Include(a => a.IdResidenteNavigation)
            .FirstOrDefaultAsync(a => a.IdAlerta == id.Value);

        if (alerta == null) return NotFound();

        if (alerta.EstadoActual == AlertaEstados.Atendida)
        {
            TempData["MensajeError"] = "Una alerta Atendida ya está cerrada y no se puede editar.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = new AlertaFormViewModel
        {
            IdAlerta = alerta.IdAlerta,
            IdResidente = alerta.IdResidente,
            ResidenteTexto = $"{alerta.IdResidenteNavigation.CodigoUnico} · {alerta.IdResidenteNavigation.Nombre} {alerta.IdResidenteNavigation.Apellido}",
            Titulo = alerta.Titulo,
            Descripcion = alerta.Descripcion,
            Prioridad = alerta.Prioridad,
            FechaProgramada = alerta.FechaProgramada,
            IdEmpleadoResponsable = alerta.IdEmpleadoResponsable,
            Estado = alerta.EstadoActual
        };

        await CargarListasFormularioAsync(modelo);
        return View(modelo);
    }

    // POST: Alertas/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AlertaFormViewModel modelo)
    {
        if (id != modelo.IdAlerta) return NotFound();

        var alerta = await _context.Alerta
            .Include(a => a.IdResidenteNavigation)
            .FirstOrDefaultAsync(a => a.IdAlerta == id);

        if (alerta == null) return NotFound();

        if (alerta.EstadoActual == AlertaEstados.Atendida)
        {
            TempData["MensajeError"] = $"La alerta «{alerta.Titulo}» ya fue marcada como Atendida y no se puede editar.";
            return RedirectToAction(nameof(Index));
        }

        // El residente y el estado no se editan aquí: se toman siempre de la base de datos.
        modelo.IdResidente = alerta.IdResidente;
        ModelState.Remove(nameof(AlertaFormViewModel.IdResidente));
        modelo.ResidenteTexto = $"{alerta.IdResidenteNavigation.CodigoUnico} · {alerta.IdResidenteNavigation.Nombre} {alerta.IdResidenteNavigation.Apellido}";
        modelo.Estado = alerta.EstadoActual;

        modelo.Titulo = modelo.Titulo?.Trim() ?? string.Empty;
        modelo.Descripcion = modelo.Descripcion?.Trim() ?? string.Empty;

        await ValidarReglasAsync(modelo, esEdicion: true, empleadoOriginal: alerta.IdEmpleadoResponsable);

        if (!ModelState.IsValid)
        {
            await CargarListasFormularioAsync(modelo);
            return View(modelo);
        }

        alerta.Titulo = modelo.Titulo;
        alerta.Descripcion = modelo.Descripcion;
        alerta.Prioridad = modelo.Prioridad;
        alerta.FechaProgramada = modelo.FechaProgramada!.Value;
        alerta.IdEmpleadoResponsable = modelo.IdEmpleadoResponsable;

        try
        {
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"Alerta «{alerta.Titulo}» actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar la alerta {IdAlerta}.", id);
            ModelState.AddModelError(string.Empty, "No se pudo guardar los cambios. Intente nuevamente.");
            await CargarListasFormularioAsync(modelo);
            return View(modelo);
        }
    }

    // GET: Alertas/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var detalle = await ConstruirDetalleAsync(id.Value);
        if (detalle == null) return NotFound();

        return View(detalle);
    }

    // POST: Alertas/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var alerta = await _context.Alerta.FirstOrDefaultAsync(a => a.IdAlerta == id);
        if (alerta == null) return NotFound();

        // Opción A: solo se elimina una alerta Pendiente y sin historial.
        var tieneHistorial = await _context.HistorialEstadoAlerta.AnyAsync(h => h.IdAlerta == id);
        if (alerta.EstadoActual != AlertaEstados.Pendiente || tieneHistorial)
        {
            TempData["MensajeError"] = $"La alerta «{alerta.Titulo}» no se puede eliminar: solo se eliminan alertas Pendientes sin historial de estados.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Alerta.Remove(alerta);
            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"Alerta «{alerta.Titulo}» eliminada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar la alerta {IdAlerta}.", id);
            TempData["MensajeError"] = "No se pudo eliminar la alerta. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string? estadoEsperado)
    {
        var alerta = await _context.Alerta.FirstOrDefaultAsync(a => a.IdAlerta == id);
        if (alerta == null)
        {
            return NotFound();
        }

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

        _context.HistorialEstadoAlerta.Add(new HistorialEstadoAlerta
        {
            IdAlerta = alerta.IdAlerta,
            IdUsuario = usuario.IdUsuario,
            EstadoAnterior = anterior,
            EstadoNuevo = siguiente,
            FechaHora = DateTime.Now
        });

        try
        {
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

    private async Task ValidarReglasAsync(AlertaFormViewModel modelo, bool esEdicion, int? empleadoOriginal)
    {
        if (!string.IsNullOrWhiteSpace(modelo.Prioridad) && !AlertaPrioridades.Todas.Contains(modelo.Prioridad))
            ModelState.AddModelError(nameof(modelo.Prioridad), "Seleccione una prioridad: Urgente, Programada o Rutina.");

        // En Edit el residente no cambia, así que solo se valida al crear.
        if (!esEdicion && modelo.IdResidente.HasValue)
        {
            var residenteValido = await _context.Residentes
                .AnyAsync(r => r.IdResidente == modelo.IdResidente.Value && r.Estado);
            if (!residenteValido)
                ModelState.AddModelError(nameof(modelo.IdResidente), "Seleccione un residente activo.");
        }

        // Si el responsable no cambió, se respeta aunque ya esté inactivo.
        if (modelo.IdEmpleadoResponsable.HasValue && modelo.IdEmpleadoResponsable != empleadoOriginal)
        {
            var empleadoValido = await _context.Empleados
                .AnyAsync(e => e.IdEmpleado == modelo.IdEmpleadoResponsable.Value && e.Estado);
            if (!empleadoValido)
                ModelState.AddModelError(nameof(modelo.IdEmpleadoResponsable), "El responsable seleccionado no existe o está inactivo.");
        }
    }

    private async Task CargarListasFormularioAsync(AlertaFormViewModel modelo)
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
            .Where(e => e.Estado || e.IdEmpleado == modelo.IdEmpleadoResponsable)
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .Select(e => new SelectListItem
            {
                Value = e.IdEmpleado.ToString(),
                Text = e.Nombre + " " + e.Apellido + " (" + e.Cargo + ")"
            })
            .ToListAsync();
    }

    private async Task<AlertaDetalleViewModel?> ConstruirDetalleAsync(int id)
    {
        var alerta = await _context.Alerta
            .AsNoTracking()
            .Include(a => a.IdResidenteNavigation)
            .Include(a => a.IdUsuarioCreadorNavigation)
            .Include(a => a.IdEmpleadoResponsableNavigation)
            .FirstOrDefaultAsync(a => a.IdAlerta == id);

        if (alerta == null) return null;

        var historial = await _context.HistorialEstadoAlerta
            .AsNoTracking()
            .Include(h => h.IdUsuarioNavigation)
            .Where(h => h.IdAlerta == id)
            .OrderBy(h => h.FechaHora)
            .ToListAsync();

        return new AlertaDetalleViewModel
        {
            IdAlerta = alerta.IdAlerta,
            Titulo = alerta.Titulo,
            Descripcion = alerta.Descripcion,
            Prioridad = alerta.Prioridad,
            Estado = alerta.EstadoActual,
            Residente = $"{alerta.IdResidenteNavigation.Nombre} {alerta.IdResidenteNavigation.Apellido}",
            CodigoResidente = alerta.IdResidenteNavigation.CodigoUnico,
            FechaProgramada = alerta.FechaProgramada,
            CreadaPor = alerta.IdUsuarioCreadorNavigation.Correo,
            Responsable = alerta.IdEmpleadoResponsableNavigation == null
                ? null
                : $"{alerta.IdEmpleadoResponsableNavigation.Nombre} {alerta.IdEmpleadoResponsableNavigation.Apellido}",
            Historial = historial.Select(h => new AlertaHistorialItemViewModel
            {
                FechaHora = h.FechaHora,
                Usuario = h.IdUsuarioNavigation.Correo,
                EstadoAnterior = h.EstadoAnterior,
                EstadoNuevo = h.EstadoNuevo,
                Comentario = h.Comentario
            }).ToList()
        };
    }
}
