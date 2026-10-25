using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class FamiliarResidente
{
    public int IdFamiliarResidente { get; set; }

    public int IdResidente { get; set; }

    public int IdUsuario { get; set; }

    public string Parentesco { get; set; } = null!;

    public string TelefonoContacto { get; set; } = null!;

    public virtual Residente IdResidenteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
