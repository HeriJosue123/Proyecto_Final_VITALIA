namespace VITALIA.Models.ViewModels
{
    public class RespaldoViewModel
    {
        public required string NombreArchivo { get; set; } = string.Empty;
        public required string Tamaño { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}