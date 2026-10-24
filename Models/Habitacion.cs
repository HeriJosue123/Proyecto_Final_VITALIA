using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models;

/// <summary>
/// Representa una habitación dentro del asilo VITALIA.
/// Mapeada a la tabla 'Habitaciones' de VITALIADB.
/// </summary>
[Table("Habitaciones")]
public class Habitacion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "ID Habitación")]
    public int IdHabitacion { get; set; }

    [Required(ErrorMessage = "El número de habitación es obligatorio.")]
    [StringLength(10, ErrorMessage = "El número no puede exceder los 10 caracteres.")]
    [Display(Name = "Número de Habitación")]
    public string Numero { get; set; } = string.Empty;

    [Required(ErrorMessage = "La capacidad es obligatoria.")]
    [Range(1, 100, ErrorMessage = "La capacidad debe ser un número entero mayor a cero.")]
    [Display(Name = "Capacidad")]
    public int Capacidad { get; set; }

    [Required(ErrorMessage = "El estado de la habitación es obligatorio.")]
    [StringLength(30, ErrorMessage = "El estado no puede exceder los 30 caracteres.")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Disponible";

    // Colección de residentes asignados (FK Residentes.IdHabitacion -> Habitaciones.IdHabitacion)
    public virtual ICollection<Residente> Residentes { get; set; } = new List<Residente>();

    [NotMapped]
    public int ResidentesAsignados => Residentes?.Count ?? 0;

    [NotMapped]
    public int PlazasDisponibles => Math.Max(0, Capacidad - ResidentesAsignados);

    [NotMapped]
    public bool EstaCompleta => ResidentesAsignados >= Capacidad;

    /// <summary>
    /// Devuelve el estado coherente con la regla de negocio de ocupación:
    /// - Si tiene 0 residentes: si está en Mantenimiento o Inactiva conserva ese estado operativo; en caso contrario es 'Disponible'.
    /// - Si alcanza o supera su capacidad máxima: su estado es 'Ocupada' (llena).
    /// - Si tiene residentes pero aún tiene plazas libres: su estado para admisiones es 'Disponible'.
    /// </summary>
    [NotMapped]
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
    /// </summary>
    [NotMapped]
    public bool TieneInconsistenciaEstado =>
        !string.Equals(Estado?.Trim(), EstadoCoherente, StringComparison.OrdinalIgnoreCase);
}
