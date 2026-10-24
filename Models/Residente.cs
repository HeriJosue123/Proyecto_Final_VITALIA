using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models;

/// <summary>
/// Representa un residente dentro del asilo VITALIA.
/// Mapeada a la tabla existente 'Residentes' de VITALIADB.
/// </summary>
[Table("Residentes")]
public class Residente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Display(Name = "ID Residente")]
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
    public DateTime FechaNacimiento { get; set; }

    [Required(ErrorMessage = "El sexo es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Sexo")]
    public string Sexo { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(150)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Ingreso")]
    public DateTime FechaIngreso { get; set; }

    [Required(ErrorMessage = "La habitación es obligatoria.")]
    [Display(Name = "Habitación")]
    public int IdHabitacion { get; set; }

    [ForeignKey(nameof(IdHabitacion))]
    public virtual Habitacion? Habitacion { get; set; }
}
