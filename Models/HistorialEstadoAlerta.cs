using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class HistorialEstadoAlerta
{
    [Key]
    public int IdHistorialEstado { get; set; }

    [Required]
    public int IdAlerta { get; set; }
    
    [ForeignKey("IdAlerta")]
    public Alerta Alerta { get; set; }

    [Required]
    public int IdUsuario { get; set; } // El usuario que realizó el cambio

    [Required]
    [MaxLength(20)]
    public string EstadoAnterior { get; set; }

    [Required]
    [MaxLength(20)]
    public string EstadoNuevo { get; set; }

    [Required]
    public DateTime FechaHora { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string Comentario { get; set; }
}