using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace VITALIA.Models;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Cargo { get; set; } = null!;

    public bool Estado { get; set; }

    [ValidateNever]
    public virtual ICollection<Alerta> Alerta { get; set; } = new List<Alerta>();

    [ValidateNever]
    public virtual ICollection<AsignacionCuidado> AsignacionCuidados { get; set; } = new List<AsignacionCuidado>();

    [ValidateNever]
    public virtual ICollection<AtencionDiaria> AtencionDiaria { get; set; } = new List<AtencionDiaria>();

    // Ignorar en la validación del modelo para evitar el error "The IdUsuarioNavigation field is required"
    [ValidateNever]
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}