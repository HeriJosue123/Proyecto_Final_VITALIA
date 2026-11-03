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
using VITALIA.Models.ViewModels;

namespace VITALIA.Residentes.Tests;

/// <summary>Como FabricaPresentacion, pero con otra base en memoria para que las dos clases no se pisen.</summary>
public class FabricaPresentacionDetalle : WebApplicationFactory<ResidentesController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<VitaliaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VitaliaDbContext>>();
            services.AddDbContext<VitaliaDbContext>(o => o.UseInMemoryDatabase("PresentacionDetallePruebas"));
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

/// <summary>
/// Revisa que el paquete de datos de presentación quede coherente: asignaciones, familiares,
/// farmacia, la residente dada de baja y que siempre cargue lo mismo.
/// </summary>
public class DatosPresentacionDetalleTests : IClassFixture<FabricaPresentacionDetalle>
{
    private readonly FabricaPresentacionDetalle _factory;
    private readonly HttpClient _client;

    public DatosPresentacionDetalleTests(FabricaPresentacionDetalle factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Cada prueba empieza sin datos de presentación
        Con(db =>
        {
            if (!(db.Database.ProviderName ?? "").Contains("InMemory"))
                throw new InvalidOperationException("Las pruebas deben usar la base en memoria");
            DatosPresentacion.QuitarAsync(db).GetAwaiter().GetResult();
            return 0;
        });
    }

    private T Con<T>(Func<VitaliaDbContext, T> consulta)
    {
        using var scope = _factory.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<VitaliaDbContext>());
    }

    private async Task<HttpResponseMessage> CargarAsync()
    {
        var html = await (await _client.GetAsync("/DatosPresentacion")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        return await _client.PostAsync("/DatosPresentacion/Cargar",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
    }

    private List<Residente> ResidentesDelPaquete(VitaliaDbContext db) =>
        db.Residentes.Where(r => r.CodigoUnico.StartsWith("RES-2026-1")).ToList();

    [Fact]
    public async Task AntesDeCargar_LaPantallaSeVe()
    {
        var resp = await _client.GetAsync("/DatosPresentacion");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Cargar datos de presentación", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
        Assert.False(Con(db => DatosPresentacion.CargadosAsync(db).GetAwaiter().GetResult()));
    }

    [Fact]
    public async Task AlCargar_LaPantallaFinalMuestraLasCuentas()
    {
        var resp = await CargarAsync();
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());

        foreach (var (correo, _, _) in DatosPresentacion.Cuentas())
            Assert.Contains(correo, html);
    }

    [Fact]
    public async Task TodasLasCuentas_ExistenConSuRol()
    {
        await CargarAsync();

        Con(db =>
        {
            var usuarios = db.Usuarios.Include(u => u.IdRolNavigation).ToList();
            foreach (var (correo, _, rol) in DatosPresentacion.Cuentas())
            {
                var usuario = usuarios.SingleOrDefault(u => u.Correo == correo);
                Assert.True(usuario != null, "Falta la cuenta " + correo);
                Assert.Equal(rol, usuario!.IdRolNavigation.NombreRol);
                Assert.StartsWith("$2", usuario.ContrasenaHash); // encriptada con BCrypt
            }
            // Solo la cuidadora que ya no trabaja queda desactivada
            Assert.Single(usuarios, u => DatosPresentacion.Cuentas().Any(c => c.Correo == u.Correo) && !u.Estado);
            return 0;
        });
    }

    [Fact]
    public async Task CadaResidenteActivo_TieneUnMedicoYUnCuidador()
    {
        await CargarAsync();

        Con(db =>
        {
            var activos = ResidentesDelPaquete(db).Where(r => r.Estado).Select(r => r.IdResidente).ToList();
            Assert.Equal(15, activos.Count);

            var asignaciones = db.AsignacionCuidados
                .Include(a => a.IdEmpleadoNavigation).ThenInclude(e => e.IdUsuarioNavigation).ThenInclude(u => u!.IdRolNavigation)
                .Where(a => activos.Contains(a.IdResidente) && a.Estado)
                .ToList();

            foreach (var id in activos)
            {
                var delResidente = asignaciones.Where(a => a.IdResidente == id).ToList();
                Assert.Single(delResidente, a => TiposAsignacion.De(a.IdEmpleadoNavigation) == TiposAsignacion.Medico);
                Assert.Single(delResidente, a => TiposAsignacion.De(a.IdEmpleadoNavigation) == TiposAsignacion.Cuidador);
                Assert.All(delResidente, a => Assert.True(a.IdEmpleadoNavigation.Estado)); // nadie inactivo atiende
            }
            return 0;
        });
    }

    [Fact]
    public async Task ElResidenteDadoDeBaja_NoTieneNadaActivo()
    {
        await CargarAsync();

        Con(db =>
        {
            var baja = db.Residentes.Single(r => r.CodigoUnico == "RES-2026-116");
            Assert.False(baja.Estado);
            Assert.Equal("Ernesto Benjamín", baja.Nombre);

            var asignaciones = db.AsignacionCuidados.Where(a => a.IdResidente == baja.IdResidente).ToList();
            Assert.NotEmpty(asignaciones);
            Assert.All(asignaciones, a => { Assert.False(a.Estado); Assert.NotNull(a.FechaFin); });

            Assert.DoesNotContain(db.EntregaMedicamentos, e => e.IdResidente == baja.IdResidente);
            Assert.DoesNotContain(db.AtencionDiaria, a => a.IdResidente == baja.IdResidente && a.FechaHora > DateTime.Now.AddDays(-44));
            return 0;
        });
    }

    [Fact]
    public async Task LaCuidadoraQueSeFue_TieneSoloAsignacionesFinalizadas()
    {
        await CargarAsync();

        Con(db =>
        {
            var alicia = db.Empleados.Include(e => e.IdUsuarioNavigation)
                .Single(e => e.IdUsuarioNavigation!.Correo == "alicia.benitez@vitalia.com");
            Assert.False(alicia.Estado);

            var suyas = db.AsignacionCuidados.Where(a => a.IdEmpleado == alicia.IdEmpleado).ToList();
            Assert.Equal(3, suyas.Count); // los 3 cambios de cuidador
            Assert.All(suyas, a => { Assert.False(a.Estado); Assert.NotNull(a.FechaFin); });
            return 0;
        });
    }

    [Fact]
    public async Task LosFamiliares_QuedanVinculadosASuResidente()
    {
        await CargarAsync();

        Con(db =>
        {
            var vinculos = db.FamiliarResidentes
                .Include(f => f.IdUsuarioNavigation).Include(f => f.IdResidenteNavigation)
                .Where(f => f.IdResidenteNavigation.CodigoUnico.StartsWith("RES-2026-1"))
                .ToList();

            Assert.Equal(10, vinculos.Count);
            Assert.Contains(vinculos, v => v.IdUsuarioNavigation.Correo == "sonia.molina@gmail.com" && v.IdResidenteNavigation.CodigoUnico == "RES-2026-101" && v.Parentesco == "Hija");
            // José Ernesto tiene a su hijo y a su nieta
            Assert.Equal(2, vinculos.Count(v => v.IdResidenteNavigation.CodigoUnico == "RES-2026-102"));
            Assert.All(vinculos, v => Assert.Matches(@"^\d{4}-\d{4}$", v.TelefonoContacto));
            Assert.All(vinculos, v => Assert.True(v.IdResidenteNavigation.Estado));
            return 0;
        });
    }

    [Fact]
    public async Task Farmacia_TieneLosCasosParaMostrar()
    {
        await CargarAsync();

        Con(db =>
        {
            var meds = db.Medicamentos.Include(m => m.LoteMedicamentos).Where(m => m.Codigo.StartsWith("MED-1")).ToList();

            Assert.Equal(0, meds.Single(m => m.Nombre == "Ibuprofeno").LoteMedicamentos.Sum(l => l.Stock));       // agotado
            Assert.False(meds.Single(m => m.Nombre == "Ranitidina").Estado);                                      // descontinuado
            Assert.Contains(meds.Single(m => m.Nombre == "Atorvastatina").LoteMedicamentos, l => l.FechaVencimiento < DateTime.Today); // vencido
            Assert.InRange(meds.Single(m => m.Nombre == "Insulina NPH").LoteMedicamentos.Sum(l => l.Stock), 1, 15); // poco stock
            Assert.All(meds, m => Assert.NotEmpty(m.LoteMedicamentos));
            return 0;
        });
    }

    [Fact]
    public async Task LasEntregas_SalenDeLotesVigentesConStock()
    {
        await CargarAsync();

        Con(db =>
        {
            var entregas = db.EntregaMedicamentos.Include(e => e.IdLoteNavigation).ThenInclude(l => l.IdMedicamentoNavigation).ToList();
            Assert.NotEmpty(entregas);
            Assert.All(entregas, e =>
            {
                Assert.True(e.IdLoteNavigation.FechaVencimiento >= DateTime.Today, "Entrega de un lote vencido: " + e.IdLoteNavigation.NumeroLote);
                Assert.True(e.IdLoteNavigation.Stock > 0, "Entrega de un lote sin stock: " + e.IdLoteNavigation.NumeroLote);
                Assert.True(e.IdLoteNavigation.IdMedicamentoNavigation.Estado, "Entrega de un medicamento inactivo");
                Assert.True(e.FechaHora <= DateTime.Now);
            });
            return 0;
        });
    }

    [Fact]
    public async Task LasRecetas_LasCreaElMedicoDelResidente()
    {
        await CargarAsync();

        Con(db =>
        {
            var recetas = db.Alerta.Include(a => a.IdUsuarioCreadorNavigation).ThenInclude(u => u.IdRolNavigation)
                .Where(a => a.Descripcion.Contains("[PRESCRIPCIÓN MÉDICA:"))
                .ToList();

            Assert.NotEmpty(recetas);
            Assert.All(recetas, a => Assert.Equal(RolesSistema.Medico, a.IdUsuarioCreadorNavigation.IdRolNavigation.NombreRol));
            Assert.All(recetas, a => Assert.Matches(@"\[PRESCRIPCIÓN MÉDICA: .+ \(.+\) - Dosis: .+\]", a.Descripcion));
            return 0;
        });
    }

    [Fact]
    public async Task LasAlertasAtendidas_TienenSuHistorialCompleto()
    {
        await CargarAsync();

        Con(db =>
        {
            var ids = ResidentesDelPaquete(db).Select(r => r.IdResidente).ToList();
            var alertas = db.Alerta.Where(a => ids.Contains(a.IdResidente)).ToList();
            var historial = db.HistorialEstadoAlerta.ToList();

            foreach (var a in alertas)
            {
                var suyo = historial.Where(h => h.IdAlerta == a.IdAlerta).ToList();
                if (a.EstadoActual == AlertaEstados.Pendiente) Assert.Empty(suyo);
                if (a.EstadoActual == AlertaEstados.EnProceso) Assert.Single(suyo);
                if (a.EstadoActual == AlertaEstados.Atendida)
                {
                    Assert.Equal(2, suyo.Count);
                    Assert.Contains(suyo, h => h.EstadoNuevo == AlertaEstados.Atendida);
                }
            }
            return 0;
        });
    }

    [Fact]
    public async Task SiempreCargaLoMismo()
    {
        await CargarAsync();
        var primera = Con(db => (
            Atenciones: db.AtencionDiaria.Count(),
            Alertas: db.Alerta.Count(),
            Entregas: db.EntregaMedicamentos.Count(),
            Correcciones: db.CorreccionAtencions.Count()));

        Con(db => DatosPresentacion.QuitarAsync(db).GetAwaiter().GetResult());
        await CargarAsync();
        var segunda = Con(db => (
            Atenciones: db.AtencionDiaria.Count(),
            Alertas: db.Alerta.Count(),
            Entregas: db.EntregaMedicamentos.Count(),
            Correcciones: db.CorreccionAtencions.Count()));

        Assert.Equal(primera, segunda);
    }

    [Fact]
    public async Task Quitar_DejaLaBaseComoAntes()
    {
        var antes = Con(db => (db.Residentes.Count(), db.Usuarios.Count(), db.Empleados.Count(), db.Medicamentos.Count(), db.Alerta.Count()));

        await CargarAsync();
        Assert.True(Con(db => DatosPresentacion.CargadosAsync(db).GetAwaiter().GetResult()));

        Con(db => DatosPresentacion.QuitarAsync(db).GetAwaiter().GetResult());

        var despues = Con(db => (db.Residentes.Count(), db.Usuarios.Count(), db.Empleados.Count(), db.Medicamentos.Count(), db.Alerta.Count()));
        Assert.Equal(antes, despues);
        Assert.False(Con(db => DatosPresentacion.CargadosAsync(db).GetAwaiter().GetResult()));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/DatosPresentacion")).StatusCode); // la opción vuelve
    }
}
