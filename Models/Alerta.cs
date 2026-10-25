using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models
{
    [Table("ALERTA")]
    public class Alerta
    {
        [Key]
        [Column("id_alerta")]
        public int IdAlerta { get; set; }

        [Column("id_residente")]
        public int IdResidente { get; set; }

        [Column("id_usuario_creador")]
        public int IdUsuarioCreador { get; set; }

        [Required, StringLength(150)]
        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("fecha_programada")]
        public DateTime FechaProgramada { get; set; }

        [Required, StringLength(30)]
        [Column("estado_actual")]
        public string EstadoActual { get; set; } = "Pendiente";

        [ForeignKey("IdResidente")]
        public virtual Residente? Residente { get; set; }

        [ForeignKey("IdUsuarioCreador")]
        public virtual Usuario? UsuarioCreador { get; set; }
    }
}