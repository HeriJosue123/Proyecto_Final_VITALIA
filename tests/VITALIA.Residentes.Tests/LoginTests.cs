using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VITALIA.Controllers;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>
/// Aplicación completa con base en memoria y el inicio de sesión real (sin sesión ficticia).
/// Crea un usuario de cada rol con la contraseña "Prueba123".
/// </summary>
public class FabricaLogin : WebApplicationFactory<CuentaController>
{
    public const string Contrasena = "Prueba123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<VitaliaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VitaliaDbContext>>();
            services.AddDbContext<VitaliaDbContext>(o => o.UseInMemoryDatabase("LoginPruebas"));
        });
    }

    // Crea los usuarios de prueba una sola vez
    public void Sembrar()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        lock (this)
        {
            if (db.Usuarios.Any(u => u.Correo.EndsWith("@prueba.com"))) return;

            // Los roles ya los crea DatosIniciales al encender la aplicación
            string hash = BCrypt.Net.BCrypt.HashPassword(Contrasena);
            var idsRoles = db.Roles.ToDictionary(r => r.NombreRol, r => r.IdRol);
            foreach (var nombreRol in new[] { RolesSistema.Administrador, RolesSistema.Enfermero, RolesSistema.Familiar })
            {
                db.Usuarios.Add(new Usuario { IdRol = idsRoles[nombreRol], Correo = nombreRol.ToLower() + "@prueba.com", ContrasenaHash = hash, Estado = true });
            }
            db.Usuarios.Add(new Usuario { IdRol = idsRoles[RolesSistema.Enfermero], Correo = "inactivo@prueba.com", ContrasenaHash = hash, Estado = false });
            db.SaveChanges();
        }
    }
}

/// <summary>Pruebas del inicio de sesión y de los permisos por rol. Todos los datos son ficticios.</summary>
[Collection("Login")] // usan la misma base en memoria: no corren al mismo tiempo
public class LoginTests : IClassFixture<FabricaLogin>
{
    private readonly FabricaLogin _factory;

    public LoginTests(FabricaLogin factory)
    {
        _factory = factory;
        _factory.Sembrar();
    }

    private HttpClient NuevoCliente() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<HttpResponseMessage> EntrarAsync(HttpClient cliente, string correo, string contrasena)
    {
        var html = await (await cliente.GetAsync("/Cuenta/Login")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        return await cliente.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Correo"] = correo,
            ["Contrasena"] = contrasena,
            ["__RequestVerificationToken"] = token
        }));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Residentes")]
    [InlineData("/Personal")]
    public async Task SinSesion_RedirigeAlLogin(string url)
    {
        var resp = await NuevoCliente().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/Login", resp.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task PaginaDeLogin_SeVeSinSesion()
    {
        var resp = await NuevoCliente().GetAsync("/Cuenta/Login");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Inicie sesión para continuar", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Administrador_EntraYVeTodoElMenu()
    {
        var cliente = NuevoCliente();
        var resp = await EntrarAsync(cliente, "administrador@prueba.com", FabricaLogin.Contrasena);
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/"));
        Assert.Contains("administrador@prueba.com", html);
        Assert.Contains("Usuarios y Roles", html);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Personal")).StatusCode);
    }

    [Fact]
    public async Task Enfermero_NoVePersonalNiUsuarios()
    {
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, "enfermero@prueba.com", FabricaLogin.Contrasena);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Residentes"));
        Assert.DoesNotContain("Usuarios y Roles", html);

        var resp = await cliente.GetAsync("/Usuario");
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Familiar_VeAvisoDeVistaEnConstruccion()
    {
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, "familiar@prueba.com", FabricaLogin.Contrasena);

        var resp = await cliente.GetAsync("/");
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Cuenta/AccesoDenegado"));
        Assert.Contains("La vista para familiares todavía está en construcción", html);
    }

    [Theory]
    [InlineData("administrador@prueba.com", "Incorrecta1")]   // contraseña equivocada
    [InlineData("noexiste@prueba.com", "Prueba123")]          // correo que no existe
    public async Task DatosIncorrectos_NoEntra(string correo, string contrasena)
    {
        var resp = await EntrarAsync(NuevoCliente(), correo, contrasena);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Correo o contraseña incorrectos.", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task UsuarioDesactivado_NoEntra()
    {
        var resp = await EntrarAsync(NuevoCliente(), "inactivo@prueba.com", FabricaLogin.Contrasena);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Este usuario está desactivado.", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task CerrarSesion_VuelveAPedirLogin()
    {
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, "administrador@prueba.com", FabricaLogin.Contrasena);

        var html = await cliente.GetStringAsync("/");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var salir = await cliente.PostAsync("/Cuenta/Logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, salir.StatusCode);

        var resp = await cliente.GetAsync("/Residentes");
        Assert.StartsWith("/Cuenta/Login", resp.Headers.Location!.PathAndQuery);
    }
}
