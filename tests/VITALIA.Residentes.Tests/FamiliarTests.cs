using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>Vista del familiar: solo ve a su residente y en modo lectura. Datos ficticios.</summary>
[Collection("Login")] // usa la misma base en memoria que las pruebas del login
public class FamiliarTests : IClassFixture<FabricaLogin>
{
    private readonly FabricaLogin _factory;

    public FamiliarTests(FabricaLogin factory)
    {
        _factory = factory;
        _factory.Sembrar();
    }

    private record Datos(string CorreoFamiliar, string CorreoSinVinculo, int Propio, string NombrePropio, int Ajeno, string NombreAjeno);

    // Un familiar con un residente vinculado (con bitácora, alertas y personal), otro residente ajeno
    // y un familiar sin residentes
    private Datos CrearDatos()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        string s = Guid.NewGuid().ToString("N")[..6];
        string hash = BCrypt.Net.BCrypt.HashPassword(FabricaLogin.Contrasena);
        int Rol(string nombre) => db.Roles.First(r => r.NombreRol == nombre).IdRol;

        var familiar = new Usuario { IdRol = Rol(RolesSistema.Familiar), Correo = "fam" + s + "@prueba.com", ContrasenaHash = hash, Estado = true };
        var sinVinculo = new Usuario { IdRol = Rol(RolesSistema.Familiar), Correo = "solo" + s + "@prueba.com", ContrasenaHash = hash, Estado = true };
        var usuarioEmpleado = new Usuario { IdRol = Rol(RolesSistema.Enfermero), Correo = "emp" + s + "@prueba.com", ContrasenaHash = hash, Estado = true };
        db.AddRange(familiar, sinVinculo, usuarioEmpleado);
        db.SaveChanges();

        Residente NuevoResidente(string nombre) => new()
        {
            CodigoUnico = "F-" + nombre[..1] + s, Nombre = nombre + s, Apellido = "Ficticia", FechaNacimiento = new DateOnly(1944, 3, 14),
            NumHabitacion = "101", TipoSangre = "O+", Alergias = "Penicilina", ContactoEmergencia = "Carlos · 7000-1111",
            Observaciones = "Hipertensión controlada.", Estado = true
        };
        var propio = NuevoResidente("Marta");
        var ajeno = NuevoResidente("Rosa");
        var empleado = new Empleado { IdUsuario = usuarioEmpleado.IdUsuario, Nombre = "Karla" + s, Apellido = "Prueba", Cargo = "Cuidadora", Estado = true };
        var tipo = new TipoAtencion { NombreCategoria = "Medicación " + s };
        db.AddRange(propio, ajeno, empleado, tipo);
        db.SaveChanges();

        db.Add(new FamiliarResidente { IdResidente = propio.IdResidente, IdUsuario = familiar.IdUsuario, Parentesco = "Hijo", TelefonoContacto = "7000-1111" });
        db.Add(new AsignacionCuidado { IdEmpleado = empleado.IdEmpleado, IdResidente = propio.IdResidente, FechaInicio = DateTime.Now.AddDays(-5), Estado = true });

        var normal = new AtencionDiaria { IdResidente = propio.IdResidente, IdEmpleado = empleado.IdEmpleado, IdTipoAtencion = tipo.IdTipoAtencion, FechaHora = DateTime.Now.AddHours(-1), Observaciones = "Losartan con el desayuno", EstadoCumplimiento = "Completado" };
        var anulada = new AtencionDiaria { IdResidente = propio.IdResidente, IdEmpleado = empleado.IdEmpleado, IdTipoAtencion = tipo.IdTipoAtencion, FechaHora = DateTime.Now.AddDays(-1), Observaciones = "Registro duplicado anulado", EstadoCumplimiento = "Completado" };
        var corregida = new AtencionDiaria { IdResidente = propio.IdResidente, IdEmpleado = empleado.IdEmpleado, IdTipoAtencion = tipo.IdTipoAtencion, FechaHora = DateTime.Now.AddDays(-2), Observaciones = "Dosis registrada", EstadoCumplimiento = "Completado" };
        var deOtro = new AtencionDiaria { IdResidente = ajeno.IdResidente, IdEmpleado = empleado.IdEmpleado, IdTipoAtencion = tipo.IdTipoAtencion, FechaHora = DateTime.Now, Observaciones = "Atencion de otro residente", EstadoCumplimiento = "Completado" };
        db.AddRange(normal, anulada, corregida, deOtro);
        db.SaveChanges();

        db.Add(new CorreccionAtencion { IdAtencion = anulada.IdAtencion, IdUsuarioAdmin = usuarioEmpleado.IdUsuario, Motivo = "Duplicado", TipoAccion = "Anulación", FechaHora = DateTime.Now });
        db.Add(new CorreccionAtencion { IdAtencion = corregida.IdAtencion, IdUsuarioAdmin = usuarioEmpleado.IdUsuario, Motivo = "La dosis correcta era 25 mg", TipoAccion = "Corrección", FechaHora = DateTime.Now });
        db.Add(new Alerta { IdResidente = propio.IdResidente, IdUsuarioCreador = usuarioEmpleado.IdUsuario, Titulo = "Glucosa alta", Descripcion = "Revisar dieta", FechaProgramada = DateTime.Now.AddHours(2), EstadoActual = AlertaEstados.Pendiente, Prioridad = "Urgente" });
        var medicamento = new Medicamento { Codigo = "MED-" + s, Nombre = "Losartan" + s, Presentacion = "Tableta 50 mg", Estado = true };
        db.Add(medicamento);
        db.SaveChanges();
        var lote = new LoteMedicamento { IdMedicamento = medicamento.IdMedicamento, NumeroLote = "L-" + s, FechaVencimiento = DateTime.Today.AddYears(1), Stock = 100 };
        db.Add(lote);
        db.SaveChanges();
        db.Add(new EntregaMedicamento { IdLote = lote.IdLote, IdResidente = propio.IdResidente, IdEmpleado = empleado.IdEmpleado, Cantidad = 2, FechaHora = DateTime.Now });
        db.Add(new Alerta { IdResidente = ajeno.IdResidente, IdUsuarioCreador = usuarioEmpleado.IdUsuario, Titulo = "Alerta de otro residente", Descripcion = "-", FechaProgramada = DateTime.Now, EstadoActual = AlertaEstados.Pendiente, Prioridad = "Rutina" });
        db.SaveChanges();

        return new Datos(familiar.Correo, sinVinculo.Correo, propio.IdResidente, propio.Nombre + " Ficticia", ajeno.IdResidente, ajeno.Nombre + " Ficticia");
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

    [Fact]
    public async Task Inicio_MuestraSoloASuResidente()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.CorreoFamiliar);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Familiar"));

        Assert.Contains(d.NombrePropio, html);
        Assert.DoesNotContain(d.NombreAjeno, html);
        Assert.Contains("1 alerta(s) activa(s)", html);
    }

    [Fact]
    public async Task Ficha_MuestraBitacoraAlertasYPersonal_SinLasAnuladas()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.CorreoFamiliar);

        var resp = await cliente.GetAsync("/Familiar/Residente/" + d.Propio);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());

        Assert.Contains("Losartan con el desayuno", html);
        Assert.Contains("Registro corregido: La dosis correcta era 25 mg", html);
        Assert.DoesNotContain("Registro duplicado anulado", html);
        Assert.DoesNotContain("Atencion de otro residente", html);
        Assert.Contains("Glucosa alta", html);
        Assert.Contains("Urgente", html);
        Assert.DoesNotContain("Alerta de otro residente", html);
        Assert.Contains("Penicilina", html);
        Assert.Contains("solo lectura", html);
        Assert.Contains("Medicamentos que recibe", html);
        Assert.Contains("Tableta 50 mg", html);
    }

    [Fact]
    public async Task Ficha_DeOtroResidente_DaAccesoDenegado()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.CorreoFamiliar);

        var resp = await cliente.GetAsync("/Familiar/Residente/" + d.Ajeno);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task ExportarPdf_DeSuResidente_DescargaUnPdf()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.CorreoFamiliar);

        var resp = await cliente.GetAsync("/Familiar/ExportarPdf/" + d.Propio);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.StartsWith("Historial_F-M", resp.Content.Headers.ContentDisposition?.FileNameStar ?? resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
    }

    [Fact]
    public async Task ExportarPdf_DeOtroResidente_DaAccesoDenegado()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.CorreoFamiliar);

        var resp = await cliente.GetAsync("/Familiar/ExportarPdf/" + d.Ajeno);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task FamiliarSinResidente_VeAviso()
    {
        var d = CrearDatos();
        var cliente = await EntrarAsync(d.CorreoSinVinculo);

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Familiar"));

        Assert.Contains("Todavía no tiene un residente vinculado", html);
    }

    [Fact]
    public async Task EntrandoDesdeLaDireccionPrincipal_LlegaAMiFamiliar()
    {
        var d = CrearDatos();
        var cliente = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        // localhost/ manda al login con ReturnUrl=/ (el Inicio del personal)
        var html = await (await cliente.GetAsync("/Cuenta/Login?ReturnUrl=%2F")).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var login = await cliente.PostAsync("/Cuenta/Login?returnUrl=%2F", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Correo"] = d.CorreoFamiliar, ["Contrasena"] = FabricaLogin.Contrasena, ["__RequestVerificationToken"] = token
        }));
        Assert.Equal("/Familiar", login.Headers.Location!.OriginalString);

        // Ya con sesión, si abre localhost/ también termina en su vista
        var denegado = await cliente.GetAsync("/");
        var resp = await cliente.GetAsync(denegado.Headers.Location!.PathAndQuery);
        Assert.Equal("/Familiar", resp.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Personal_NoPuedeEntrarALaVistaDelFamiliar()
    {
        var cliente = await EntrarAsync("enfermero@prueba.com");

        var resp = await cliente.GetAsync("/Familiar");

        Assert.StartsWith("/Cuenta/AccesoDenegado", resp.Headers.Location!.PathAndQuery);
    }
}
