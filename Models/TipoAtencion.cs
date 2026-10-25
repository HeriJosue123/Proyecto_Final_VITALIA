using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class TipoAtencion
{
    public int IdTipoAtencion { get; set; }

    public string NombreCategoria { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<AtencionDiarium> AtencionDiaria { get; set; } = new List<AtencionDiarium>();
}
