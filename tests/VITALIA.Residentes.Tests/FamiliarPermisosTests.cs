using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>
/// Más pruebas de la vista del familiar: residentes dados de baja, varios residentes,
/// alertas atendidas y que no pueda entrar ni cambiar nada del personal. Datos ficticios.
/// </summary>
[Collection("Login")]
public class FamiliarPermisosTests : IClassFixture<FabricaLogin>
{
    private readonly FabricaLogin _factory;

    public FamiliarPermisosTests(FabricaLogin factory)
    {
        _factory = factory;
        _factory.Sembrar();
    }

    private record Datos(string Sufijo, string Correo, int Activo, string NombreActivo, int Segundo, string NombreSegundo, int DeBaja, string NombreDeBaja);

    // Un familiar vinculado a dos residentes activos y a uno dado de baja.
    // El primero solo tiene una alerta ya atendida; el segundo, una pendiente.
    private Datos CrearDatos()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string s = Guid.NewGuid().ToString("N")[..6];
        int Rol(string nombre) => db.Roles.First(r => r.NombreRol == nombre).IdRol;

        var familiar = new Usuario { IdRol = Rol(RolesSistema.Familiar), Correo = "varios" + s + "@prueba.com", ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(FabricaLogin.Contrasena), Estado = true };
        var creador = new Usuario { IdRol = Rol(RolesSistema.Enfermero), Correo = "creador" + s + "@prueba.com", ContrasenaHash = "x", Estado = true };
        db.AddRange(familiar, creador);
        db.SaveChanges();

        Residente Nuevo(string nombre, bool activo) => new()
        {
            CodigoUnico = "V-" + nombre[..2] + s, Nombre = nombre + s, Apellido = "Ficticio", FechaNacimiento = new DateOnly(1942, 5, 20),
            NumHabitacion = "110", ContactoEmergencia = "Carlos · 7000-1111", Estado = activo
        };
        var activo = Nuevo("Tomas", true);
        var segundo = Nuevo("Elena", true);
        var deBaja = Nuevo("Baja", false);
        db.AddRange(activo, segundo, deBaja);
        db.SaveChanges();

        foreach (var r in new[] { activo, segundo, deBaja })
            db.Add(new FamiliarResidente { IdResidente = r.IdResidente, IdUsuario = familiar.IdUsuario, Parentesco = "Nieto", TelefonoContacto = "7000-1111" });

        db.Add(new Alerta { IdResidente = activo.IdResidente, IdUsuarioCreador = creador.IdUsuario, Titulo = "Ya resuelta " + s, Descripcion = "-", FechaProgramada = DateTime.Now.AddDays(-1), EstadoActual = AlertaEstados.Atendida, Prioridad = AlertaPrioridades.Rutina });
        db.Add(new Alerta { IdResidente = segundo.IdResidente, IdUsuarioCreador = creador.IdUsuario, Titulo = "Pendiente " + s, Descripcion = "-", FechaProgramada = DateTime.Now.AddDays(1), EstadoActual = AlertaEstados.Pendiente, Prioridad = AlertaPrioridades.Programada });
        db.SaveChanges();

        return new Datos(s, familiar.Correo, activo.IdResidente, activo.Nombre + " Ficticio", segundo.IdResidente, segundo.Nombre + " Ficticio",
            deBaja.IdResidente, deBaja.Nombre + " Ficticio");
    }

    private async Task<HttpClient> EntrarAsync(string correo)
    {
        var cliente = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var html = await (await cliente.GetAsync("/Cuenta/Login")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        await cliente.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Correo"] = correo, ["Contrasena"] = FabricaLogin.Contrasena, ["__RequestVerificationToken"] = token
        }));
        return cliente;
    }

    private static void EsAccesoDenegado(HttpResponseMessage resp)
    {
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task ConVariosResidentes_VeLosActivos_YNoAlDadoDeBaja()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Familiar"));

        Assert.Contains(d.NombreActivo, html);
        Assert.Contains(d.NombreSegundo, html);
        Assert.DoesNotContain(d.NombreDeBaja, html);
    }

    [Fact]
    public async Task ResidenteDadoDeBaja_NoSeAbreNiSeDescarga()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        EsAccesoDenegado(await cliente.GetAsync("/Familiar/Residente/" + d.DeBaja));
        EsAccesoDenegado(await cliente.GetAsync("/Familiar/ExportarPdf/" + d.DeBaja));
    }

    [Fact]
    public async Task ResidenteQueNoExiste_DaAccesoDenegado()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        EsAccesoDenegado(await cliente.GetAsync("/Familiar/Residente/999999"));
    }

    [Fact]
    public async Task LasAlertasAtendidas_NoCuentanComoActivas()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Familiar"));

        // Uno tiene solo una atendida y el otro una pendiente
        Assert.Contains("Sin alertas activas", html);
        Assert.Contains("1 alerta(s) activa(s)", html);
    }

    [Fact]
    public async Task LaFicha_SeparaLasAlertasActivasDeLasAtendidas()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        var resp = await cliente.GetAsync("/Familiar/Residente/" + d.Activo);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());

        Assert.Contains("Ya resuelta", html);
        Assert.Contains("Usted es su nieto", html);
        Assert.DoesNotContain("Pendiente " + d.Sufijo, html); // la alerta del otro residente no se mezcla
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Residentes")]
    [InlineData("/Bitacora")]
    [InlineData("/Alertas")]
    [InlineData("/Farmacia")]
    [InlineData("/TipoAtencion")]
    [InlineData("/Personal")]
    [InlineData("/Usuario")]
    [InlineData("/Respaldos")]
    [InlineData("/DatosPresentacion")]
    [InlineData("/Bitacora/ExportarPdf")]
    public async Task NoEntraALasPantallasDelPersonal(string url)
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        var resp = await cliente.GetAsync(url);

        // "/" lo manda a su vista; lo demás es acceso denegado
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    [Theory]
    [InlineData("/Residentes/Delete/{0}")]
    [InlineData("/Bitacora/Eliminar")]
    [InlineData("/Alertas/CambiarEstado")]
    [InlineData("/VinculosFamiliares/Desvincular/1")]
    public async Task NoPuedeCambiarNada(string plantilla)
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);

        // Toma un token válido de su propia pantalla
        var html = await cliente.GetStringAsync("/Familiar");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var resp = await cliente.PostAsync(string.Format(plantilla, d.Activo), new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = d.Activo.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        EsAccesoDenegado(resp);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        Assert.True(db.Residentes.Find(d.Activo)!.Estado);
    }

    [Fact]
    public async Task SiLeQuitanElVinculo_YaNoVeAlResidente()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.Correo);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Familiar/Residente/" + d.Activo)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
            var vinculo = db.FamiliarResidentes.First(f => f.IdResidente == d.Activo);
            db.Remove(vinculo);
            db.SaveChanges();
        }

        // Con la misma sesión abierta, ya no lo puede ver
        EsAccesoDenegado(await cliente.GetAsync("/Familiar/Residente/" + d.Activo));
    }
}
