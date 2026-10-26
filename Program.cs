using Microsoft.EntityFrameworkCore;
using VITALIA.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar soporte para controladores y vistas (MVC)
builder.Services.AddControllersWithViews();

// 2. Configuración de Entity Framework Core (SOLO EN MEMORIA)
builder.Services.AddDbContext<VitaliaDbContext>(options =>
    options.UseInMemoryDatabase("VitaliaTestDb"));

var builderApp = builder.Build();

// 3. Configuración del pipeline de solicitudes HTTP
if (!builderApp.Environment.IsDevelopment())
{
    builderApp.UseExceptionHandler("/Home/Error");
    builderApp.UseHsts();
}

builderApp.UseHttpsRedirection();
builderApp.UseStaticFiles();
builderApp.UseRouting();
builderApp.UseAuthorization();

builderApp.MapStaticAssets();

builderApp.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

builderApp.Run();