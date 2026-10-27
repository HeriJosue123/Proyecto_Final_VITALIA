using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Alerta
{
    [Key]
    public int IdAlerta { get; set; }

    [Required]
    public int IdResidente { get; set; }

    [Required]
    public int IdUsuarioCreador { get; set; }

    [Required]
    [MaxLength(150)]
    public string Titulo { get; set; }

    [Required]
    [MaxLength(500)]
    public string Descripcion { get; set; }

    [Required]
    public DateTime FechaProgramada { get; set; }

    [Required]
    [MaxLength(20)]
    public string EstadoActual { get; set; } = "Pendiente";

    // Navegación hacia el historial
    public ICollection<HistorialEstadoAlerta> Historiales { get; set; }
}