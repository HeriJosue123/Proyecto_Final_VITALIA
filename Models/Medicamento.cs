using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VITALIA.Models;

public partial class Medicamento
{
    public int IdMedicamento { get; set; }

    [Required(ErrorMessage = "El código del medicamento es obligatorio.")]
    [StringLength(50, ErrorMessage = "El código no puede exceder los 50 caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del medicamento es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
    [Display(Name = "Nombre del Medicamento")]
    public string Nombre { get; set; } = null!;

    [StringLength(255, ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
    [Display(Name = "Descripción / Indicaciones")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Debe especificar la presentación del medicamento.")]
    [StringLength(50, ErrorMessage = "La presentación no puede exceder los 50 caracteres.")]
    [Display(Name = "Presentación")]
    public string? Presentacion { get; set; }

    public bool Estado { get; set; } = true;

    public virtual ICollection<LoteMedicamento> LoteMedicamentos { get; set; } = new List<LoteMedicamento>();

    public virtual ICollection<EntregaMedicamento> EntregaMedicamentos { get; set; } = new List<EntregaMedicamento>();
}