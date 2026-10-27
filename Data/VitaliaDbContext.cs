using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data;

public partial class VitaliaDbContext : DbContext
{
    public VitaliaDbContext()
    {
    }

    public VitaliaDbContext(DbContextOptions<VitaliaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alertum> Alerta { get; set; }
    public virtual DbSet<AsignacionCuidado> AsignacionCuidados { get; set; }
    public virtual DbSet<AtencionDiarium> AtencionDiaria { get; set; }
    public virtual DbSet<CorreccionAtencion> CorreccionAtencions { get; set; }
    public virtual DbSet<Empleado> Empleados { get; set; }
    public virtual DbSet<FamiliarResidente> FamiliarResidentes { get; set; }
    public virtual DbSet<HistorialEstadoAlertum> HistorialEstadoAlerta { get; set; }
    public virtual DbSet<Residente> Residentes { get; set; }
    public virtual DbSet<Rol> Rols { get; set; }
    public virtual DbSet<TipoAtencion> TipoAtencions { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }

    // SE ELIMINÓ EL MÉTODO OnConfiguring PARA EVITAR EL BLOQUEO CON SQL SERVER

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alertum>(entity =>
        {
            entity.HasKey(e => e.IdAlerta);
            // RESTRICCIÓN AGREGADA AQUÍ
            entity.ToTable("ALERTA", t => t.HasCheckConstraint("CK_Alerta_Estado", "[estado_actual] IN ('Pendiente', 'En Proceso', 'Atendida')"));
            entity.Property(e => e.IdAlerta).HasColumnName("id_alerta");
            entity.Property(e => e.Descripcion).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.EstadoActual).HasMaxLength(30).IsUnicode(false).HasDefaultValue("Pendiente", "DF_ALERTA_estado").HasColumnName("estado_actual");
            entity.Property(e => e.FechaProgramada).HasColumnType("datetime").HasColumnName("fecha_programada");
            entity.Property(e => e.IdResidente).HasColumnName("id_residente");
            entity.Property(e => e.IdUsuarioCreador).HasColumnName("id_usuario_creador");
            entity.Property(e => e.Titulo).HasMaxLength(150).IsUnicode(false).HasColumnName("titulo");
            entity.HasOne(d => d.IdResidenteNavigation).WithMany(p => p.Alerta).HasForeignKey(d => d.IdResidente).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ALERTA_RESIDENTE");
            entity.HasOne(d => d.IdUsuarioCreadorNavigation).WithMany(p => p.Alerta).HasForeignKey(d => d.IdUsuarioCreador).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ALERTA_USUARIO");
        });

        modelBuilder.Entity<AsignacionCuidado>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion);
            entity.ToTable("ASIGNACION_CUIDADO");
            entity.Property(e => e.IdAsignacion).HasColumnName("id_asignacion");
            entity.Property(e => e.Estado).HasDefaultValue(true, "DF_ASIGNACION_estado").HasColumnName("estado");
            entity.Property(e => e.FechaFin).HasColumnType("datetime").HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasDefaultValueSql("(getdate())", "DF_ASIGNACION_inicio").HasColumnType("datetime").HasColumnName("fecha_inicio");
            entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
            entity.Property(e => e.IdResidente).HasColumnName("id_residente");
            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.AsignacionCuidados).HasForeignKey(d => d.IdEmpleado).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ASIGNACION_EMPLEADO");
            entity.HasOne(d => d.IdResidenteNavigation).WithMany(p => p.AsignacionCuidados).HasForeignKey(d => d.IdResidente).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ASIGNACION_RESIDENTE");
        });

        modelBuilder.Entity<AtencionDiarium>(entity =>
        {
            entity.HasKey(e => e.IdAtencion);
            entity.ToTable("ATENCION_DIARIA", tb => tb.HasTrigger("TRG_AtencionDiaria_Inmutable"));
            entity.Property(e => e.IdAtencion).HasColumnName("id_atencion");
            entity.Property(e => e.EstadoCumplimiento).HasMaxLength(50).IsUnicode(false).HasDefaultValue("Completado", "DF_ATENCION_cumplimiento").HasColumnName("estado_cumplimiento");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(getdate())", "DF_ATENCION_fecha").HasColumnType("datetime").HasColumnName("fecha_hora");
            entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
            entity.Property(e => e.IdResidente).HasColumnName("id_residente");
            entity.Property(e => e.IdTipoAtencion).HasColumnName("id_tipo_atencion");
            entity.Property(e => e.Observaciones).IsUnicode(false).HasColumnName("observaciones");
            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.AtencionDiaria).HasForeignKey(d => d.IdEmpleado).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ATENCION_EMPLEADO");
            entity.HasOne(d => d.IdResidenteNavigation).WithMany(p => p.AtencionDiaria).HasForeignKey(d => d.IdResidente).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ATENCION_RESIDENTE");
            entity.HasOne(d => d.IdTipoAtencionNavigation).WithMany(p => p.AtencionDiaria).HasForeignKey(d => d.IdTipoAtencion).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_ATENCION_TIPO");
        });

        modelBuilder.Entity<CorreccionAtencion>(entity =>
        {
            entity.HasKey(e => e.IdCorreccion);
            entity.ToTable("CORRECCION_ATENCION");
            entity.Property(e => e.IdCorreccion).HasColumnName("id_correccion");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(getdate())", "DF_CORRECCION_fecha").HasColumnType("datetime").HasColumnName("fecha_hora");
            entity.Property(e => e.IdAtencion).HasColumnName("id_atencion");
            entity.Property(e => e.IdUsuarioAdmin).HasColumnName("id_usuario_admin");
            entity.Property(e => e.Motivo).IsUnicode(false).HasColumnName("motivo");
            entity.Property(e => e.TipoAccion).HasMaxLength(30).IsUnicode(false).HasColumnName("tipo_accion");
            entity.HasOne(d => d.IdAtencionNavigation).WithMany(p => p.CorreccionAtencions).HasForeignKey(d => d.IdAtencion).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_CORRECCION_ATENCION");
            entity.HasOne(d => d.IdUsuarioAdminNavigation).WithMany(p => p.CorreccionAtencions).HasForeignKey(d => d.IdUsuarioAdmin).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_CORRECCION_USUARIO");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado);
            entity.ToTable("EMPLEADO");
            entity.HasIndex(e => e.IdUsuario, "UQ_EMPLEADO_usuario").IsUnique();
            entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
            entity.Property(e => e.Apellido).HasMaxLength(100).IsUnicode(false).HasColumnName("apellido");
            entity.Property(e => e.Cargo).HasMaxLength(100).IsUnicode(false).HasColumnName("cargo");
            entity.Property(e => e.Estado).HasDefaultValue(true, "DF_EMPLEADO_estado").HasColumnName("estado");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre");
            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Empleado).HasForeignKey<Empleado>(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_EMPLEADO_USUARIO");
        });

        modelBuilder.Entity<FamiliarResidente>(entity =>
        {
            entity.HasKey(e => e.IdFamiliarResidente);
            entity.ToTable("FAMILIAR_RESIDENTE");
            entity.Property(e => e.IdFamiliarResidente).HasColumnName("id_familiar_residente");
            entity.Property(e => e.IdResidente).HasColumnName("id_residente");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Parentesco).HasMaxLength(50).IsUnicode(false).HasColumnName("parentesco");
            entity.Property(e => e.TelefonoContacto).HasMaxLength(20).IsUnicode(false).HasColumnName("telefono_contacto");
            entity.HasOne(d => d.IdResidenteNavigation).WithMany(p => p.FamiliarResidentes).HasForeignKey(d => d.IdResidente).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_FAMILIAR_RESIDENTE_RESIDENTE");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.FamiliarResidentes).HasForeignKey(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_FAMILIAR_RESIDENTE_USUARIO");
        });

        modelBuilder.Entity<HistorialEstadoAlertum>(entity =>
        {
            entity.HasKey(e => e.IdHistorialEstado);
            entity.ToTable("HISTORIAL_ESTADO_ALERTA");
            entity.Property(e => e.IdHistorialEstado).HasColumnName("id_historial_estado");
            entity.Property(e => e.Comentario).IsUnicode(false).HasColumnName("comentario");
            entity.Property(e => e.EstadoAnterior).HasMaxLength(30).IsUnicode(false).HasColumnName("estado_anterior");
            entity.Property(e => e.EstadoNuevo).HasMaxLength(30).IsUnicode(false).HasColumnName("estado_nuevo");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(getdate())", "DF_HISTORIAL_ALERTA_fecha").HasColumnType("datetime").HasColumnName("fecha_hora");
            entity.Property(e => e.IdAlerta).HasColumnName("id_alerta");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.HasOne(d => d.IdAlertaNavigation).WithMany(p => p.HistorialEstadoAlerta).HasForeignKey(d => d.IdAlerta).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_HISTORIAL_ALERTA");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialEstadoAlerta).HasForeignKey(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_HISTORIAL_USUARIO");
        });

        modelBuilder.Entity<Residente>(entity =>
        {
            entity.HasKey(e => e.IdResidente);
            entity.ToTable("RESIDENTE");
            entity.HasIndex(e => e.CodigoUnico, "UQ_RESIDENTE_codigo").IsUnique();
            entity.Property(e => e.IdResidente).HasColumnName("id_residente");
            entity.Property(e => e.Alergias).HasMaxLength(255).IsUnicode(false).HasColumnName("alergias");
            entity.Property(e => e.Apellido).HasMaxLength(100).IsUnicode(false).HasColumnName("apellido");
            entity.Property(e => e.CodigoUnico).HasMaxLength(20).IsUnicode(false).HasColumnName("codigo_unico");
            entity.Property(e => e.ContactoEmergencia).HasMaxLength(150).IsUnicode(false).HasColumnName("contacto_emergencia");
            entity.Property(e => e.Estado).HasDefaultValue(true, "DF_RESIDENTE_estado").HasColumnName("estado");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.NumHabitacion).HasMaxLength(20).IsUnicode(false).HasColumnName("num_habitacion");
            entity.Property(e => e.Observaciones).IsUnicode(false).HasColumnName("observaciones");
            entity.Property(e => e.TipoSangre).HasMaxLength(10).IsUnicode(false).HasColumnName("tipo_sangre");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol);
            entity.ToTable("ROL");
            entity.HasIndex(e => e.NombreRol, "UQ_ROL_nombre").IsUnique();
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Descripcion).HasMaxLength(250).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.NombreRol).HasMaxLength(50).IsUnicode(false).HasColumnName("nombre_rol");
        });

        modelBuilder.Entity<TipoAtencion>(entity =>
        {
            entity.HasKey(e => e.IdTipoAtencion);
            entity.ToTable("TIPO_ATENCION");
            entity.HasIndex(e => e.NombreCategoria, "UQ_TIPO_ATENCION_nombre").IsUnique();
            entity.Property(e => e.IdTipoAtencion).HasColumnName("id_tipo_atencion");
            entity.Property(e => e.Descripcion).HasMaxLength(250).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.NombreCategoria).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre_categoria");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);
            entity.ToTable("USUARIO");
            entity.HasIndex(e => e.Correo, "UQ_USUARIO_correo").IsUnique();
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.ContrasenaHash).HasMaxLength(255).IsUnicode(false).HasColumnName("contrasena_hash");
            entity.Property(e => e.Correo).HasMaxLength(150).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.Estado).HasDefaultValue(true, "DF_USUARIO_estado").HasColumnName("estado");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios).HasForeignKey(d => d.IdRol).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_USUARIO_ROL");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}