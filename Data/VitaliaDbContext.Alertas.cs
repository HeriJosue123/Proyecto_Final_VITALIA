using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data;

public partial class VitaliaDbContext
{
    // VitaliaDbContext.cs ya contiene el mapeo completo de Alerta.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Método dejado vacío para evitar sobrescribir la configuración del DbContext principal
    }
}