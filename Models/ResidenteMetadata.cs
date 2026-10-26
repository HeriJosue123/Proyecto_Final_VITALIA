using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace VITALIA.Models;

/// <summary>
/// Validaciones del residente. Se declaran en una clase de metadatos para no
/// modificar la entidad Residente generada a partir de la base de datos.
/// </summary>
[ModelMetadataType(typeof(ResidenteMetadata))]
public partial class Residente
{
}

public class ResidenteMetadata
{
    [Display(Name = "Código único")]
    [Required(ErrorMessage = "El código único del residente es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código único no puede exceder los 20 caracteres.")]
    [RegularExpression(@"^\s*[A-Za-z0-9-]+\s*$", ErrorMessage = "El código único solo admite letras, números y guiones (ej. RES-2026-001).")]
    public string CodigoUnico { get; set; } = null!;

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "Apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder los 100 caracteres.")]
    public string Apellido { get; set; } = null!;

    [Display(Name = "Fecha de nacimiento")]
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly FechaNacimiento { get; set; }

    [Display(Name = "Habitación")]
    [Required(ErrorMessage = "La habitación es obligatoria.")]
    [StringLength(20, ErrorMessage = "La habitación no puede exceder los 20 caracteres.")]
    public string NumHabitacion { get; set; } = null!;

    [Display(Name = "Tipo de sangre")]
    [StringLength(10, ErrorMessage = "El tipo de sangre no puede exceder los 10 caracteres.")]
    public string? TipoSangre { get; set; }

    [Display(Name = "Alergias")]
    [StringLength(255, ErrorMessage = "Las alergias no pueden exceder los 255 caracteres.")]
    public string? Alergias { get; set; }

    [Display(Name = "Contacto de emergencia")]
    [Required(ErrorMessage = "El contacto de emergencia es obligatorio.")]
    [StringLength(150, ErrorMessage = "El contacto de emergencia no puede exceder los 150 caracteres.")]
    public string ContactoEmergencia { get; set; } = null!;

    [Display(Name = "Antecedentes / observaciones")]
    [Required(ErrorMessage = "Los antecedentes del residente son obligatorios.")]
    public string? Observaciones { get; set; }

    [Display(Name = "Estado")]
    public bool Estado { get; set; }
}
