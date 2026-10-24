using System.ComponentModel.DataAnnotations;
using VITALIA.Models;

namespace VITALIA.Models.ViewModels;

/// <summary>
/// Modelo de vista para mostrar cada residente en el listado.
/// </summary>
public class ResidenteItemViewModel
{
    public int IdResidente { get; set; }

    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Display(Name = "Nombre Completo")]
    public string NombreCompleto => $"{Nombre} {Apellido}";

    [Display(Name = "Fecha de Nacimiento")]
    [DataType(DataType.Date)]
    public DateTime FechaNacimiento { get; set; }

    [Display(Name = "Edad")]
    public int Edad
    {
        get
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - FechaNacimiento.Year;
            if (FechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return Math.Max(0, edad);
        }
    }

    [Display(Name = "Sexo")]
    public string Sexo { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Display(Name = "Fecha de Ingreso")]
    [DataType(DataType.Date)]
    public DateTime FechaIngreso { get; set; }

    [Display(Name = "ID Habitación")]
    public int IdHabitacion { get; set; }

    [Display(Name = "Habitación")]
    public string NumeroHabitacion { get; set; } = string.Empty;

    [Display(Name = "Estado Habitación")]
    public string EstadoHabitacion { get; set; } = string.Empty;
}

/// <summary>
/// Modelo de vista para la página principal del listado de residentes.
/// </summary>
public class ResidentesIndexViewModel
{
    public IEnumerable<ResidenteItemViewModel> Residentes { get; set; } = new List<ResidenteItemViewModel>();

    public string? Busqueda { get; set; }
    public string? OrdenActual { get; set; }

    // Métricas del listado
    public int TotalResidentes { get; set; }
    public int TotalHombres { get; set; }
    public int TotalMujeres { get; set; }
}

/// <summary>
/// Representa una opción de habitación en el selector de asignación.
/// </summary>
public class HabitacionOpcionViewModel
{
    public int IdHabitacion { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public int ResidentesAsignados { get; set; }
    public int PlazasDisponibles => Math.Max(0, Capacidad - ResidentesAsignados);
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Devuelve el estado operativo coherente de la habitación según sus plazas y reglas de negocio.
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

            return "Disponible";
        }
    }

    // Regla de negocio: Disponible si estado coherente es 'Disponible' y quedan plazas libres (> 0)
    public bool EstaHabilitadaParaAsignar => EstadoCoherente == "Disponible" && PlazasDisponibles > 0;

    public string TextoVisual =>
        $"Habitación {Numero} | Plazas libres: {PlazasDisponibles}/{Capacidad} | Estado: {EstadoCoherente}" +
        (EstaHabilitadaParaAsignar ? "" : " (No disponible)");
}

/// <summary>
/// Modelo de vista para creación y edición de residentes.
/// </summary>
public class ResidenteFormViewModel
{
    public int IdResidente { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(50, ErrorMessage = "El apellido no puede exceder los 50 caracteres.")]
    [Display(Name = "Apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Nacimiento")]
    public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-70);

    [Required(ErrorMessage = "El sexo es obligatorio.")]
    [StringLength(20, ErrorMessage = "El sexo no puede exceder los 20 caracteres.")]
    [Display(Name = "Sexo")]
    public string Sexo { get; set; } = "Femenino";

    [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
    [Display(Name = "Teléfono de Contacto")]
    public string? Telefono { get; set; }

    [StringLength(150, ErrorMessage = "La dirección no puede exceder los 150 caracteres.")]
    [Display(Name = "Dirección de Procedencia")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Ingreso")]
    public DateTime FechaIngreso { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Debe asignar una habitación.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una habitación válida.")]
    [Display(Name = "Habitación Asignada")]
    public int IdHabitacion { get; set; }

    // Lista de habitaciones para renderizar en el formulario
    public List<HabitacionOpcionViewModel> HabitacionesDisponibles { get; set; } = new();
}

/// <summary>
/// Modelo de vista para la ficha de detalle o confirmación de eliminación de un residente.
/// </summary>
public class ResidenteDetalleViewModel
{
    public Residente Residente { get; set; } = null!;
    public int PlazasOcupadasHabitacion { get; set; }
    public int PlazasLibresHabitacion { get; set; }
    
    // Lista de dependencias en tablas externas que bloquean la eliminación
    public List<string> DependenciasBloqueantes { get; set; } = new();

    public bool PuedeEliminar => DependenciasBloqueantes.Count == 0;
}
