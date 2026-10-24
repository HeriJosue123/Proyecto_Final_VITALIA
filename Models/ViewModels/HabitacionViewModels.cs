using System.ComponentModel.DataAnnotations;

namespace VITALIA.Models.ViewModels;

/// <summary>
/// Modelo de vista para mostrar cada elemento en la tabla de habitaciones
/// con los cálculos de ocupación y disponibilidad.
/// </summary>
public class HabitacionItemViewModel
{
    public int IdHabitacion { get; set; }
    
    [Display(Name = "Número")]
    public string Numero { get; set; } = string.Empty;

    [Display(Name = "Capacidad")]
    public int Capacidad { get; set; }

    [Display(Name = "Estado")]
    public string Estado { get; set; } = string.Empty;

    [Display(Name = "Residentes Asignados")]
    public int ResidentesAsignados { get; set; }

    [Display(Name = "Plazas Disponibles")]
    public int PlazasDisponibles => Math.Max(0, Capacidad - ResidentesAsignados);

    public bool TieneSobreocupacion => ResidentesAsignados > Capacidad;

    public bool EstaCompleta => ResidentesAsignados >= Capacidad;

    /// <summary>
    /// Devuelve el estado coherente con la regla de negocio de ocupación:
    /// - Si está en Mantenimiento o Inactiva y no tiene residentes, mantiene ese estado operativo.
    /// - Si tiene 0 residentes, debe aparecer como 'Disponible'.
    /// - Si alcanza o supera su capacidad máxima, debe identificarse como 'Ocupada' (llena).
    /// - Si tiene residentes pero aún tiene plazas libres, su estado para admisiones es 'Disponible'.
    /// </summary>
    public string EstadoCoherente
    {
        get
        {
            if (ResidentesAsignados == 0)
            {
                if (string.Equals(Estado?.Trim(), "Mantenimiento", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Estado?.Trim(), "Inactiva", StringComparison.OrdinalIgnoreCase))
                {
                    return Estado?.Trim() ?? "Disponible";
                }
                return "Disponible";
            }

            if (ResidentesAsignados >= Capacidad && Capacidad > 0)
            {
                return "Ocupada";
            }

            // Ocupación parcial: tiene residentes pero aún quedan plazas libres
            return "Disponible";
        }
    }

    /// <summary>
    /// Indica si existe una inconsistencia entre el campo guardado en BD y la ocupación real
    /// (por ejemplo, habitación vacía guardada como 'Ocupada').
    /// </summary>
    public bool TieneInconsistenciaEstado =>
        !string.Equals(Estado?.Trim(), EstadoCoherente, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Modelo de vista para el listado general con filtros, orden y métricas.
/// </summary>
public class HabitacionesIndexViewModel
{
    public IEnumerable<HabitacionItemViewModel> Habitaciones { get; set; } = new List<HabitacionItemViewModel>();

    public string? Busqueda { get; set; }
    public string? FiltroEstado { get; set; }
    public string? OrdenActual { get; set; }

    public List<string> EstadosDisponibles { get; set; } = new();

    // Métricas del listado
    public int TotalHabitaciones { get; set; }
    public int TotalCapacidad { get; set; }
    public int TotalResidentesAsignados { get; set; }
    public int TotalPlazasLibres { get; set; }
}

/// <summary>
/// Modelo de vista para la operación controlada de cambio de estado.
/// </summary>
public class CambiarEstadoViewModel
{
    private static readonly HashSet<string> EstadosValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Disponible",
        "Ocupada",
        "Mantenimiento",
        "Inactiva"
    };

    public int IdHabitacion { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string EstadoActual { get; set; } = string.Empty;
    public int ResidentesAsignados { get; set; }
    public int PlazasDisponibles => Math.Max(0, Capacidad - ResidentesAsignados);

    [Required(ErrorMessage = "Debe seleccionar o indicar el nuevo estado.")]
    [StringLength(30, ErrorMessage = "El estado no puede exceder 30 caracteres.")]
    [Display(Name = "Nuevo Estado")]
    public string NuevoEstado { get; set; } = string.Empty;

    public string EstadoCoherente
    {
        get
        {
            if (ResidentesAsignados == 0)
            {
                if (string.Equals(EstadoActual?.Trim(), "Mantenimiento", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(EstadoActual?.Trim(), "Inactiva", StringComparison.OrdinalIgnoreCase))
                {
                    return EstadoActual?.Trim() ?? "Disponible";
                }
                return "Disponible";
            }

            if (ResidentesAsignados >= Capacidad && Capacidad > 0)
            {
                return "Ocupada";
            }

            return "Disponible";
        }
    }

    public bool TieneInconsistenciaEstado =>
        !string.Equals(EstadoActual?.Trim(), EstadoCoherente, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Evalúa si un estado objetivo está permitido según las reglas de negocio de ocupación.
    /// </summary>
    public bool EsEstadoPermitido(string estado, out string? motivoRechazo)
    {
        motivoRechazo = null;

        if (string.IsNullOrWhiteSpace(estado) || !EstadosValidos.Contains(estado.Trim()))
        {
            motivoRechazo = "Debe seleccionar un estado operativo válido (Disponible, Ocupada, Mantenimiento o Inactiva).";
            return false;
        }

        string est = estado.Trim();

        // Regla: Habitación con 0 residentes no puede marcarse como Ocupada
        if (ResidentesAsignados == 0 && est.Equals("Ocupada", StringComparison.OrdinalIgnoreCase))
        {
            motivoRechazo = "No permitido: La habitación no tiene residentes asignados.";
            return false;
        }

        // Regla: Habitación con capacidad completa no puede marcarse como Disponible
        if (ResidentesAsignados >= Capacidad && Capacidad > 0 && est.Equals("Disponible", StringComparison.OrdinalIgnoreCase))
        {
            motivoRechazo = "No permitido: La habitación ha alcanzado su capacidad máxima.";
            return false;
        }

        // Regla: Habitación con plazas libres no puede marcarse como Ocupada (aún no está llena)
        if (ResidentesAsignados > 0 && ResidentesAsignados < Capacidad && est.Equals("Ocupada", StringComparison.OrdinalIgnoreCase))
        {
            motivoRechazo = $"No permitido: Aún cuenta con {PlazasDisponibles} plaza(s) libre(s).";
            return false;
        }

        // Regla: Habitación con residentes no puede ponerse en Mantenimiento o Inactiva
        if (ResidentesAsignados > 0 && (est.Equals("Mantenimiento", StringComparison.OrdinalIgnoreCase) || est.Equals("Inactiva", StringComparison.OrdinalIgnoreCase)))
        {
            motivoRechazo = "No permitido: Debe reubicar a los residentes antes de inactivar la habitación.";
            return false;
        }

        return true;
    }
}
