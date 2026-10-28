using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VITALIA.Models;

public partial class Alerta
{
    public int IdAlerta { get; set; }

    public int IdResidente { get; set; }

    public int IdUsuarioCreador { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public DateTime FechaProgramada { get; set; }

    public string EstadoActual { get; set; } = null!;

    public string Prioridad { get; set; } = null!;

    // Mapeo explícito de la columna SQL real
    [Column("id_empleado_responsable")]
    public int? IdEmpleadoResponsable { get; set; }

    public virtual ICollection<HistorialEstadoAlerta> HistorialEstadoAlerta { get; set; } = new List<HistorialEstadoAlerta>();

    // Única propiedad de navegación para el responsable
    [ForeignKey(nameof(IdEmpleadoResponsable))]
    public virtual Empleado? IdEmpleadoResponsableNavigation { get; set; }

    // Propiedad wrapper con [NotMapped] obligatorio para evitar columnas fantasma
    [NotMapped]
    public virtual Empleado? EmpleadoResponsable
    {
        get => IdEmpleadoResponsableNavigation;
        set => IdEmpleadoResponsableNavigation = value;
    }

    public virtual Residente IdResidenteNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioCreadorNavigation { get; set; } = null!;
}

/// <summary>Estados permitidos del ciclo de vida (RN-006).</summary>
public static class AlertaEstados
{
    public const string Pendiente = "Pendiente";
    public const string EnProceso = "En Proceso";
    public const string Atendida = "Atendida";

    public static readonly string[] Todos = { Pendiente, EnProceso, Atendida };

    public static string? Siguiente(string actual) => actual switch
    {
        Pendiente => EnProceso,
        EnProceso => Atendida,
        _ => null
    };
}

/// <summary>Niveles de prioridad del tablero.</summary>
public static class AlertaPrioridades
{
    public const string Urgente = "Urgente";
    public const string Programada = "Programada";
    public const string Rutina = "Rutina";

    public static readonly string[] Todas = { Urgente, Programada, Rutina };

    public static int Orden(string prioridad) => prioridad switch
    {
        Urgente => 0,
        Programada => 1,
        _ => 2
    };
}