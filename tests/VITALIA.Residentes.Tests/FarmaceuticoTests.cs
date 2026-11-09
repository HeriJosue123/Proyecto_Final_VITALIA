using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>El farmacéutico entra a su panel y a Farmacia, pero no puede borrar medicamentos ni modificar la bitácora.</summary>
public class FarmaceuticoTests : IClassFixture<FabricaResidentes>
{
    private readonly HttpClient _client;

    public FarmaceuticoTests(FabricaResidentes factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add("X-Usuario-Prueba", "999");
        peticion.Headers.Add("X-Rol-Prueba", RolesSistema.Farmaceutico);
        return _client.SendAsync(peticion);
    }

    [Theory]
    [InlineData("/Farmaceutico")]
    [InlineData("/Farmacia")]
    [InlineData("/Bitacora")]
    [InlineData("/Alertas")]
    public async Task Farmaceutico_EntraASusPantallas(string url)
    {
        var resp = await Enviar(HttpMethod.Get, url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/Farmacia/Delete/1")]
    [InlineData("POST", "/Farmacia/Delete/1")]
    [InlineData("POST", "/Bitacora/Eliminar")]
    [InlineData("POST", "/Bitacora/Registrar")]
    [InlineData("POST", "/Bitacora/Editar")]
    [InlineData("POST", "/Bitacora/CrearTipoAtencion")]
    [InlineData("GET", "/Residentes")]
    public async Task Farmaceutico_NoPuedeHacerLoQueNoLeToca(string metodo, string url)
    {
        var resp = await Enviar(new HttpMethod(metodo), url);
        // En la aplicación real lo manda a "Acceso denegado"; con la sesión de pruebas llega como 403
        Assert.True(resp.StatusCode == HttpStatusCode.Forbidden
                    || (resp.StatusCode == HttpStatusCode.Redirect && resp.Headers.Location?.ToString().Contains("AccesoDenegado") == true),
            $"Se esperaba acceso denegado y respondió {(int)resp.StatusCode}");
    }
}
