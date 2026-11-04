using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models.Rrhh
{
    [Table("RRHH_PERSONAL_EXTERNO")]
    public class PersonalExterno
    {
        [Key]
        [Column("id_personal_externo")]
        public int IdPersonalExterno { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras.")]
        [Column("nombre")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El apellido solo puede contener letras.")]
        [Column("apellido")]
        public string Apellido { get; set; } = null!;

        [Required(ErrorMessage = "El Documento de Identidad es obligatorio.")]
        [StringLength(10, ErrorMessage = "El DUI no puede superar los 10 caracteres.")]
        [RegularExpression(@"^\d{8}-\d{1}$", ErrorMessage = "Formato de DUI inválido (Ejemplo: 00000000-0).")]
        [Column("documento_identidad")]
        public string DocumentoIdentidad { get; set; } = null!;

        [StringLength(100, ErrorMessage = "La empresa proveedora no puede superar los 100 caracteres.")]
        [Column("empresa_proveedora")]
        public string EmpresaProveedora { get; set; } = "Particular";

        [Required(ErrorMessage = "Debe seleccionar un área de servicio.")]
        [StringLength(50)]
        [Column("area_servicio")]
        public string AreaServicio { get; set; } = null!;

        [StringLength(9, ErrorMessage = "El teléfono debe tener formato 0000-0000.")]
        [RegularExpression(@"^[267]\d{3}-\d{4}$", ErrorMessage = "Formato de teléfono inválido (Ejemplo: 7000-0000 o 2200-0000).")]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [StringLength(100, ErrorMessage = "El horario no puede superar los 100 caracteres.")]
        [Column("horario_turno")]
        public string? HorarioTurno { get; set; }

        [Range(0.01, 100000.00, ErrorMessage = "El monto acordado debe ser mayor a $0.00.")]
        [Column("monto_servicio", TypeName = "decimal(18, 2)")]
        public decimal? MontoServicio { get; set; }

        [StringLength(20)]
        [Column("estado_pago")]
        public string EstadoPago { get; set; } = "Pendiente";

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}