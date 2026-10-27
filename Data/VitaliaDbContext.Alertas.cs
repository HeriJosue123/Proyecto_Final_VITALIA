using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data;

public partial class VitaliaDbContext
{
    // VitaliaDbContext.cs ya declara este método parcial y lo llama al final de OnModelCreating.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alertum>(entity =>
        {
            entity.Property(e => e.Prioridad)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Rutina", "DF_ALERTA_prioridad")
                .HasColumnName("prioridad");

            entity.Property(e => e.IdEmpleadoResponsable)
                .HasColumnName("id_empleado_responsable");

            entity.HasOne(d => d.EmpleadoResponsable)
                .WithMany()
                .HasForeignKey(d => d.IdEmpleadoResponsable)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ALERTA_EMPLEADO");
        });
    }
}
