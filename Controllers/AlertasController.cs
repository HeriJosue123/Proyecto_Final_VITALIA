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

        var alerta = new Alerta
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
            _logger.LogError(ex, "Error al crear la alerta.");
            TempData["MensajeError"] = "No se pudo guardar la alerta. Intente nuevamente.";
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
}