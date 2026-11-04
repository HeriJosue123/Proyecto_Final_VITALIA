using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models.Rrhh
{
    [Table("RRHH_CONTRATO")]
    public class ContratoEmpleado
    {
        [Key]
        public int IdContrato { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoContrato { get; set; } = "Indefinido"; // Indefinido, Temporal, Honorarios

        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime? FechaFin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalarioBase { get; set; }

        public bool Activo { get; set; } = true;

        [ForeignKey(nameof(IdEmpleado))]
        public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;
    }
}