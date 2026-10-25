using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class Alertum
{
    public int IdAlerta { get; set; }

    public int IdResidente { get; set; }

    public int IdUsuarioCreador { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaProgramada { get; set; }

    public string EstadoActual { get; set; } = null!;

    public virtual ICollection<HistorialEstadoAlertum> HistorialEstadoAlerta { get; set; } = new List<HistorialEstadoAlertum>();

    public virtual Residente IdResidenteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioCreadorNavigation { get; set; } = null!;
}
