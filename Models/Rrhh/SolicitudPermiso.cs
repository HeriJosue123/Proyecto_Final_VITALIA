using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models.Rrhh
{
    [Table("RRHH_PERMISO")]
    public class SolicitudPermiso
    {
        [Key]
        [Column("id_permiso")]
        public int IdPermiso { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un empleado.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado válido.")]
        [Column("id_empleado")]
        public int IdEmpleado { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(50, ErrorMessage = "El motivo no puede superar los 50 caracteres.")]
        [Column("tipo_motivo")]
        public string TipoMotivo { get; set; } = null!;

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [Column("fecha_inicio")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        [Column("fecha_fin")]
        public DateTime FechaFin { get; set; } = DateTime.Today;

        [StringLength(500, ErrorMessage = "La observación no puede superar los 500 caracteres.")]
        [Column("observacion")]
        public string? Observacion { get; set; }

        [StringLength(20)]
        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [ForeignKey("IdEmpleado")]
        public virtual Empleado? IdEmpleadoNavigation { get; set; }
    }
}