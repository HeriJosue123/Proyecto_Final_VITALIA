using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VITALIA.Models.ViewModels;

/// <summary>Datos de una tarjeta del tablero.</summary>
public class AlertaTarjetaViewModel
{
    public int IdAlerta { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Prioridad { get; set; } = AlertaPrioridades.Rutina;
    public string Estado { get; set; } = AlertaEstados.Pendiente;
    public string Residente { get; set; } = string.Empty;
    public string CodigoResidente { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string CreadaPor { get; set; } = string.Empty;
    public string? Responsable { get; set; }

    // Solo para alertas Atendidas: sello de cierre (fecha/hora y quién la cerró)
    public DateTime? FechaCierre { get; set; }
    public string? CerradaPor { get; set; }
}

/// <summary>Pantalla completa del tablero por estado.</summary>
public class AlertasTableroViewModel
{
    public List<AlertaTarjetaViewModel> Pendientes { get; set; } = new();
    public List<AlertaTarjetaViewModel> EnProceso { get; set; } = new();
    public List<AlertaTarjetaViewModel> Atendidas { get; set; } = new();

    public List<SelectListItem> Residentes { get; set; } = new();
    public List<SelectListItem> Empleados { get; set; } = new();

    // Filtros aplicados (RF-016)
    public int? FiltroResidente { get; set; }
    public string? FiltroPrioridad { get; set; }
}

/// <summary>Formulario compartido por Create y Edit.</summary>
public class AlertaFormViewModel
{
    public int IdAlerta { get; set; }

    [Display(Name = "Residente")]
    [Required(ErrorMessage = "Seleccione un residente.")]
    public int? IdResidente { get; set; }

    [Display(Name = "Título")]
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, ErrorMessage = "El título no puede superar los 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Prioridad")]
    [Required(ErrorMessage = "Seleccione una prioridad: Urgente, Programada o Rutina.")]
    public string Prioridad { get; set; } = AlertaPrioridades.Rutina;

    [Display(Name = "Fecha y hora programada")]
    [Required(ErrorMessage = "La fecha y hora programada es obligatoria.")]
    public DateTime? FechaProgramada { get; set; }

    [Display(Name = "Responsable")]
    public int? IdEmpleadoResponsable { get; set; }

    // Solo informativos en Edit (no se editan desde el formulario)
    public string? ResidenteTexto { get; set; }
    public string Estado { get; set; } = AlertaEstados.Pendiente;

    [ValidateNever] public List<SelectListItem> Residentes { get; set; } = new();
    [ValidateNever] public List<SelectListItem> Empleados { get; set; } = new();
}

/// <summary>Una fila del historial de estados de una alerta.</summary>
public class AlertaHistorialItemViewModel
{
    public DateTime FechaHora { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Comentario { get; set; }
}

/// <summary>Datos para Details y Delete.</summary>
public class AlertaDetalleViewModel
{
    public int IdAlerta { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Residente { get; set; } = string.Empty;
    public string CodigoResidente { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string CreadaPor { get; set; } = string.Empty;
    public string? Responsable { get; set; }
    public List<AlertaHistorialItemViewModel> Historial { get; set; } = new();

    public bool PuedeEditar => Estado != AlertaEstados.Atendida;
    public bool PuedeEliminar => Estado == AlertaEstados.Pendiente && Historial.Count == 0;

    public string? MotivoNoEliminar => PuedeEliminar
        ? null
        : "Solo se pueden eliminar alertas Pendientes que no tengan historial de cambios de estado.";
}
