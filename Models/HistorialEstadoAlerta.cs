using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models
{
    [Table("HISTORIAL_ESTADO_ALERTA")]
    public class HistorialEstadoAlerta
    {
        [Key]
        [Column("id_historial_estado")]
        public int IdHistorialEstado { get; set; }

        [Column("id_alerta")]
        public int IdAlerta { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required, StringLength(30)]
        [Column("estado_anterior")]
        public string EstadoAnterior { get; set; } = string.Empty;

        [Required, StringLength(30)]
        [Column("estado_nuevo")]
        public string EstadoNuevo { get; set; } = string.Empty;

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [Column("comentario")]
        public string? Comentario { get; set; }

        [ForeignKey("IdAlerta")]
        public virtual Alerta? Alerta { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? Usuario { get; set; }
    }
}