using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class LoteMedicamento
{
    public int IdLote { get; set; }
    public int IdMedicamento { get; set; }
    public string NumeroLote { get; set; } = null!;
    public DateTime FechaVencimiento { get; set; }
    public int Stock { get; set; }

    public virtual Medicamento IdMedicamentoNavigation { get; set; } = null!;
    public virtual ICollection<EntregaMedicamento> EntregaMedicamentos { get; set; } = new List<EntregaMedicamento>();
}