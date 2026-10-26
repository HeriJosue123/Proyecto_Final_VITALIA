using VITALIA.Models;

namespace VITALIA.Models.ViewModels;

/// <summary>
/// Datos de la pantalla de listado de residentes: resultados, filtros aplicados y
/// residente seleccionado para el panel lateral.
/// </summary>
public class ResidentesListadoViewModel
{
    public List<Residente> Residentes { get; set; } = new();

    public string? Busqueda { get; set; }

    // "activos" (por defecto), "inactivos" o "todos"
    public string Estado { get; set; } = "activos";

    public string? Habitacion { get; set; }

    public List<string> Habitaciones { get; set; } = new();

    public Residente? Seleccionado { get; set; }

    public int TotalActivos { get; set; }

    public int TotalInactivos { get; set; }
}
