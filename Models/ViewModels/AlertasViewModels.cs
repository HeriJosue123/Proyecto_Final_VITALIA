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
