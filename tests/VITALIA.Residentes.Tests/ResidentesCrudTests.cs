using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VITALIA.Controllers;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Residentes.Tests;

/// <summary>
/// Pruebas de integración del módulo Residentes. Levantan la aplicación completa en memoria
/// (WebApplicationFactory) con la base de datos InMemory configurada en Program.cs, por lo que
/// no tocan SQL Server ni VITALIADB. Todos los datos son ficticios.
/// </summary>
public class ResidentesCrudTests : IClassFixture<WebApplicationFactory<ResidentesController>>
{
    private readonly WebApplicationFactory<ResidentesController> _factory;
    private readonly HttpClient _client;

    public ResidentesCrudTests(WebApplicationFactory<ResidentesController> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    // ---------- utilidades ----------

    private static string NuevoCodigo() => "T-" + Guid.NewGuid().ToString("N")[..10].ToUpper();

    private static Dictionary<string, string> DatosValidos(string codigo) => new()
    {
        ["CodigoUnico"] = codigo,
        ["Nombre"] = "Prueba",
        ["Apellido"] = "Ficticia",
        ["FechaNacimiento"] = "1945-05-20",
        ["NumHabitacion"] = "101",
        ["TipoSangre"] = "O+",
        ["Alergias"] = "Penicilina",
        ["ContactoEmergencia"] = "Contacto Ficticio · 7000-0000",
        ["Observaciones"] = "Hipertensión controlada (dato ficticio)."
    };

    private async Task<string> GetHtmlAsync(string url)
    {
        var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> PostFormAsync(string urlFormulario, string urlPost, Dictionary<string, string> datos)
    {
        var html = await (await _client.GetAsync(urlFormulario)).Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "No se encontró el token antiforgery en " + urlFormulario);
        datos["__RequestVerificationToken"] = token;
        return await _client.PostAsync(urlPost, new FormUrlEncodedContent(datos));
    }

    private Task<HttpResponseMessage> RegistrarAsync(Dictionary<string, string> datos) =>
        PostFormAsync("/Residentes/Create", "/Residentes/Create", datos);

    private Residente? BuscarPorCodigo(string codigo)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        return db.Residentes.FirstOrDefault(r => r.CodigoUnico == codigo);
    }

    private int ContarPorCodigo(string codigo)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        return db.Residentes.Count(r => r.CodigoUnico.ToUpper() == codigo.ToUpper());
    }

    private Residente Sembrar(string codigo, string nombre, string apellido, string habitacion, bool activo = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        var r = new Residente
        {
            CodigoUnico = codigo, Nombre = nombre, Apellido = apellido,
            FechaNacimiento = new DateOnly(1940, 1, 15), NumHabitacion = habitacion,
            ContactoEmergencia = "Contacto Ficticio", Observaciones = "Dato ficticio", Estado = activo
        };
        db.Residentes.Add(r);
        db.SaveChanges();
        return r;
    }

    // ---------- CP-02 Registro ----------

    [Fact]
    public async Task CP02_RegistroValido_GuardaResidenteActivoYRedirige()
    {
        string codigo = NuevoCodigo();
        var resp = await RegistrarAsync(DatosValidos(codigo));

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var guardado = BuscarPorCodigo(codigo);
        Assert.NotNull(guardado);
        Assert.True(guardado!.Estado);
        Assert.Equal("Prueba", guardado.Nombre);
        Assert.Equal(new DateOnly(1945, 5, 20), guardado.FechaNacimiento);
    }

    [Fact]
    public async Task CP02b_RegistroNormalizaCodigoYEspacios()
    {
        string codigo = NuevoCodigo();
        var datos = DatosValidos("  " + codigo.ToLower() + "  ");
        datos["Nombre"] = "  Prueba  ";
        var resp = await RegistrarAsync(datos);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var guardado = BuscarPorCodigo(codigo);
        Assert.NotNull(guardado);
        Assert.Equal("Prueba", guardado!.Nombre);
    }

    // ---------- CP-03 Obligatorios ----------

    [Fact]
    public async Task CP03_CamposObligatoriosVacios_RechazaConMensajesEnEspanol()
    {
        var datos = new Dictionary<string, string>
        {
            ["CodigoUnico"] = "", ["Nombre"] = "", ["Apellido"] = "", ["FechaNacimiento"] = "",
            ["NumHabitacion"] = "", ["TipoSangre"] = "", ["Alergias"] = "", ["ContactoEmergencia"] = "", ["Observaciones"] = ""
        };
        var resp = await RegistrarAsync(datos);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
        Assert.Contains("El código único del residente es obligatorio.", html);
        Assert.Contains("El nombre es obligatorio.", html);
        Assert.Contains("El apellido es obligatorio.", html);
        Assert.Contains("La fecha de nacimiento es obligatoria.", html);
        Assert.Contains("La habitación es obligatoria.", html);
        Assert.Contains("El contacto de emergencia es obligatorio.", html);
        Assert.Contains("Los antecedentes del residente son obligatorios.", html);
        Assert.DoesNotContain("is invalid", html);
        Assert.DoesNotContain("field is required", html);
    }

    // ---------- CP-04 Código duplicado ----------

    [Fact]
    public async Task CP04_CodigoDuplicado_RechazaSegundoRegistro()
    {
        string codigo = NuevoCodigo();
        Assert.Equal(HttpStatusCode.Redirect, (await RegistrarAsync(DatosValidos(codigo))).StatusCode);

        var resp = await RegistrarAsync(DatosValidos(codigo.ToLower()));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
        Assert.Contains($"Ya existe un residente con el código {codigo}.", html);
        Assert.Equal(1, ContarPorCodigo(codigo));
    }

    [Fact]
    public async Task CP04b_CodigoDeResidenteInactivo_TampocoSePuedeReutilizar()
    {
        string codigo = NuevoCodigo();
        Sembrar(codigo, "Baja", "Previa", "300", activo: false);

        var resp = await RegistrarAsync(DatosValidos(codigo));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(1, ContarPorCodigo(codigo));
    }

    // ---------- CP-05 Longitudes y formato ----------

    [Fact]
    public async Task CP05_LongitudesNoPermitidas_Rechaza()
    {
        var datos = DatosValidos("T-" + new string('9', 25));
        datos["Nombre"] = new string('A', 101);
        datos["Apellido"] = new string('B', 101);
        datos["ContactoEmergencia"] = new string('C', 151);
        datos["Alergias"] = new string('D', 256);
        var resp = await RegistrarAsync(datos);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
        Assert.Contains("El código único no puede exceder los 20 caracteres.", html);
        Assert.Contains("El nombre no puede exceder los 100 caracteres.", html);
        Assert.Contains("El apellido no puede exceder los 100 caracteres.", html);
        Assert.Contains("El contacto de emergencia no puede exceder los 150 caracteres.", html);
        Assert.Contains("Las alergias no pueden exceder los 255 caracteres.", html);
    }

    [Fact]
    public async Task CP05b_CodigoConCaracteresNoPermitidos_Rechaza()
    {
        var resp = await RegistrarAsync(DatosValidos("RES 01/#"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync());
        Assert.Contains("El código único solo admite letras, números y guiones", html);
    }

    [Fact]
    public async Task CP05c_TipoDeSangreInvalido_Rechaza()
    {
        var datos = DatosValidos(NuevoCodigo());
        datos["TipoSangre"] = "Z+";
        var resp = await RegistrarAsync(datos);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Seleccione un tipo de sangre válido.", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
    }

    // ---------- CP-06 Fechas ----------

    [Theory]
    [InlineData("futura", "La fecha de nacimiento debe ser anterior a la fecha actual.")]
    [InlineData("hoy", "La fecha de nacimiento debe ser anterior a la fecha actual.")]
    [InlineData("1850-01-01", "La fecha de nacimiento no es válida (más de 130 años).")]
    [InlineData("31/02/1950", "La fecha de nacimiento no tiene un formato válido.")]
    public async Task CP06_FechasInvalidas_Rechaza(string fecha, string mensaje)
    {
        string valor = fecha switch
        {
            "futura" => DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"),
            "hoy" => DateTime.Today.ToString("yyyy-MM-dd"),
            _ => fecha
        };
        string codigo = NuevoCodigo();
        var datos = DatosValidos(codigo);
        datos["FechaNacimiento"] = valor;
        var resp = await RegistrarAsync(datos);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains(mensaje, WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
        Assert.Null(BuscarPorCodigo(codigo));
    }

    // ---------- CP-07 Listado y consulta ----------

    [Fact]
    public async Task CP07_ListadoYDetalle_MuestranDatosRealesSinDatosDeDemostracion()
    {
        string codigo = NuevoCodigo();
        var r = Sembrar(codigo, "Listado", "Consulta", "110");

        var listado = await GetHtmlAsync("/Residentes?busqueda=" + codigo);
        Assert.Contains(codigo, listado);
        Assert.Contains("Listado Consulta", listado);
        Assert.DoesNotContain("RES-2026-001", listado);

        var ficha = await GetHtmlAsync($"/Residentes/Details/{r.IdResidente}");
        Assert.Contains(codigo, ficha);
        Assert.Contains("Contacto Ficticio", ficha);
        Assert.Contains("Activo", ficha);
    }

    // ---------- CP-08 Búsqueda y filtros ----------

    [Fact]
    public async Task CP08_Busqueda_PorNombreApellidoYCodigo_SinDistinguirMayusculas()
    {
        string sufijo = Guid.NewGuid().ToString("N")[..6];
        string c1 = NuevoCodigo(), c2 = NuevoCodigo();
        Sembrar(c1, "Aurelia" + sufijo, "Paredes", "201");
        Sembrar(c2, "Teodoro", "Quintanilla" + sufijo, "202");

        var porNombre = await GetHtmlAsync("/Residentes?busqueda=aurelia" + sufijo);
        Assert.Contains(c1, porNombre);
        Assert.DoesNotContain(c2, porNombre);

        var porApellido = await GetHtmlAsync("/Residentes?busqueda=QUINTANILLA" + sufijo.ToUpper());
        Assert.Contains(c2, porApellido);
        Assert.DoesNotContain(c1, porApellido);

        var porCodigo = await GetHtmlAsync("/Residentes?busqueda=" + c1.ToLower());
        Assert.Contains(c1, porCodigo);

        var sinResultados = await GetHtmlAsync("/Residentes?busqueda=noexiste" + sufijo);
        Assert.Contains("No hay residentes que coincidan", sinResultados);
    }

    [Fact]
    public async Task CP08b_FiltroEstado_SeparaActivosEInactivos()
    {
        string activo = NuevoCodigo(), inactivo = NuevoCodigo();
        Sembrar(activo, "Filtro", "Activo", "203");
        Sembrar(inactivo, "Filtro", "Inactivo", "203", activo: false);

        var activos = await GetHtmlAsync("/Residentes?estado=activos");
        Assert.Contains(activo, activos);
        Assert.DoesNotContain(inactivo, activos);

        var inactivos = await GetHtmlAsync("/Residentes?estado=inactivos");
        Assert.Contains(inactivo, inactivos);
        Assert.DoesNotContain(activo, inactivos);

        var todos = await GetHtmlAsync("/Residentes?estado=todos");
        Assert.Contains(activo, todos);
        Assert.Contains(inactivo, todos);
    }

    [Fact]
    public async Task CP08c_FiltroHabitacion_MuestraSoloEsaHabitacion()
    {
        string hab = "H" + Guid.NewGuid().ToString("N")[..5];
        string enHab = NuevoCodigo(), otra = NuevoCodigo();
        Sembrar(enHab, "Filtro", "Habitacion", hab);
        Sembrar(otra, "Otra", "Habitacion", "999");

        var html = await GetHtmlAsync("/Residentes?habitacion=" + hab);
        Assert.Contains(enHab, html);
        Assert.DoesNotContain(otra, html);
        Assert.Contains($"<option value=\"{hab}\" selected", html);
    }

    // ---------- CP-09 Edición ----------

    [Fact]
    public async Task CP09_Edicion_ActualizaDatosYConservaEstado()
    {
        string codigo = NuevoCodigo();
        var r = Sembrar(codigo, "Antes", "Edicion", "120");

        var datos = DatosValidos(codigo);
        datos["IdResidente"] = r.IdResidente.ToString();
        datos["Nombre"] = "Despues";
        datos["NumHabitacion"] = "121";
        var resp = await PostFormAsync($"/Residentes/Edit/{r.IdResidente}", $"/Residentes/Edit/{r.IdResidente}", datos);

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var actualizado = BuscarPorCodigo(codigo)!;
        Assert.Equal("Despues", actualizado.Nombre);
        Assert.Equal("121", actualizado.NumHabitacion);
        Assert.True(actualizado.Estado);
    }

    [Fact]
    public async Task CP09b_EdicionConDatosInvalidos_NoGuarda()
    {
        string codigo = NuevoCodigo();
        var r = Sembrar(codigo, "Sin", "Cambios", "122");

        var datos = DatosValidos(codigo);
        datos["IdResidente"] = r.IdResidente.ToString();
        datos["Nombre"] = "";
        var resp = await PostFormAsync($"/Residentes/Edit/{r.IdResidente}", $"/Residentes/Edit/{r.IdResidente}", datos);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("El nombre es obligatorio.", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
        Assert.Equal("Sin", BuscarPorCodigo(codigo)!.Nombre);
    }

    [Fact]
    public async Task CP09c_EdicionConCodigoDeOtroResidente_Rechaza()
    {
        string c1 = NuevoCodigo(), c2 = NuevoCodigo();
        Sembrar(c1, "Primero", "Codigo", "123");
        var r2 = Sembrar(c2, "Segundo", "Codigo", "123");

        var datos = DatosValidos(c1);
        datos["IdResidente"] = r2.IdResidente.ToString();
        var resp = await PostFormAsync($"/Residentes/Edit/{r2.IdResidente}", $"/Residentes/Edit/{r2.IdResidente}", datos);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains($"Ya existe un residente con el código {c1}", WebUtility.HtmlDecode(await resp.Content.ReadAsStringAsync()));
        Assert.Equal(c2, BuscarPorCodigo(c2)!.CodigoUnico);
    }

    // ---------- CP-10 Identificadores inexistentes ----------

    [Theory]
    [InlineData("/Residentes/Details/999999")]
    [InlineData("/Residentes/Edit/999999")]
    [InlineData("/Residentes/Delete/999999")]
    [InlineData("/Residentes/Details")]
    [InlineData("/Residentes/Edit")]
    [InlineData("/Residentes/Delete")]
    public async Task CP10_IdentificadorInexistente_Devuelve404(string url)
    {
        var resp = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CP10b_PostsConIdentificadorInexistenteOIncoherente_Devuelven404()
    {
        var r = Sembrar(NuevoCodigo(), "Id", "Coherente", "124");

        var bajaInexistente = await PostFormAsync("/Residentes/Create", "/Residentes/Delete/999999", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.NotFound, bajaInexistente.StatusCode);

        var datos = DatosValidos(NuevoCodigo());
        datos["IdResidente"] = (r.IdResidente + 1000).ToString();
        var editarIncoherente = await PostFormAsync($"/Residentes/Edit/{r.IdResidente}", $"/Residentes/Edit/{r.IdResidente}", datos);
        Assert.Equal(HttpStatusCode.NotFound, editarIncoherente.StatusCode);
    }

    // ---------- CP-11 Baja lógica ----------

    [Fact]
    public async Task CP11_BajaLogica_MarcaInactivoSinEliminarRegistro()
    {
        string codigo = NuevoCodigo();
        var r = Sembrar(codigo, "Para", "Baja", "130");

        var resp = await PostFormAsync($"/Residentes/Delete/{r.IdResidente}", $"/Residentes/Delete/{r.IdResidente}", new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var tras = BuscarPorCodigo(codigo);
        Assert.NotNull(tras);              // no se eliminó físicamente
        Assert.False(tras!.Estado);        // quedó inactivo

        // La redirección muestra el mensaje de confirmación (TempData) con el código del residente
        var confirmacion = await GetHtmlAsync(resp.Headers.Location!.OriginalString);
        Assert.Contains("fue dado de baja", confirmacion);

        var activos = await GetHtmlAsync("/Residentes?estado=activos");
        Assert.DoesNotContain(codigo, activos);
        var ficha = await GetHtmlAsync($"/Residentes/Details/{r.IdResidente}");
        Assert.Contains("dado de baja", ficha);
    }

    // ---------- CP-12 Residentes inactivos ----------

    [Fact]
    public async Task CP12_ResidenteInactivo_NoSePuedeDarDeBajaDeNuevoYEdicionConservaEstado()
    {
        string codigo = NuevoCodigo();
        var r = Sembrar(codigo, "Ya", "Inactivo", "131", activo: false);

        var formulario = await GetHtmlAsync($"/Residentes/Delete/{r.IdResidente}");
        Assert.Contains("Este residente ya está dado de baja.", formulario);
        Assert.DoesNotContain("Confirmar baja", formulario);

        var resp = await PostFormAsync($"/Residentes/Delete/{r.IdResidente}", $"/Residentes/Delete/{r.IdResidente}", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.False(BuscarPorCodigo(codigo)!.Estado);

        var datos = DatosValidos(codigo);
        datos["IdResidente"] = r.IdResidente.ToString();
        datos["Observaciones"] = "Actualizado estando inactivo (ficticio).";
        var edicion = await PostFormAsync($"/Residentes/Edit/{r.IdResidente}", $"/Residentes/Edit/{r.IdResidente}", datos);
        Assert.Equal(HttpStatusCode.Redirect, edicion.StatusCode);
        var tras = BuscarPorCodigo(codigo)!;
        Assert.False(tras.Estado);
        Assert.Equal("Actualizado estando inactivo (ficticio).", tras.Observaciones);
    }
}
