using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Cargo { get; set; } = null!;

    public bool Estado { get; set; }

    public virtual ICollection<Alerta> Alerta { get; set; } = new List<Alerta>();

    public virtual ICollection<AsignacionCuidado> AsignacionCuidados { get; set; } = new List<AsignacionCuidado>();

    public virtual ICollection<AtencionDiaria> AtencionDiaria { get; set; } = new List<AtencionDiaria>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
