using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data;

/// <summary>
/// Contexto de datos principal de Entity Framework Core para la plataforma VITALIA.
/// Configurado para conectar con SQL Server mediante inyección de dependencias.
/// </summary>
public class VitaliaDbContext : DbContext
{
    public VitaliaDbContext(DbContextOptions<VitaliaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Habitacion> Habitaciones => Set<Habitacion>();
    public DbSet<Residente> Residentes => Set<Residente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mapeo de la tabla existente 'Habitaciones'
        modelBuilder.Entity<Habitacion>(entity =>
        {
            entity.ToTable("Habitaciones");
            entity.HasKey(e => e.IdHabitacion);
            entity.Property(e => e.IdHabitacion).ValueGeneratedOnAdd();
            entity.Property(e => e.Numero).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Capacidad).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(30).IsRequired();

            // Relación 1 a muchos: Una habitación puede tener varios residentes
            entity.HasMany(e => e.Residentes)
                  .WithOne(r => r.Habitacion)
                  .HasForeignKey(r => r.IdHabitacion)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Mapeo de la tabla existente 'Residentes'
        modelBuilder.Entity<Residente>(entity =>
        {
            entity.ToTable("Residentes");
            entity.HasKey(e => e.IdResidente);
            entity.Property(e => e.IdResidente).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Apellido).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FechaNacimiento).HasColumnType("date").IsRequired();
            entity.Property(e => e.Sexo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Telefono).HasMaxLength(20).IsRequired(false);
            entity.Property(e => e.Direccion).HasMaxLength(150).IsRequired(false);
            entity.Property(e => e.FechaIngreso).HasColumnType("date").IsRequired();
            entity.Property(e => e.IdHabitacion).IsRequired();
        });
    }
}
