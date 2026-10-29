using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data;

/// <summary>
/// Roles y usuarios que VITALIA necesita para poder iniciar sesión.
/// Se crean al encender el servidor solo si todavía no existen: no duplica, no borra
/// y no cambia nada de lo que ya hay en la base.
/// </summary>
public static class DatosIniciales
{
    // Contraseña de los usuarios por defecto, ya encriptada con BCrypt.
    // La contraseña real no se guarda en el código: la comparte el administrador del grupo.
    private const string ContrasenaPorDefectoHash = "$2a$11$FGhrMII8HZBBuCwT3EiJledvKKam0SCQbqYG97U47SIKBbfHBJDVy";

    private static readonly (string Nombre, string Descripcion)[] Roles =
    {
        (RolesSistema.Administrador, "Acceso completo al sistema"),
        (RolesSistema.Enfermero, "Enfermeros y cuidadores: registran atenciones y revisan residentes"),
        (RolesSistema.Medico, "Revisa la salud de los residentes"),
        (RolesSistema.Familiar, "Consulta la información de su familiar")
    };

    private static readonly (string Correo, string Rol)[] Usuarios =
    {
        ("admin@vitalia.com", RolesSistema.Administrador),
        ("enfermero@vitalia.com", RolesSistema.Enfermero),
        ("medico@vitalia.com", RolesSistema.Medico),
        ("familiar@vitalia.com", RolesSistema.Familiar)
    };

    public static async Task CrearSiFaltanAsync(VitaliaDbContext db)
    {
        // 1. Roles que falten
        var rolesExistentes = await db.Roles.Select(r => r.NombreRol).ToListAsync();
        foreach (var (nombre, descripcion) in Roles)
        {
            if (!rolesExistentes.Contains(nombre))
            {
                db.Roles.Add(new Rol { NombreRol = nombre, Descripcion = descripcion });
            }
        }
        await db.SaveChangesAsync();

        // 2. Usuarios que falten (si el correo ya existe, se deja como está)
        var idsRoles = await db.Roles.ToDictionaryAsync(r => r.NombreRol, r => r.IdRol);
        var correosExistentes = await db.Usuarios.Select(u => u.Correo).ToListAsync();
        foreach (var (correo, rol) in Usuarios)
        {
            if (!correosExistentes.Contains(correo))
            {
                db.Usuarios.Add(new Usuario
                {
                    Correo = correo,
                    IdRol = idsRoles[rol],
                    ContrasenaHash = ContrasenaPorDefectoHash,
                    Estado = true
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
