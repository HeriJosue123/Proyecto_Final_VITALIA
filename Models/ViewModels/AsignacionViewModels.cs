namespace VITALIA.Models.ViewModels;

/// <summary>Tipos de asignación: cada residente puede tener un médico y un cuidador (o enfermero).</summary>
public static class TiposAsignacion
{
    public const string Medico = "Médico";
    public const string Cuidador = "Cuidador";

    // El tipo sale del rol del usuario del empleado: Médico -> médico; cualquier otro -> cuidador
    public static string De(Empleado empleado) =>
        empleado.IdUsuarioNavigation?.IdRolNavigation?.NombreRol == RolesSistema.Medico ? Medico : Cuidador;
}

/// <summary>Empleado en la lista del formulario Asignar.</summary>
public class EmpleadoAsignacionOpcion
{
    public int IdEmpleado { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Tipo { get; set; } = TiposAsignacion.Cuidador;
}

/// <summary>Residente en la lista del formulario Asignar, con quién lo atiende ahora.</summary>
public class ResidenteAsignacionOpcion
{
    public int IdResidente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Cuidador { get; set; }
    public string? Medico { get; set; }

    public string Texto =>
        Cuidador == null && Medico == null
            ? Nombre
            : Nombre + " (" + string.Join(" · ", new[]
            {
                Cuidador != null ? "cuidador: " + Cuidador : null,
                Medico != null ? "médico: " + Medico : null
            }.Where(t => t != null)) + ")";
}
