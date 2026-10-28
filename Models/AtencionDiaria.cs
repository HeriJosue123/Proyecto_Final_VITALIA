using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class AtencionDiaria
{
    public int IdAtencion { get; set; }

    public int IdResidente { get; set; }

    public int IdEmpleado { get; set; }

    public int IdTipoAtencion { get; set; }

    public DateTime FechaHora { get; set; }

    public string Observaciones { get; set; } = null!;

    public string EstadoCumplimiento { get; set; } = null!;

    public virtual ICollection<CorreccionAtencion> CorreccionAtencions { get; set; } = new List<CorreccionAtencion>();

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Residente IdResidenteNavigation { get; set; } = null!;

    public virtual TipoAtencion IdTipoAtencionNavigation { get; set; } = null!;
}