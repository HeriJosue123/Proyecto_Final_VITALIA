using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data
{
    public class VitaliaDbContext : DbContext
    {
        public VitaliaDbContext(DbContextOptions<VitaliaDbContext> options) : base(options) { }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Residente> Residentes { get; set; }
        public DbSet<FamiliarResidente> FamiliaresResidentes { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<AsignacionCuidado> AsignacionesCuidado { get; set; }
        public DbSet<TipoAtencion> TiposAtencion { get; set; }
        public DbSet<AtencionDiaria> AtencionesDiarias { get; set; }
        public DbSet<CorreccionAtencion> CorreccionesAtencion { get; set; }
        public DbSet<Alerta> Alertas { get; set; }
        public DbSet<HistorialEstadoAlerta> HistorialEstadosAlerta { get; set; }
    }
}