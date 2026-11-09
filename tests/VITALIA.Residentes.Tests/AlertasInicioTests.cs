using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>Inicio (contadores) y alertas (a nombre de quién queda cada cambio). Datos ficticios.</summary>
public class AlertasInicioTests : IClassFixture<FabricaResidentes>
{
    private readonly FabricaResidentes _factory;
    private readonly HttpClient _client;

    public AlertasInicioTests(FabricaResidentes factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private T Db<T>(Func<VitaliaDbContext, T> accion)
    {
        using var scope = _factory.Services.CreateScope();
        return accion(scope.ServiceProvider.GetRequiredService<VitaliaDbContext>());
    }

    private int CrearAlerta(string estado, int idCreador)
    {
        return Db(db =>
        {
            string s = Guid.NewGuid().ToString("N")[..6];
            var residente = new Residente { CodigoUnico = "AL-" + s, Nombre = "Rosa", Apellido = "Prueba" + s, FechaNacimiento = new DateOnly(1945, 1, 1), NumHabitacion = "101", ContactoEmergencia = "Contacto · 7000-0000", Estado = true };
            db.Add(residente);
            db.SaveChanges();
            var alerta = new Alerta
            {
                IdResidente = residente.IdResidente, IdUsuarioCreador = idCreador, Titulo = "Alerta " + s, Descripcion = "Prueba",
                FechaProgramada = DateTime.Now.AddHours(1), EstadoActual = estado, Prioridad = "Media"
            };
            db.Add(alerta);
            db.SaveChanges();
            return alerta.IdAlerta;
        });
    }

    private static int Numero(string html, string etiqueta)
    {
        var m = Regex.Match(html, etiqueta + @"[\s\S]{0,400}?<h3[^>]*>\s*(\d+)\s*</h3>", RegexOptions.IgnoreCase);
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }

    [Fact]
    public async Task Inicio_NoCuentaLasAlertasAtendidasComoActivas()
    {
        int antes = Numero(await (await _client.GetAsync("/")).Content.ReadAsStringAsync(), "ALERTAS ACTIVAS");
        CrearAlerta(AlertaEstados.Pendiente, 1);
        CrearAlerta(AlertaEstados.EnProceso, 1);
        CrearAlerta(AlertaEstados.Atendida, 1);
        CrearAlerta(AlertaEstados.Atendida, 1);

        int despues = Numero(await (await _client.GetAsync("/")).Content.ReadAsStringAsync(), "ALERTAS ACTIVAS");

        Assert.True(antes >= 0);
        Assert.Equal(antes + 2, despues);
    }

    [Fact]
    public async Task Inicio_MuestraLosMedicamentosDelCatalogo()
    {
        int antes = Numero(await (await _client.GetAsync("/")).Content.ReadAsStringAsync(), "MEDICAMENTOS EN CATÁLOGO");
        Db(db =>
        {
            db.Add(new Medicamento { Nombre = "Medicamento " + Guid.NewGuid().ToString("N")[..6], Codigo = "MED-" + Guid.NewGuid().ToString("N")[..6], Presentacion = "Tableta", Estado = true });
            db.SaveChanges();
            return 0;
        });

        int despues = Numero(await (await _client.GetAsync("/")).Content.ReadAsStringAsync(), "MEDICAMENTOS EN CATÁLOGO");

        Assert.True(antes >= 0);
        Assert.Equal(antes + 1, despues);
    }

    [Fact]
    public async Task CambiarEstado_QuedaAGuardadoALaPersonaQueLoHizo()
    {
        // El médico (id distinto del administrador) pasa la alerta de Pendiente a En Proceso
        int idMedico = Db(db =>
        {
            var rol = db.Roles.First(r => r.NombreRol == RolesSistema.Medico);
            var u = new Usuario { IdRol = rol.IdRol, Correo = "med" + Guid.NewGuid().ToString("N")[..6] + "@prueba.com", ContrasenaHash = "x", Estado = true };
            db.Add(u);
            db.SaveChanges();
            return u.IdUsuario;
        });
        int idAlerta = CrearAlerta(AlertaEstados.Pendiente, 1);
        // El token antifalsificación va ligado al usuario, así que se pide ya como el médico
        var pedido = new HttpRequestMessage(HttpMethod.Get, "/Alertas/Create");
        pedido.Headers.Add("X-Usuario-Prueba", idMedico.ToString());
        pedido.Headers.Add("X-Rol-Prueba", RolesSistema.Medico);
        var html = await (await _client.SendAsync(pedido)).Content.ReadAsStringAsync();
        string token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;

        var peticion = new HttpRequestMessage(HttpMethod.Post, "/Alertas/CambiarEstado")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["id"] = idAlerta.ToString(), ["estadoEsperado"] = AlertaEstados.Pendiente, ["__RequestVerificationToken"] = token
            })
        };
        peticion.Headers.Add("X-Usuario-Prueba", idMedico.ToString());
        peticion.Headers.Add("X-Rol-Prueba", RolesSistema.Medico);
        var resp = await _client.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var historial = Db(db => db.HistorialEstadoAlerta.AsNoTracking().Single(h => h.IdAlerta == idAlerta));
        Assert.Equal(idMedico, historial.IdUsuario);
    }
}
