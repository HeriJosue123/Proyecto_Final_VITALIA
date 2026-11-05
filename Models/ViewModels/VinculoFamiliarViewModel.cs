using System.ComponentModel.DataAnnotations;

namespace VITALIA.Models.ViewModels;

/// <summary>Formulario para vincular un usuario Familiar con un residente.</summary>
public class VinculoFamiliarViewModel
{
    public static readonly string[] Parentescos =
        { "Hijo", "Hija", "Nieto", "Nieta", "Esposo", "Esposa", "Hermano", "Hermana", "Sobrino", "Sobrina", "Otro" };

    public int IdResidente { get; set; }
    public string ResidenteNombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar el usuario del familiar.")]
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el parentesco.")]
    public string Parentesco { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono del familiar es obligatorio.")]
    [RegularExpression(ResidenteMetadata.PatronTelefono, ErrorMessage = "El teléfono debe tener 8 dígitos y empezar con 2, 6 o 7 (ej. 7000-1111).")]
    public string Telefono { get; set; } = string.Empty;
}
