using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models
{
    [Table("ATENCION_DIARIA")]
    public class AtencionDiaria
    {
        [Key]
        [Column("id_atencion")]
        public int IdAtencion { get; set; }

        [Column("id_residente")]
        public int IdResidente { get; set; }

        [Column("id_empleado")]
        public int IdEmpleado { get; set; }

        [Column("id_tipo_atencion")]
        public int IdTipoAtencion { get; set; }

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [Required]
        [Column("observaciones")]
        public string Observaciones { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Column("estado_cumplimiento")]
        public string EstadoCumplimiento { get; set; } = "Completado";

        [ForeignKey("IdResidente")]
        public virtual Residente? Residente { get; set; }

        [ForeignKey("IdEmpleado")]
        public virtual Empleado? Empleado { get; set; }

        [ForeignKey("IdTipoAtencion")]
        public virtual TipoAtencion? TipoAtencion { get; set; }
    }
}