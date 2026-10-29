namespace VITALIA.Models;

/// <summary>Nombres de los roles tal como están guardados en la tabla ROL.</summary>
public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string Enfermero = "Enfermero";
    public const string Medico = "Médico";
    public const string Familiar = "Familiar";

    // Roles que atienden a los residentes (Inicio, Residentes, Bitácora y Alertas)
    public const string PersonalClinico = Administrador + "," + Enfermero + "," + Medico;
}
