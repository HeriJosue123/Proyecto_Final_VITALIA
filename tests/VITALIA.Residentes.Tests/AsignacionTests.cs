using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>
/// Regla de asignaciones: cada residente puede tener un médico y un cuidador (o enfermero),
/// pero no dos del mismo tipo. Datos ficticios.
/// </summary>
public class AsignacionTests : IClassFixture<FabricaResidentes>
{
    private readonly FabricaResidentes _factory;
    private readonly HttpClient _client;

    public AsignacionTests(FabricaResidentes factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private record Datos(int Residente, string NombreResidente, int Cuidador1, string NombreCuidador1, int Cuidador2, int Medico1, string NombreMedico1, int Medico2);

    // Crea un residente, dos cuidadores y dos médicos nuevos para que cada prueba sea independiente
    private Datos CrearDatos()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string sufijo = Guid.NewGuid().ToString("N")[..6];

        Empleado NuevoEmpleado(string rol, string nombre, string cargo)
        {
            var usuario = new Usuario
            {
                IdRol = db.Roles.First(r => r.NombreRol == rol).IdRol,
                Correo = nombre.ToLower() + sufijo + "@prueba.com",
                ContrasenaHash = "x",
                Estado = true
            };
            db.Add(usuario);
            db.SaveChanges();
            var empleado = new Empleado { IdUsuario = usuario.IdUsuario, Nombre = nombre + sufijo, Apellido = "Prueba", Cargo = cargo, Estado = true };
            db.Add(empleado);
            db.SaveChanges();
            return empleado;
        }

        var c1 = NuevoEmpleado(RolesSistema.Enfermero, "Karla", "Cuidadora");
        var c2 = NuevoEmpleado(RolesSistema.Enfermero, "Oscar", "Cuidador");
        var m1 = NuevoEmpleado(RolesSistema.Medico, "Roberto", "Médico general");
        var m2 = NuevoEmpleado(RolesSistema.Medico, "Lucia", "Geriatra");
        var r = new Residente
        {
            CodigoUnico = "A-" + sufijo, Nombre = "Marta" + sufijo, Apellido = "Ficticia", FechaNacimiento = new DateOnly(1945, 1, 1),
            NumHabitacion = "101", ContactoEmergencia = "Contacto · 7000-0000", Estado = true
        };
        db.Add(r);
        db.SaveChanges();

        return new Datos(r.IdResidente, r.Nombre + " Ficticia",
            c1.IdEmpleado, c1.Nombre + " Prueba", c2.IdEmpleado,
            m1.IdEmpleado, m1.Nombre + " Prueba", m2.IdEmpleado);
    }

    private async Task<string> TokenDe(string url)
    {
        var html = await (await _client.GetAsync(url)).Content.ReadAsStringAsync();
        return Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    }

    private async Task<HttpResponseMessage> AsignarAsync(int empleado, int residente) =>
        await _client.PostAsync("/Personal/Asignar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["IdEmpleado"] = empleado.ToString(),
            ["IdResidente"] = residente.ToString(),
            ["__RequestVerificationToken"] = await TokenDe("/Personal/Asignar")
        }));

    private int AsignacionesActivas(int residente)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        return db.AsignacionCuidados.Count(a => a.IdResidente == residente && a.Estado);
    }

    [Fact]
    public async Task ResidenteSinAsignaciones_SeAsigna()
    {
        var d = CrearDatos();

        var resp = await AsignarAsync(d.Cuidador1, d.Residente);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal(1, AsignacionesActivas(d.Residente));
    }

    [Fact]
    public async Task UnCuidadorYUnMedico_SePermiten()
    {
        var d = CrearDatos();

        var r1 = await AsignarAsync(d.Cuidador1, d.Residente);
        var r2 = await AsignarAsync(d.Medico1, d.Residente);

        Assert.Equal(HttpStatusCode.Redirect, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, r2.StatusCode);
        Assert.Equal(2, AsignacionesActivas(d.Residente));
    }

    [Fact]
    public async Task DosCuidadores_NoSePermiten()
    {
        var d = CrearDatos();
        await AsignarAsync(d.Cuidador1, d.Residente);

        var resp = await AsignarAsync(d.Cuidador2, d.Residente);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
        Assert.Contains(d.NombreResidente + " ya tiene un cuidador asignado (" + d.NombreCuidador1 + ")", html);
        Assert.Equal(1, AsignacionesActivas(d.Residente));
    }

    [Fact]
    public async Task DosMedicos_NoSePermiten()
    {
        var d = CrearDatos();
        await AsignarAsync(d.Medico1, d.Residente);

        var resp = await AsignarAsync(d.Medico2, d.Residente);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
        Assert.Contains(d.NombreResidente + " ya tiene un médico asignado (" + d.NombreMedico1 + ")", html);
        Assert.Equal(1, AsignacionesActivas(d.Residente));
    }

    [Fact]
    public async Task FormularioAsignar_MuestraQuienAtiendeACadaResidente()
    {
        var d = CrearDatos();
        await AsignarAsync(d.Cuidador1, d.Residente);
        await AsignarAsync(d.Medico1, d.Residente);

        var html = WebUtility.HtmlDecode(await _client.GetStringAsync("/Personal/Asignar"));

        string listaResidentes = html.Substring(html.IndexOf("name=\"IdResidente\""));
        var opcion = Regex.Match(listaResidentes, "<option value=\"" + d.Residente + "\"[^>]*>[^<]*</option>").Value;
        Assert.Contains("data-cuidador=\"1\"", opcion);
        Assert.Contains("data-medico=\"1\"", opcion);
        Assert.Contains("(cuidador: " + d.NombreCuidador1 + " · médico: " + d.NombreMedico1 + ")", opcion);
    }

    [Fact]
    public async Task AlFinalizarLaAsignacion_SePuedeAsignarOtroDelMismoTipo()
    {
        var d = CrearDatos();
        await AsignarAsync(d.Cuidador1, d.Residente);
        int idAsignacion;
        using (var scope = _factory.Services.CreateScope())
        {
            idAsignacion = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>()
                .AsignacionCuidados.Single(a => a.IdResidente == d.Residente && a.Estado).IdAsignacion;
        }

        var fin = await _client.PostAsync("/Personal/FinalizarAsignacion/" + idAsignacion, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = await TokenDe("/Personal")
        }));
        Assert.Equal(HttpStatusCode.Redirect, fin.StatusCode);

        var resp = await AsignarAsync(d.Cuidador2, d.Residente);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal(1, AsignacionesActivas(d.Residente));
    }
}
