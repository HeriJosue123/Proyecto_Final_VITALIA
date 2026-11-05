using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models.Rrhh
{
    [Table("RRHH_NOMINA")]
    public class Nomina
    {
        [Key]
        [Column("id_nomina")]
        public int IdNomina { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un empleado.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado válido.")]
        [Column("id_empleado")]
        public int IdEmpleado { get; set; }

        [Required(ErrorMessage = "El mes es obligatorio.")]
        [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12.")]
        [Column("mes")]
        public int Mes { get; set; }

        [Required(ErrorMessage = "El año es obligatorio.")]
        [Range(2020, 2100, ErrorMessage = "Año fuera del rango permitido.")]
        [Column("anio")]
        public int Anio { get; set; }

        [Required(ErrorMessage = "El salario bruto es obligatorio.")]
        [Range(0.01, 100000.00, ErrorMessage = "El salario bruto debe ser positivo.")]
        [Column("salario_bruto", TypeName = "decimal(18, 2)")]
        public decimal SalarioBruto { get; set; }

        [Range(0.00, 100000.00, ErrorMessage = "Las deducciones no pueden ser negativas.")]
        [Column("deducciones", TypeName = "decimal(18, 2)")]
        public decimal Deducciones { get; set; } = 0;

        [Column("salario_neto", TypeName = "decimal(18, 2)")]
        public decimal SalarioNeto { get; set; }

        [Required(ErrorMessage = "La fecha de pago es obligatoria.")]
        [Column("fecha_pago")]
        public DateTime FechaPago { get; set; } = DateTime.Now;

        [StringLength(20)]
        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [ForeignKey("IdEmpleado")]
        public virtual Empleado? IdEmpleadoNavigation { get; set; }
    }
}