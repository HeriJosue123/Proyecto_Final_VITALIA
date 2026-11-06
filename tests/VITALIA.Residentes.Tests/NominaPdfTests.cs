using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.Rrhh;

namespace VITALIA.Residentes.Tests;

/// <summary>Recibo de nómina en PDF (RRHH): el nombre del archivo no puede llevar tildes ni ñ. Datos ficticios.</summary>
public class NominaPdfTests : IClassFixture<FabricaResidentes>
{
    private readonly FabricaResidentes _factory;
    private readonly HttpClient _client;

    public NominaPdfTests(FabricaResidentes factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // Crea un empleado con el apellido indicado y una nómina suya; devuelve el id de la nómina
    private int CrearNomina(string apellido)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string s = Guid.NewGuid().ToString("N")[..6];

        var rol = db.Roles.First(r => r.NombreRol == RolesSistema.Enfermero);
        var usuario = new Usuario { IdRol = rol.IdRol, Correo = "n" + s + "@prueba.com", ContrasenaHash = "x", Estado = true };
        db.Add(usuario);
        db.SaveChanges();
        var empleado = new Empleado { IdUsuario = usuario.IdUsuario, Nombre = "Karla", Apellido = apellido, Cargo = "Cuidadora", Estado = true };
        db.Add(empleado);
        db.SaveChanges();
        var nomina = new Nomina
        {
            IdEmpleado = empleado.IdEmpleado, Mes = 11, Anio = 2026, SalarioBruto = 520m, Deducciones = 80m,
            SalarioNeto = 440m, FechaPago = new DateTime(2026, 11, 30), Estado = "Pendiente"
        };
        db.Add(nomina);
        db.SaveChanges();
        return nomina.IdNomina;
    }

    private static string NombreDelArchivo(HttpResponseMessage resp) =>
        resp.Content.Headers.ContentDisposition?.FileNameStar
        ?? resp.Content.Headers.ContentDisposition?.FileName?.Trim('"')
        ?? "";

    [Theory]
    [InlineData("Mejía")]
    [InlineData("Peña Ñandú")]
    [InlineData("Henríquez Solís")]
    [InlineData("O'Brien-Pérez")]
    public async Task ReciboPdf_ConTildesEnElApellido_DescargaUnPdf(string apellido)
    {
        int id = CrearNomina(apellido);

        var resp = await _client.GetAsync("/RRHH/Nomina/DescargarReciboPdf/" + id);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task ReciboPdf_ElNombreDelArchivoQuedaSinTildesNiEspacios()
    {
        int id = CrearNomina("Peña Ñandú");

        var resp = await _client.GetAsync("/RRHH/Nomina/DescargarReciboPdf/" + id);

        var nombre = NombreDelArchivo(resp);
        Assert.Equal("Recibo_Nomina_Pena_Nandu_11_2026.pdf", nombre);
    }

    [Fact]
    public async Task ReciboPdf_DeUnaNominaQueNoExiste_DaNoEncontrado()
    {
        var resp = await _client.GetAsync("/RRHH/Nomina/DescargarReciboPdf/999999");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
