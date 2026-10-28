using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class TipoAtencion
{
    public int IdTipoAtencion { get; set; }

    public string NombreCategoria { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<AtencionDiaria> AtencionDiaria { get; set; } = new List<AtencionDiaria>();
}
