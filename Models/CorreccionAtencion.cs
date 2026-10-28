using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class CorreccionAtencion
{
    public int IdCorreccion { get; set; }

    public int IdAtencion { get; set; }

    public int IdUsuarioAdmin { get; set; }

    public string Motivo { get; set; } = null!;

    public string TipoAccion { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public virtual AtencionDiaria IdAtencionNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioAdminNavigation { get; set; } = null!;
}
