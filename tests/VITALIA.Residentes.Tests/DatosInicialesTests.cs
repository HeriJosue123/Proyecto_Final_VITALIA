using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>Pruebas de los roles y usuarios por defecto que se crean al encender VITALIA.</summary>
[Collection("Login")] // usan la misma base en memoria: no corren al mismo tiempo
public class DatosInicialesTests : IClassFixture<FabricaLogin>
{
    private static readonly string[] CorreosPorDefecto =
        { "admin@vitalia.com", "enfermero@vitalia.com", "medico@vitalia.com", "familiar@vitalia.com" };

    private readonly FabricaLogin _factory;

    public DatosInicialesTests(FabricaLogin factory)
    {
        _factory = factory;
        _factory.CreateClient(); // enciende la aplicación (ahí se crean los datos)
    }

    [Fact]
    public void AlEncender_ExistenLosCuatroRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();

        foreach (var rol in new[] { RolesSistema.Administrador, RolesSistema.Enfermero, RolesSistema.Medico, RolesSistema.Familiar })
        {
            Assert.Equal(1, db.Roles.Count(r => r.NombreRol == rol));
        }
    }

    [Fact]
    public void AlEncender_ExistenLosUsuariosPorDefectoActivosYConSuRol()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();

        var esperado = new Dictionary<string, string>
        {
            ["admin@vitalia.com"] = RolesSistema.Administrador,
            ["enfermero@vitalia.com"] = RolesSistema.Enfermero,
            ["medico@vitalia.com"] = RolesSistema.Medico,
            ["familiar@vitalia.com"] = RolesSistema.Familiar
        };
        foreach (var (correo, rol) in esperado)
        {
            var usuario = db.Usuarios.Single(u => u.Correo == correo);
            Assert.True(usuario.Estado);
            Assert.Equal(rol, db.Roles.Single(r => r.IdRol == usuario.IdRol).NombreRol);
            Assert.StartsWith("$2", usuario.ContrasenaHash); // guardada con BCrypt, nunca en texto plano
        }
    }

    [Fact]
    public async Task SeVuelveAEjecutar_NoDuplicaNiCambiaUsuariosExistentes()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();

        // Alguien cambió la contraseña del administrador: no se debe sobrescribir
        var admin = db.Usuarios.Single(u => u.Correo == "admin@vitalia.com");
        string hashAnterior = admin.ContrasenaHash;
        admin.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword("OtraClave99");
        await db.SaveChangesAsync();
        string hashNuevo = admin.ContrasenaHash;

        await DatosIniciales.CrearSiFaltanAsync(db);
        await DatosIniciales.CrearSiFaltanAsync(db);

        Assert.Equal(4, db.Roles.Count(r => r.NombreRol == RolesSistema.Administrador || r.NombreRol == RolesSistema.Enfermero
                                          || r.NombreRol == RolesSistema.Medico || r.NombreRol == RolesSistema.Familiar));
        foreach (var correo in CorreosPorDefecto)
        {
            Assert.Equal(1, db.Usuarios.Count(u => u.Correo == correo));
        }
        Assert.Equal(hashNuevo, db.Usuarios.Single(u => u.Correo == "admin@vitalia.com").ContrasenaHash);

        // Se deja como estaba para las demás pruebas
        admin.ContrasenaHash = hashAnterior;
        await db.SaveChangesAsync();
    }
}
