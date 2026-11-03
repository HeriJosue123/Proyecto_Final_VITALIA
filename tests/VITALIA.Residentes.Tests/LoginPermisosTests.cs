using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>
/// Más pruebas del inicio de sesión (a dónde manda, "Mantener la sesión", nombre en el menú)
/// y de los permisos de cada rol. Datos ficticios.
/// </summary>
[Collection("Login")]
public class LoginPermisosTests : IClassFixture<FabricaLogin>
{
    private readonly FabricaLogin _factory;

    public LoginPermisosTests(FabricaLogin factory)
    {
        _factory = factory;
        _factory.Sembrar();
    }

    private HttpClient NuevoCliente() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<HttpResponseMessage> EntrarAsync(HttpClient cliente, string correo, string contrasena = FabricaLogin.Contrasena,
        string? returnUrl = null, bool recordarme = false)
    {
        var html = await (await cliente.GetAsync("/Cuenta/Login")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var campos = new Dictionary<string, string>
        {
            ["Correo"] = correo,
            ["Contrasena"] = contrasena,
            ["__RequestVerificationToken"] = token
        };
        if (recordarme) campos["Recordarme"] = "true";
        string url = "/Cuenta/Login" + (returnUrl == null ? "" : "?returnUrl=" + Uri.EscapeDataString(returnUrl));
        return await cliente.PostAsync(url, new FormUrlEncodedContent(campos));
    }

    private static async Task<string> Texto(HttpResponseMessage resp) =>
        WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());

    // Crea un usuario con su empleado y devuelve el correo
    private string NuevoEmpleado(string rol, bool empleadoActivo, out string nombreCompleto)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string s = Guid.NewGuid().ToString("N")[..6];
        var usuario = new Usuario
        {
            IdRol = db.Roles.First(r => r.NombreRol == rol).IdRol,
            Correo = "con.nombre" + s + "@prueba.com",
            ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(FabricaLogin.Contrasena),
            Estado = true
        };
        db.Add(usuario);
        db.SaveChanges();
        db.Add(new Empleado { IdUsuario = usuario.IdUsuario, Nombre = "Lorena" + s, Apellido = "Prueba", Cargo = "Enfermera", Estado = empleadoActivo });
        db.SaveChanges();
        nombreCompleto = "Lorena" + s + " Prueba";
        return usuario.Correo;
    }

    // ------------------------------------------------------------ a dónde manda el login

    [Fact]
    public async Task ConSesionIniciada_LaPaginaDeLogin_MandaAlInicio()
    {
        var personal = NuevoCliente();
        await EntrarAsync(personal, "administrador@prueba.com");
        var resp = await personal.GetAsync("/Cuenta/Login");
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/", resp.Headers.Location!.OriginalString);

        var familiar = NuevoCliente();
        await EntrarAsync(familiar, "familiar@prueba.com");
        var respFamiliar = await familiar.GetAsync("/Cuenta/Login");
        Assert.Equal("/Familiar", respFamiliar.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ConReturnUrlLocal_VuelveALaPaginaQuePidio()
    {
        var resp = await EntrarAsync(NuevoCliente(), "enfermero@prueba.com", returnUrl: "/Bitacora");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/Bitacora", resp.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("https://sitio-falso.com/robar")]
    [InlineData("//sitio-falso.com")]
    public async Task ConReturnUrlDeOtroSitio_NoSaleDelSistema(string returnUrl)
    {
        var resp = await EntrarAsync(NuevoCliente(), "administrador@prueba.com", returnUrl: returnUrl);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/", resp.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Familiar_ConReturnUrlDelPersonal_VaASuVista()
    {
        var resp = await EntrarAsync(NuevoCliente(), "familiar@prueba.com", returnUrl: "/Residentes");

        Assert.Equal("/Familiar", resp.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task CorreoConEspacios_EntraIgual()
    {
        var resp = await EntrarAsync(NuevoCliente(), "  administrador@prueba.com  ");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/", resp.Headers.Location!.OriginalString);
    }

    // ------------------------------------------------------------ formulario y cookie

    [Fact]
    public async Task FormularioVacio_MuestraLosMensajes()
    {
        var resp = await EntrarAsync(NuevoCliente(), "", "");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await Texto(resp);
        Assert.Contains("El correo es obligatorio.", html);
        Assert.Contains("La contraseña es obligatoria.", html);
    }

    [Fact]
    public async Task ContrasenaGuardadaSinEncriptar_NoEntra()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
            if (!db.Usuarios.Any(u => u.Correo == "texto.plano@prueba.com"))
            {
                db.Add(new Usuario
                {
                    IdRol = db.Roles.First(r => r.NombreRol == RolesSistema.Enfermero).IdRol,
                    Correo = "texto.plano@prueba.com",
                    ContrasenaHash = FabricaLogin.Contrasena, // guardada tal cual, sin BCrypt
                    Estado = true
                });
                db.SaveChanges();
            }
        }

        var resp = await EntrarAsync(NuevoCliente(), "texto.plano@prueba.com");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Correo o contraseña incorrectos.", await Texto(resp));
    }

    [Fact]
    public async Task MantenerLaSesion_GuardaLaCookieConFechaDeVencimiento()
    {
        var sinRecordar = await EntrarAsync(NuevoCliente(), "enfermero@prueba.com");
        var conRecordar = await EntrarAsync(NuevoCliente(), "enfermero@prueba.com", recordarme: true);

        string CookieDeSesion(HttpResponseMessage r) =>
            r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(".AspNetCore.Cookies"));

        Assert.DoesNotContain("expires=", CookieDeSesion(sinRecordar), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", CookieDeSesion(conRecordar), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmpleadoActivo_VeSuNombreEnElMenu()
    {
        string correo = NuevoEmpleado(RolesSistema.Enfermero, empleadoActivo: true, out string nombre);
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, correo);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Residentes"));

        Assert.Contains(nombre, html);
    }

    [Fact]
    public async Task EmpleadoDadoDeBaja_ConCuentaActiva_VeSuCorreoEnLugarDelNombre()
    {
        string correo = NuevoEmpleado(RolesSistema.Enfermero, empleadoActivo: false, out string nombre);
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, correo);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Residentes"));

        Assert.DoesNotContain(nombre, html);
        Assert.Contains(correo, html);
    }

    // ------------------------------------------------------------ permisos por rol

    [Theory]
    [InlineData("/Familiar")]
    [InlineData("/Bitacora")]
    [InlineData("/Alertas")]
    [InlineData("/Farmacia")]
    [InlineData("/DatosPresentacion")]
    [InlineData("/Perfil/CambiarContrasena")]
    [InlineData("/Familiar/ExportarPdf/1")]
    public async Task SinSesion_TodoMandaAlLogin(string url)
    {
        var resp = await NuevoCliente().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/Login", resp.Headers.Location!.PathAndQuery);
    }

    [Theory]
    [InlineData("/Personal")]
    [InlineData("/Personal/Asignar")]
    [InlineData("/Usuario")]
    [InlineData("/Roles")]
    [InlineData("/Respaldos")]
    [InlineData("/DatosPresentacion")]
    [InlineData("/VinculosFamiliares/Create?idResidente=1")]
    public async Task Enfermero_NoEntraALoDelAdministrador(string url)
    {
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, "enfermero@prueba.com");

        var resp = await cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Residentes")]
    [InlineData("/Bitacora")]
    [InlineData("/Alertas")]
    [InlineData("/Farmacia")]
    [InlineData("/TipoAtencion")]
    [InlineData("/Perfil/CambiarContrasena")]
    public async Task Enfermero_EntraALoClinico(string url)
    {
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, "enfermero@prueba.com");

        var resp = await cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task AccesoDenegado_SeMuestraAlPersonal()
    {
        var cliente = NuevoCliente();
        await EntrarAsync(cliente, "enfermero@prueba.com");

        var resp = await cliente.GetAsync("/Cuenta/AccesoDenegado?ReturnUrl=%2FPersonal");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
