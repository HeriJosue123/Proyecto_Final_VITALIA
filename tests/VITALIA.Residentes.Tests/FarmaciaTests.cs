using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>Eliminar medicamentos de Farmacia con lotes y entregas. Datos ficticios.</summary>
public class FarmaciaTests : IClassFixture<FabricaResidentes>
{
    private readonly FabricaResidentes _factory;
    private readonly HttpClient _client;

    public FarmaciaTests(FabricaResidentes factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // Crea un medicamento con un lote; si conEntrega, también una entrega a un residente
    private int CrearMedicamento(bool conEntrega)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string s = Guid.NewGuid().ToString("N")[..6];

        var medicamento = new Medicamento { Codigo = "T-" + s, Nombre = "Ibuprofeno" + s, Presentacion = "Tableta 400 mg", Estado = true };
        db.Add(medicamento);
        db.SaveChanges();
        var lote = new LoteMedicamento { IdMedicamento = medicamento.IdMedicamento, NumeroLote = "L-" + s, FechaVencimiento = DateTime.Today.AddYears(1), Stock = 30 };
        db.Add(lote);
        db.SaveChanges();

        if (conEntrega)
        {
            var usuario = new Usuario { IdRol = db.Roles.First(r => r.NombreRol == RolesSistema.Enfermero).IdRol, Correo = "f" + s + "@prueba.com", ContrasenaHash = "x", Estado = true };
            db.Add(usuario);
            db.SaveChanges();
            var empleado = new Empleado { IdUsuario = usuario.IdUsuario, Nombre = "Karla", Apellido = "Prueba", Cargo = "Cuidadora", Estado = true };
            var residente = new Residente
            {
                CodigoUnico = "FA-" + s, Nombre = "Marta", Apellido = "Ficticia", FechaNacimiento = new DateOnly(1945, 1, 1),
                NumHabitacion = "101", ContactoEmergencia = "Contacto · 7000-0000", Estado = true
            };
            db.AddRange(empleado, residente);
            db.SaveChanges();
            db.Add(new EntregaMedicamento { IdLote = lote.IdLote, IdResidente = residente.IdResidente, IdEmpleado = empleado.IdEmpleado, Cantidad = 1, FechaHora = DateTime.Now });
            db.SaveChanges();
        }
        return medicamento.IdMedicamento;
    }

    private async Task<HttpResponseMessage> EliminarAsync(int id)
    {
        var html = await (await _client.GetAsync("/Farmacia/Delete/" + id)).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        return await _client.PostAsync("/Farmacia/Delete/" + id, new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
    }

    private (bool medicamento, int lotes) Existe(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        return (db.Medicamentos.Any(m => m.IdMedicamento == id), db.LoteMedicamentos.Count(l => l.IdMedicamento == id));
    }

    [Fact]
    public async Task MedicamentoConLotesSinEntregas_SeEliminaConSusLotes()
    {
        int id = CrearMedicamento(conEntrega: false);

        var pantalla = WebUtility.HtmlDecode(await _client.GetStringAsync("/Farmacia/Delete/" + id));
        Assert.Contains("También se eliminarán sus <strong>1 lote(s)</strong> de inventario", pantalla);

        var resp = await EliminarAsync(id);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal((false, 0), Existe(id));
    }

    [Fact]
    public async Task MedicamentoConEntregas_NoSeElimina()
    {
        int id = CrearMedicamento(conEntrega: true);

        var resp = await EliminarAsync(id);
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal((true, 1), Existe(id));

        var lista = WebUtility.HtmlDecode(await _client.GetStringAsync(resp.Headers.Location!.OriginalString));
        Assert.Contains("porque ya tiene 1 entrega(s) a residentes", lista);
    }
}
