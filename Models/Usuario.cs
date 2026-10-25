using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public string Correo { get; set; } = null!;

    public string ContrasenaHash { get; set; } = null!;

    public bool Estado { get; set; }

    public virtual ICollection<Alertum> Alerta { get; set; } = new List<Alertum>();

    public virtual ICollection<CorreccionAtencion> CorreccionAtencions { get; set; } = new List<CorreccionAtencion>();

    public virtual Empleado? Empleado { get; set; }

    public virtual ICollection<FamiliarResidente> FamiliarResidentes { get; set; } = new List<FamiliarResidente>();

    public virtual ICollection<HistorialEstadoAlertum> HistorialEstadoAlerta { get; set; } = new List<HistorialEstadoAlertum>();

    public virtual Rol IdRolNavigation { get; set; } = null!;
}
