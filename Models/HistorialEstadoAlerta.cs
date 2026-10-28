using System;
using System.Collections.Generic;

namespace VITALIA.Models;

public partial class HistorialEstadoAlerta
{
    public int IdHistorialEstado { get; set; }

    public int IdAlerta { get; set; }

    public int IdUsuario { get; set; }

    public string EstadoAnterior { get; set; } = null!;

    public string EstadoNuevo { get; set; } = null!;

    public DateTime FechaHora { get; set; }

    public string? Comentario { get; set; }

    public virtual Alerta IdAlertaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}