using System;

namespace VITALIA.Models;

public partial class EntregaMedicamento
{
    public int IdEntrega { get; set; }
    public int IdLote { get; set; }
    public int IdResidente { get; set; }
    public int IdEmpleado { get; set; }
    public int Cantidad { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Observaciones { get; set; }

    public virtual LoteMedicamento IdLoteNavigation { get; set; } = null!;
    public virtual Residente IdResidenteNavigation { get; set; } = null!;
    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;
}