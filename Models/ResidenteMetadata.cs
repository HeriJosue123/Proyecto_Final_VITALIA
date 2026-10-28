using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;

namespace VITALIA.Models;

/// <summary>
/// Validaciones del residente. Se declaran en una clase de metadatos para no
/// modificar la entidad Residente generada a partir de la base de datos.
/// </summary>
[ModelMetadataType(typeof(ResidenteMetadata))]
public partial class Residente
{
    // El formulario pide el contacto de emergencia en dos campos; en la base se guarda
    // unido en contacto_emergencia como "Nombre · 7000-1111". No son columnas de la tabla.

    [NotMapped]
    [Display(Name = "Nombre del contacto")]
    [Required(ErrorMessage = "El nombre del contacto de emergencia es obligatorio.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "El nombre del contacto debe tener entre 2 y 120 caracteres.")]
    [RegularExpression(ResidenteMetadata.PatronNombre, ErrorMessage = "El nombre del contacto solo admite letras y espacios.")]
    public string? ContactoNombre { get; set; }

    [NotMapped]
    [Display(Name = "Teléfono del contacto")]
    [Required(ErrorMessage = "El teléfono del contacto de emergencia es obligatorio.")]
    [RegularExpression(ResidenteMetadata.PatronTelefono, ErrorMessage = "El teléfono debe tener 8 dígitos y empezar por 2, 6 o 7 (ej. 7000-1111).")]
    public string? ContactoTelefono { get; set; }
}

public class ResidenteMetadata
{
    // Letras (con tildes y ñ) separadas por espacios, apóstrofo, punto o guion
    public const string PatronNombre = @"^\s*[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+(?:[\s'.-]+[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]+)*\.?\s*$";

    // Teléfono de El Salvador: 8 dígitos, con o sin guion (7000-1111 o 70001111)
    public const string PatronTelefono = @"^\s*[267]\d{3}-?\d{4}\s*$";

    [Display(Name = "Código único")]
    [Required(ErrorMessage = "El código único del residente es obligatorio.")]
    [StringLength(20, ErrorMessage = "El código único no puede exceder los 20 caracteres.")]
    [RegularExpression(@"^\s*[A-Za-z0-9-]+\s*$", ErrorMessage = "El código único solo admite letras, números y guiones (ej. RES-2026-001).")]
    public string CodigoUnico { get; set; } = null!;

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    [RegularExpression(PatronNombre, ErrorMessage = "El nombre solo admite letras y espacios.")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "Apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder los 100 caracteres.")]
    [RegularExpression(PatronNombre, ErrorMessage = "El apellido solo admite letras y espacios.")]
    public string Apellido { get; set; } = null!;

    [Display(Name = "Fecha de nacimiento")]
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly FechaNacimiento { get; set; }

    [Display(Name = "Habitación")]
    [Required(ErrorMessage = "La habitación es obligatoria.")]
    [StringLength(20, ErrorMessage = "La habitación no puede exceder los 20 caracteres.")]
    [RegularExpression(@"^\s*[A-Za-z0-9-]+\s*$", ErrorMessage = "La habitación solo admite letras, números y guiones (ej. 101 o A-12).")]
    public string NumHabitacion { get; set; } = null!;

    [Display(Name = "Tipo de sangre")]
    [StringLength(10, ErrorMessage = "El tipo de sangre no puede exceder los 10 caracteres.")]
    public string? TipoSangre { get; set; }

    [Display(Name = "Alergias")]
    [StringLength(255, ErrorMessage = "Las alergias no pueden exceder los 255 caracteres.")]
    public string? Alergias { get; set; }

    // Se arma en el controlador a partir de ContactoNombre y ContactoTelefono
    [Display(Name = "Contacto de emergencia")]
    [StringLength(150, ErrorMessage = "El contacto de emergencia no puede exceder los 150 caracteres.")]
    public string ContactoEmergencia { get; set; } = null!;

    [Display(Name = "Antecedentes / observaciones")]
    [Required(ErrorMessage = "Los antecedentes del residente son obligatorios.")]
    [StringLength(2000, ErrorMessage = "Los antecedentes no pueden exceder los 2000 caracteres.")]
    public string? Observaciones { get; set; }

    [Display(Name = "Estado")]
    public bool Estado { get; set; }
}
