namespace VITALIA.Models;

/// <summary>
/// Extiende la entidad generada Alertum (que es "partial") con los campos que necesita
/// el tablero: prioridad y responsable. Así NO se modifica Alertum.cs.
/// </summary>
public partial class Alertum
{
    public string Prioridad { get; set; } = AlertaPrioridades.Rutina;

    public int? IdEmpleadoResponsable { get; set; }

    public virtual Empleado? EmpleadoResponsable { get; set; }
}

/// <summary>Estados permitidos del ciclo de vida (RN-006).</summary>
public static class AlertaEstados
{
    public const string Pendiente = "Pendiente";
    public const string EnProceso = "En Proceso";
    public const string Atendida = "Atendida";

    public static readonly string[] Todos = { Pendiente, EnProceso, Atendida };

    /// <summary>Devuelve el único estado siguiente válido, o null si ya está cerrada.</summary>
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

    /// <summary>Orden de aparición: Urgente primero.</summary>
    public static int Orden(string prioridad) => prioridad switch
    {
        Urgente => 0,
        Programada => 1,
        _ => 2
    };
}
