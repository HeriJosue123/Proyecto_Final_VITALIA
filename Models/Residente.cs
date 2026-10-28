using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class Residente
{
    public int IdResidente { get; set; }

    public string CodigoUnico { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public DateOnly FechaNacimiento { get; set; }

    public string NumHabitacion { get; set; } = null!;

    public string? TipoSangre { get; set; }

    public string? Alergias { get; set; }

    public string ContactoEmergencia { get; set; } = null!;

    public string? Observaciones { get; set; }

    public bool Estado { get; set; }

    public virtual ICollection<Alerta> Alerta { get; set; } = new List<Alerta>();

    public virtual ICollection<AsignacionCuidado> AsignacionCuidados { get; set; } = new List<AsignacionCuidado>();

    public virtual ICollection<AtencionDiaria> AtencionDiaria { get; set; } = new List<AtencionDiaria>();

    public virtual ICollection<FamiliarResidente> FamiliarResidentes { get; set; } = new List<FamiliarResidente>();
}
