using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>Módulo Personal: registrar, editar y dar de baja empleados, y finalizar asignaciones. Datos ficticios.</summary>
public class PersonalTests : IClassFixture<FabricaResidentes>
{
    private readonly FabricaResidentes _factory;
    private readonly HttpClient _client;

    public PersonalTests(FabricaResidentes factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private T Con<T>(Func<VitaliaDbContext, T> consulta)
    {
        using var scope = _factory.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<VitaliaDbContext>());
    }

    private static string Sufijo() => Guid.NewGuid().ToString("N")[..6];

    // Cuenta sin empleado (para poder registrarlo)
    private int NuevoUsuario(string sufijo, string rol = RolesSistema.Enfermero) => Con(db =>
    {
        var usuario = new Usuario
        {
            IdRol = db.Roles.First(r => r.NombreRol == rol).IdRol,
            Correo = "persona" + sufijo + "@prueba.com",
            ContrasenaHash = "x",
            Estado = true
        };
        db.Add(usuario);
        db.SaveChanges();
        return usuario.IdUsuario;
    });

    private int NuevoEmpleado(string sufijo, bool activo = true) => Con(db =>
    {
        var usuario = new Usuario
        {
            IdRol = db.Roles.First(r => r.NombreRol == RolesSistema.Enfermero).IdRol,
            Correo = "empleado" + sufijo + "@prueba.com",
            ContrasenaHash = "x",
            Estado = activo
        };
        db.Add(usuario);
        db.SaveChanges();
        var empleado = new Empleado { IdUsuario = usuario.IdUsuario, Nombre = "Nombre" + sufijo, Apellido = "Prueba", Cargo = "Cuidador", Estado = activo };
        db.Add(empleado);
        db.SaveChanges();
        return empleado.IdEmpleado;
    });

    private async Task<string> TokenDe(string url)
    {
        var html = await (await _client.GetAsync(url)).Content.ReadAsStringAsync();
        return Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    }

    private async Task<HttpResponseMessage> EnviarAsync(string urlFormulario, string urlPost, Dictionary<string, string> campos)
    {
        campos["__RequestVerificationToken"] = await TokenDe(urlFormulario);
        return await _client.PostAsync(urlPost, new FormUrlEncodedContent(campos));
    }

    private static async Task<string> Texto(HttpResponseMessage resp) =>
        WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());

    [Fact]
    public async Task Index_MuestraActivosEInactivos()
    {
        string s1 = Sufijo(), s2 = Sufijo();
        NuevoEmpleado(s1);
        NuevoEmpleado(s2, activo: false);

        var resp = await _client.GetAsync("/Personal");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await Texto(resp);

        // El filtro "Inactivos" de la pantalla necesita que vengan los dos
        Assert.Contains("Nombre" + s1, html);
        Assert.Contains("Nombre" + s2, html);
        Assert.Contains("data-estado=\"inactivo\"", html);
    }

    [Fact]
    public async Task Create_MuestraSoloUsuariosSinEmpleado()
    {
        string libre = Sufijo(), ocupado = Sufijo();
        NuevoUsuario(libre);
        NuevoEmpleado(ocupado);

        var html = await Texto(await _client.GetAsync("/Personal/Create"));

        Assert.Contains("persona" + libre + "@prueba.com", html);
        Assert.DoesNotContain("empleado" + ocupado + "@prueba.com", html);
    }

    [Fact]
    public async Task Create_ConUsuarioLibre_RegistraAlEmpleadoActivo()
    {
        string s = Sufijo();
        int idUsuario = NuevoUsuario(s);

        var resp = await EnviarAsync("/Personal/Create", "/Personal/Create", new()
        {
            ["IdUsuario"] = idUsuario.ToString(),
            ["Nombre"] = "Carmen" + s,
            ["Apellido"] = "Prueba",
            ["Cargo"] = "Enfermera"
        });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var empleado = Con(db => db.Empleados.Single(e => e.IdUsuario == idUsuario));
        Assert.Equal("Carmen" + s, empleado.Nombre);
        Assert.Equal("Enfermera", empleado.Cargo);
        Assert.True(empleado.Estado);
    }

    [Fact]
    public async Task Create_SinDatos_MuestraLosMensajesEnEspanol()
    {
        var resp = await EnviarAsync("/Personal/Create", "/Personal/Create", new()
        {
            ["IdUsuario"] = "0",
            ["Nombre"] = "",
            ["Apellido"] = "",
            ["Cargo"] = ""
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await Texto(resp);
        Assert.Contains("Debe seleccionar un usuario.", html);
        Assert.Contains("El nombre es obligatorio.", html);
        Assert.Contains("El apellido es obligatorio.", html);
        Assert.Contains("El cargo es obligatorio.", html);
    }

    [Fact]
    public async Task Create_ConUsuarioQueYaEsEmpleado_NoDeja()
    {
        string s = Sufijo();
        int idEmpleado = NuevoEmpleado(s);
        int idUsuario = Con(db => db.Empleados.Find(idEmpleado)!.IdUsuario);

        var resp = await EnviarAsync("/Personal/Create", "/Personal/Create", new()
        {
            ["IdUsuario"] = idUsuario.ToString(),
            ["Nombre"] = "Otro" + s,
            ["Apellido"] = "Prueba",
            ["Cargo"] = "Cuidador"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Ese usuario ya está vinculado a otro empleado.", await Texto(resp));
        Assert.Equal(1, Con(db => db.Empleados.Count(e => e.IdUsuario == idUsuario)));
    }

    [Fact]
    public async Task Edit_CambiaLosDatos()
    {
        string s = Sufijo();
        int id = NuevoEmpleado(s);
        int idUsuario = Con(db => db.Empleados.Find(id)!.IdUsuario);

        var resp = await EnviarAsync("/Personal/Edit/" + id, "/Personal/Edit/" + id, new()
        {
            ["IdEmpleado"] = id.ToString(),
            ["IdUsuario"] = idUsuario.ToString(),
            ["Nombre"] = "Cambiado" + s,
            ["Apellido"] = "Prueba",
            ["Cargo"] = "Enfermero jefe"
        });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var empleado = Con(db => db.Empleados.Find(id)!);
        Assert.Equal("Cambiado" + s, empleado.Nombre);
        Assert.Equal("Enfermero jefe", empleado.Cargo);
    }

    [Fact]
    public async Task Edit_ConElUsuarioDeOtroEmpleado_NoDeja()
    {
        int id = NuevoEmpleado(Sufijo());
        int otro = NuevoEmpleado(Sufijo());
        int usuarioDelOtro = Con(db => db.Empleados.Find(otro)!.IdUsuario);
        int usuarioPropio = Con(db => db.Empleados.Find(id)!.IdUsuario);

        var resp = await EnviarAsync("/Personal/Edit/" + id, "/Personal/Edit/" + id, new()
        {
            ["IdEmpleado"] = id.ToString(),
            ["IdUsuario"] = usuarioDelOtro.ToString(),
            ["Nombre"] = "Nombre",
            ["Apellido"] = "Prueba",
            ["Cargo"] = "Cuidador"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Ese usuario ya está vinculado a otro empleado.", await Texto(resp));
        Assert.Equal(usuarioPropio, Con(db => db.Empleados.Find(id)!.IdUsuario));
    }

    [Fact]
    public async Task Edit_ConIdQueNoCoincide_DaNoEncontrado()
    {
        int id = NuevoEmpleado(Sufijo());

        var resp = await EnviarAsync("/Personal/Edit/" + id, "/Personal/Edit/" + id, new()
        {
            ["IdEmpleado"] = (id + 1000).ToString(),
            ["IdUsuario"] = "1",
            ["Nombre"] = "Nombre",
            ["Apellido"] = "Prueba",
            ["Cargo"] = "Cuidador"
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_EsBajaLogica_ElEmpleadoSeConserva()
    {
        string s = Sufijo();
        int id = NuevoEmpleado(s);

        var pantalla = await _client.GetAsync("/Personal/Delete/" + id);
        Assert.Equal(HttpStatusCode.OK, pantalla.StatusCode);

        var resp = await EnviarAsync("/Personal/Delete/" + id, "/Personal/Delete/" + id, new());

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var empleado = Con(db => db.Empleados.Find(id));
        Assert.NotNull(empleado);
        Assert.False(empleado!.Estado);
    }

    [Theory]
    [InlineData("/Personal/Details/999999")]
    [InlineData("/Personal/Edit/999999")]
    [InlineData("/Personal/Delete/999999")]
    public async Task EmpleadoQueNoExiste_DaNoEncontrado(string url)
    {
        var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Details_MuestraAlEmpleado()
    {
        string s = Sufijo();
        int id = NuevoEmpleado(s);

        var resp = await _client.GetAsync("/Personal/Details/" + id);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await Texto(resp);
        Assert.Contains("Nombre" + s, html);
        Assert.Contains("empleado" + s + "@prueba.com", html);
    }

    [Fact]
    public async Task FinalizarAsignacion_GuardaLaFechaDeFin()
    {
        int idEmpleado = NuevoEmpleado(Sufijo());
        int idAsignacion = Con(db =>
        {
            var r = new Residente
            {
                CodigoUnico = "P-" + Sufijo(), Nombre = "Rosa", Apellido = "Ficticia", FechaNacimiento = new DateOnly(1944, 2, 2),
                NumHabitacion = "102", ContactoEmergencia = "Contacto · 7000-0000", Estado = true
            };
            db.Add(r);
            db.SaveChanges();
            var a = new AsignacionCuidado { IdEmpleado = idEmpleado, IdResidente = r.IdResidente, FechaInicio = DateTime.Now.AddDays(-10), Estado = true };
            db.Add(a);
            db.SaveChanges();
            return a.IdAsignacion;
        });

        var resp = await EnviarAsync("/Personal", "/Personal/FinalizarAsignacion", new() { ["id"] = idAsignacion.ToString() });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var asignacion = Con(db => db.AsignacionCuidados.Find(idAsignacion)!);
        Assert.False(asignacion.Estado);
        Assert.NotNull(asignacion.FechaFin);
        Assert.True(asignacion.FechaFin >= asignacion.FechaInicio);
    }

    [Fact]
    public async Task Asignar_SinElegirNada_MuestraLosMensajes()
    {
        var resp = await EnviarAsync("/Personal/Asignar", "/Personal/Asignar", new()
        {
            ["IdEmpleado"] = "0",
            ["IdResidente"] = "0"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await Texto(resp);
        Assert.Contains("Debe seleccionar un empleado.", html);
        Assert.Contains("Debe seleccionar un residente.", html);
    }

    [Fact]
    public async Task Asignar_NoOfreceEmpleadosInactivos()
    {
        string activo = Sufijo(), inactivo = Sufijo();
        NuevoEmpleado(activo);
        NuevoEmpleado(inactivo, activo: false);

        var html = await Texto(await _client.GetAsync("/Personal/Asignar"));

        Assert.Contains("Nombre" + activo, html);
        Assert.DoesNotContain("Nombre" + inactivo, html);
    }
}
