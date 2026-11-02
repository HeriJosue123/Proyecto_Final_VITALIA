using System.ComponentModel.DataAnnotations;

namespace VITALIA.Models.ViewModels;

/// <summary>Formulario para que cualquier usuario cambie su propia contraseña.</summary>
public class CambiarContrasenaViewModel
{
    [Required(ErrorMessage = "Escriba su contraseña actual.")]
    [DataType(DataType.Password)]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escriba la contraseña nueva.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña nueva debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Za-zÁÉÍÓÚÑáéíóúñ])(?=.*\d).+$", ErrorMessage = "La contraseña nueva debe tener al menos una letra y un número.")]
    public string ContrasenaNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repita la contraseña nueva.")]
    [DataType(DataType.Password)]
    [Compare(nameof(ContrasenaNueva), ErrorMessage = "Las contraseñas nuevas no coinciden.")]
    public string ConfirmarContrasena { get; set; } = string.Empty;
}
