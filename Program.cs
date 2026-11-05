using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using VITALIA.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar soporte para controladores y vistas (MVC)
builder.Services.AddControllersWithViews(opciones => opciones.Filters.Add(new AuthorizeFilter()));

// Inicio de sesión con cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.LogoutPath = "/Cuenta/Logout";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
    });

// 2. Conexión a SQL Server
builder.Services.AddDbContext<VitaliaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var builderApp = builder.Build();

// ==========================================
// 3. CONFIGURACIÓN DE ROTATIVA
// ==========================================
RotativaConfiguration.Setup(builderApp.Environment.WebRootPath, "Rotativa");

// Roles y usuarios por defecto (solo se crean si no existen)
using (var scope = builderApp.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<VitaliaDbContext>();
        await DatosIniciales.CrearSiFaltanAsync(db);
    }
    catch (Exception ex)
    {
        builderApp.Logger.LogWarning("No se pudieron crear los roles y usuarios por defecto: {Mensaje}", ex.Message);
    }
}

// 4. Configuración del pipeline de solicitudes HTTP
if (!builderApp.Environment.IsDevelopment())
{
    builderApp.UseExceptionHandler("/Home/Error");
    builderApp.UseHsts();
}

builderApp.UseHttpsRedirection();
builderApp.UseStaticFiles();
builderApp.UseRouting();
builderApp.UseAuthentication();
builderApp.UseAuthorization();

builderApp.MapStaticAssets();

// Mapeo de rutas para el Área de RRHH
builderApp.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

builderApp.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

builderApp.Run();