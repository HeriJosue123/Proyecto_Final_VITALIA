using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class AsignacionCuidado
{
    public int IdAsignacion { get; set; }

    public int IdEmpleado { get; set; }

    public int IdResidente { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Estado { get; set; }

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Residente IdResidenteNavigation { get; set; } = null!;
}
