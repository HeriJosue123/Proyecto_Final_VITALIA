using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
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

/// <summary>Igual que FabricaResidentes, pero con su propia base en memoria para no mezclar datos con otras pruebas.</summary>
public class FabricaPresentacion : WebApplicationFactory<ResidentesController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<VitaliaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VitaliaDbContext>>();
            services.AddDbContext<VitaliaDbContext>(o => o.UseInMemoryDatabase("PresentacionPruebas"));
            services.AddAuthentication(o =>
                {
                    o.DefaultScheme = SesionDePrueba.Esquema;
                    o.DefaultAuthenticateScheme = SesionDePrueba.Esquema;
                    o.DefaultChallengeScheme = SesionDePrueba.Esquema;
                })
                .AddScheme<AuthenticationSchemeOptions, SesionDePrueba>(SesionDePrueba.Esquema, _ => { });
        });
    }
}

/// <summary>Cargar los datos de presentación desde el panel del administrador (una sola vez por base).</summary>
public class DatosPresentacionTests : IClassFixture<FabricaPresentacion>
{
    private readonly FabricaPresentacion _factory;
    private readonly HttpClient _client;

    public DatosPresentacionTests(FabricaPresentacion factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        if (!(db.Database.ProviderName ?? "").Contains("InMemory"))
            throw new InvalidOperationException("Las pruebas deben usar la base en memoria");
        // Cada prueba empieza sin datos de presentación
        DatosPresentacion.QuitarAsync(db).GetAwaiter().GetResult();
    }

    private async Task<HttpResponseMessage> CargarAsync()
    {
        var html = await (await _client.GetAsync("/DatosPresentacion")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        return await _client.PostAsync("/DatosPresentacion/Cargar",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
    }

    private T Con<T>(Func<VitaliaDbContext, T> consulta)
    {
        using var scope = _factory.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<VitaliaDbContext>());
    }

    [Fact]
    public async Task Cargar_LlenaTodosLosModulos()
    {
        var respuesta = await CargarAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Datos de presentación cargados", WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));

        Con(db =>
        {
            var residentes = db.Residentes.Where(r => r.CodigoUnico.StartsWith("RES-2026-1")).ToList();
            var ids = residentes.Select(r => r.IdResidente).ToList();
            Assert.Equal(16, residentes.Count);
            Assert.Single(residentes, r => !r.Estado); // uno dado de baja

            var porDefecto = new[] { "admin@vitalia.com", "enfermero@vitalia.com", "medico@vitalia.com" };
            var personal = db.Empleados.Include(e => e.IdUsuarioNavigation).ToList()
                .Where(e => e.IdUsuarioNavigation!.Correo.EndsWith("@vitalia.com") && !porDefecto.Contains(e.IdUsuarioNavigation.Correo))
                .ToList();
            Assert.Equal(11, personal.Count);
            Assert.Single(personal, e => !e.Estado); // una ya no trabaja
            Assert.Contains(db.Usuarios, u => u.Correo == "alicia.benitez@vitalia.com" && !u.Estado);

            Assert.Equal(30, db.AsignacionCuidados.Count(a => ids.Contains(a.IdResidente) && a.Estado));
            Assert.True(db.AsignacionCuidados.Count(a => ids.Contains(a.IdResidente) && !a.Estado && a.FechaFin != null) >= 5);
            Assert.Equal(10, db.FamiliarResidentes.Count(f => ids.Contains(f.IdResidente)));

            var medicamentos = db.Medicamentos.Where(m => m.Codigo.StartsWith("MED-1")).ToList();
            Assert.Equal(16, medicamentos.Count);
            Assert.Single(medicamentos, m => !m.Estado); // uno descontinuado

            Assert.True(db.AtencionDiaria.Count(a => ids.Contains(a.IdResidente)) > 500);
            Assert.Contains(db.CorreccionAtencions, c => c.TipoAccion == "Corrección");
            Assert.Contains(db.CorreccionAtencions, c => c.TipoAccion == "Anulación");

            var alertas = db.Alerta.Where(a => ids.Contains(a.IdResidente)).ToList();
            foreach (var estado in AlertaEstados.Todos) Assert.Contains(alertas, a => a.EstadoActual == estado);
            foreach (var prioridad in AlertaPrioridades.Todas) Assert.Contains(alertas, a => a.Prioridad == prioridad);
            Assert.Contains(alertas, a => a.Descripcion.Contains("[PRESCRIPCIÓN MÉDICA:"));
            Assert.True(db.HistorialEstadoAlerta.Any());

            // El residente dado de baja no tiene nada pendiente
            var baja = residentes.Single(r => !r.Estado).IdResidente;
            Assert.All(alertas.Where(a => a.IdResidente == baja), a => Assert.Equal(AlertaEstados.Atendida, a.EstadoActual));

            Assert.True(db.EntregaMedicamentos.Count(e => ids.Contains(e.IdResidente)) > 50);
            Assert.Contains(db.LoteMedicamentos, l => l.FechaVencimiento < DateTime.Today);
            // Nada queda en el futuro: la bitácora es hasta este momento
            Assert.DoesNotContain(db.AtencionDiaria, a => ids.Contains(a.IdResidente) && a.FechaHora > DateTime.Now);
            return 0;
        });
    }

    [Fact]
    public async Task AlTerminarDeCargar_LaOpcionDesaparece()
    {
        var antes = await (await _client.GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("href=\"/DatosPresentacion\"", antes);

        await CargarAsync();

        var despues = await (await _client.GetAsync("/")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("href=\"/DatosPresentacion\"", despues);
        var pagina = await _client.GetAsync("/DatosPresentacion");
        Assert.Equal(HttpStatusCode.Redirect, pagina.StatusCode);
        Assert.Equal("/", pagina.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CargarOtraVez_NoDuplicaNada()
    {
        var html = await (await _client.GetAsync("/DatosPresentacion")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var formulario = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };

        await _client.PostAsync("/DatosPresentacion/Cargar", new FormUrlEncodedContent(formulario));
        var antes = Con(db => (db.Residentes.Count(), db.Usuarios.Count(), db.AtencionDiaria.Count(), db.Alerta.Count(), db.Medicamentos.Count()));

        // Como si alguien le diera dos veces al botón
        var segunda = await _client.PostAsync("/DatosPresentacion/Cargar", new FormUrlEncodedContent(formulario));

        Assert.Equal(HttpStatusCode.Redirect, segunda.StatusCode);
        var despues = Con(db => (db.Residentes.Count(), db.Usuarios.Count(), db.AtencionDiaria.Count(), db.Alerta.Count(), db.Medicamentos.Count()));
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task NoTocaLosDatosQueYaExisten()
    {
        // Datos propios de alguien del grupo, uno con un código igual al de la presentación
        Con(db =>
        {
            db.Add(new Residente
            {
                CodigoUnico = "RES-2026-110", Nombre = "Marta", Apellido = "Propia", FechaNacimiento = new DateOnly(1950, 1, 1),
                NumHabitacion = "201", ContactoEmergencia = "Contacto · 7000-0000", Estado = true
            });
            db.Add(new Medicamento { Codigo = "MED-001", Nombre = "Medicamento propio", Presentacion = "Tableta", Estado = true });
            db.SaveChanges();
            return 0;
        });

        var html = WebUtility.HtmlDecode(await (await CargarAsync()).Content.ReadAsStringAsync());

        Assert.Contains("El código RES-2026-110 ya lo tiene Marta Propia", html);
        Con(db =>
        {
            Assert.Single(db.Residentes, r => r.CodigoUnico == "RES-2026-110" && r.Nombre == "Marta");
            Assert.Contains(db.Medicamentos, m => m.Codigo == "MED-001" && m.Nombre == "Medicamento propio");
            Assert.Equal(15, db.Residentes.Count(r => r.CodigoUnico.StartsWith("RES-2026-1") && r.Nombre != "Marta"));

            // Limpieza: lo propio no lo quita QuitarAsync
            db.RemoveRange(db.Residentes.Where(r => r.Nombre == "Marta" && r.Apellido == "Propia"));
            db.RemoveRange(db.Medicamentos.Where(m => m.Codigo == "MED-001"));
            db.SaveChanges();
            return 0;
        });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Residentes")]
    [InlineData("/Bitacora")]
    [InlineData("/Alertas")]
    [InlineData("/Farmacia")]
    [InlineData("/Personal")]
    [InlineData("/Usuario")]
    [InlineData("/Roles")]
    [InlineData("/TipoAtencion")]
    public async Task ConLosDatosCargados_LasPantallasAbrenBien(string ruta)
    {
        await CargarAsync();
        var respuesta = await _client.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task FichaDeUnResidente_MuestraSusDatos()
    {
        await CargarAsync();
        int id = Con(db => db.Residentes.First(r => r.CodigoUnico == "RES-2026-102").IdResidente);

        var html = WebUtility.HtmlDecode(await (await _client.GetAsync("/Residentes/Details/" + id)).Content.ReadAsStringAsync());

        Assert.Contains("José Ernesto", html);
        Assert.Contains("Diabetes mellitus tipo 2", html);
    }
}
