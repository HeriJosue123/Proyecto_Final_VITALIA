using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar soporte para controladores y vistas (MVC)
//    Todas las páginas piden haber iniciado sesión, salvo las marcadas con [AllowAnonymous]
builder.Services.AddControllersWithViews(opciones => opciones.Filters.Add(new AuthorizeFilter()));

// Inicio de sesión con cookie (usuarios de la tabla USUARIO)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.LogoutPath = "/Cuenta/Logout";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
    });

// 2. Conexión a SQL Server (Persistencia Real)
builder.Services.AddDbContext<VitaliaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var builderApp = builder.Build();

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
        // Si la base no está disponible, el sistema arranca igual y lo avisa en la consola
        builderApp.Logger.LogWarning("No se pudieron crear los roles y usuarios por defecto: {Mensaje}", ex.Message);
    }
}

// 3. Configuración del pipeline de solicitudes HTTP
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

builderApp.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

builderApp.Run();