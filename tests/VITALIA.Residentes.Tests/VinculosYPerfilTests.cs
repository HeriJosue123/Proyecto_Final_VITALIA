using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>Vincular familiares (Administrador) y cambiar la propia contraseña. Datos ficticios.</summary>
[Collection("Login")] // usa la misma base en memoria que las pruebas del login
public class VinculosYPerfilTests : IClassFixture<FabricaLogin>
{
    private readonly FabricaLogin _factory;

    public VinculosYPerfilTests(FabricaLogin factory)
    {
        _factory = factory;
        _factory.Sembrar();
    }

    private (int residente, int familiar, string correoFamiliar, int enfermero) CrearDatos()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string s = Guid.NewGuid().ToString("N")[..6];
        string hash = BCrypt.Net.BCrypt.HashPassword(FabricaLogin.Contrasena);
        int Rol(string nombre) => db.Roles.First(r => r.NombreRol == nombre).IdRol;

        var familiar = new Usuario { IdRol = Rol(RolesSistema.Familiar), Correo = "vfam" + s + "@prueba.com", ContrasenaHash = hash, Estado = true };
        var enfermero = new Usuario { IdRol = Rol(RolesSistema.Enfermero), Correo = "venf" + s + "@prueba.com", ContrasenaHash = hash, Estado = true };
        var residente = new Residente
        {
            CodigoUnico = "V-" + s, Nombre = "Ana" + s, Apellido = "Ficticia", FechaNacimiento = new DateOnly(1940, 5, 5),
            NumHabitacion = "102", ContactoEmergencia = "Luis · 7000-2222", Estado = true
        };
        db.AddRange(familiar, enfermero, residente);
        db.SaveChanges();
        return (residente.IdResidente, familiar.IdUsuario, familiar.Correo, enfermero.IdUsuario);
    }

    private async Task<HttpClient> EntrarAsync(string correo, string contrasena = FabricaLogin.Contrasena)
    {
        var cliente = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var html = await (await cliente.GetAsync("/Cuenta/Login")).Content.ReadAsStringAsync();
        await cliente.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Correo"] = correo, ["Contrasena"] = contrasena, ["__RequestVerificationToken"] = Token(html)
        }));
        return cliente;
    }

    private static string Token(string html) =>
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;

    private static async Task<HttpResponseMessage> EnviarAsync(HttpClient cliente, string urlFormulario, string urlPost, Dictionary<string, string> datos)
    {
        datos["__RequestVerificationToken"] = Token(await (await cliente.GetAsync(urlFormulario)).Content.ReadAsStringAsync());
        return await cliente.PostAsync(urlPost, new FormUrlEncodedContent(datos));
    }

    private FamiliarResidente? Vinculo(int residente, int usuario)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<VitaliaDbContext>()
            .FamiliarResidentes.FirstOrDefault(f => f.IdResidente == residente && f.IdUsuario == usuario);
    }

    // ---------- Vincular familiares ----------

    [Fact]
    public async Task Administrador_VinculaFamiliar_YElFamiliarVeAlResidente()
    {
        var d = CrearDatos();
        var admin = await EntrarAsync("administrador@prueba.com");

        var resp = await EnviarAsync(admin, "/VinculosFamiliares/Create?idResidente=" + d.residente, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.familiar.ToString(), ["Parentesco"] = "Hija", ["Telefono"] = "70001234" });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var vinculo = Vinculo(d.residente, d.familiar);
        Assert.NotNull(vinculo);
        Assert.Equal("7000-1234", vinculo!.TelefonoContacto);
        Assert.Equal("Hija", vinculo.Parentesco);

        var familiar = await EntrarAsync(d.correoFamiliar);
        Assert.Equal(HttpStatusCode.OK, (await familiar.GetAsync("/Familiar/Residente/" + d.residente)).StatusCode);
    }

    [Fact]
    public async Task NoVinculaUsuarioQueNoEsFamiliar_NiDosVecesElMismo()
    {
        var d = CrearDatos();
        var admin = await EntrarAsync("administrador@prueba.com");
        string form = "/VinculosFamiliares/Create?idResidente=" + d.residente;

        var noFamiliar = await EnviarAsync(admin, form, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.enfermero.ToString(), ["Parentesco"] = "Hijo", ["Telefono"] = "7000-1234" });
        Assert.Contains("El usuario debe estar activo y tener el rol Familiar.", WebUtility.HtmlDecode(await noFamiliar.Content.ReadAsStringAsync()));
        Assert.Null(Vinculo(d.residente, d.enfermero));

        await EnviarAsync(admin, form, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.familiar.ToString(), ["Parentesco"] = "Hijo", ["Telefono"] = "7000-1234" });
        var repetido = await EnviarAsync(admin, form, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.familiar.ToString(), ["Parentesco"] = "Hijo", ["Telefono"] = "7000-1234" });
        Assert.Contains("Ese familiar ya está vinculado a este residente.", WebUtility.HtmlDecode(await repetido.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task TelefonoQueNoEmpiezaCon267_NoVincula()
    {
        var d = CrearDatos();
        var admin = await EntrarAsync("administrador@prueba.com");

        var resp = await EnviarAsync(admin, "/VinculosFamiliares/Create?idResidente=" + d.residente, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.familiar.ToString(), ["Parentesco"] = "Hijo", ["Telefono"] = "1234-5678" });

        Assert.Contains("empezar con 2, 6 o 7", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
        Assert.Null(Vinculo(d.residente, d.familiar));
    }

    [Fact]
    public async Task TelefonoInvalido_NoVincula()
    {
        var d = CrearDatos();
        var admin = await EntrarAsync("administrador@prueba.com");

        var resp = await EnviarAsync(admin, "/VinculosFamiliares/Create?idResidente=" + d.residente, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.familiar.ToString(), ["Parentesco"] = "Hijo", ["Telefono"] = "7000-12" });

        Assert.Contains("El teléfono debe tener 8 dígitos", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
        Assert.Null(Vinculo(d.residente, d.familiar));
    }

    [Fact]
    public async Task Desvincular_QuitaElAcceso()
    {
        var d = CrearDatos();
        var admin = await EntrarAsync("administrador@prueba.com");
        await EnviarAsync(admin, "/VinculosFamiliares/Create?idResidente=" + d.residente, "/VinculosFamiliares/Create",
            new() { ["IdResidente"] = d.residente.ToString(), ["IdUsuario"] = d.familiar.ToString(), ["Parentesco"] = "Nieto", ["Telefono"] = "7000-1234" });
        int idVinculo = Vinculo(d.residente, d.familiar)!.IdFamiliarResidente;

        var resp = await EnviarAsync(admin, "/Residentes/Details/" + d.residente, "/VinculosFamiliares/Desvincular/" + idVinculo, new());

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Null(Vinculo(d.residente, d.familiar));
        var familiar = await EntrarAsync(d.correoFamiliar);
        Assert.StartsWith("/Cuenta/AccesoDenegado", (await familiar.GetAsync("/Familiar/Residente/" + d.residente)).Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Enfermero_NoPuedeVincularFamiliares()
    {
        var d = CrearDatos();
        var enfermero = await EntrarAsync("enfermero@prueba.com");

        var resp = await enfermero.GetAsync("/VinculosFamiliares/Create?idResidente=" + d.residente);

        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    // ---------- Cambiar contraseña ----------

    [Fact]
    public async Task CambiarContrasena_FuncionaYSeEntraConLaNueva()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.correoFamiliar);

        var resp = await EnviarAsync(cliente, "/Perfil/CambiarContrasena", "/Perfil/CambiarContrasena",
            new() { ["ContrasenaActual"] = FabricaLogin.Contrasena, ["ContrasenaNueva"] = "NuevaClave2026", ["ConfirmarContrasena"] = "NuevaClave2026" });
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

        var otraVez = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await EnviarAsync(otraVez, "/Cuenta/Login", "/Cuenta/Login",
            new() { ["Correo"] = d.correoFamiliar, ["Contrasena"] = "NuevaClave2026" });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var vieja = await EnviarAsync(_factory.CreateClient(), "/Cuenta/Login", "/Cuenta/Login",
            new() { ["Correo"] = d.correoFamiliar, ["Contrasena"] = FabricaLogin.Contrasena });
        Assert.Contains("Correo o contraseña incorrectos.", WebUtility.HtmlDecode(await vieja.Content.ReadAsStringAsync()));
    }

    [Theory]
    [InlineData("Incorrecta1", "NuevaClave2026", "NuevaClave2026", "La contraseña actual no es correcta.")]
    [InlineData(FabricaLogin.Contrasena, "corta1", "corta1", "al menos 8 caracteres")]
    [InlineData(FabricaLogin.Contrasena, "SoloLetrasAqui", "SoloLetrasAqui", "al menos una letra y un número")]
    [InlineData(FabricaLogin.Contrasena, "NuevaClave2026", "OtraClave2026", "Las contraseñas nuevas no coinciden.")]
    [InlineData(FabricaLogin.Contrasena, FabricaLogin.Contrasena, FabricaLogin.Contrasena, "debe ser distinta de la actual")]
    public async Task CambiarContrasena_ConDatosMalos_NoCambia(string actual, string nueva, string confirmar, string mensaje)
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.correoFamiliar);

        var resp = await EnviarAsync(cliente, "/Perfil/CambiarContrasena", "/Perfil/CambiarContrasena",
            new() { ["ContrasenaActual"] = actual, ["ContrasenaNueva"] = nueva, ["ConfirmarContrasena"] = confirmar });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains(mensaje, WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
    }
}
