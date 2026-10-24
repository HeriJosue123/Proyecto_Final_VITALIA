using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers;

public class HomeController : Controller
{
    private readonly VitaliaDbContext _context;

    public HomeController(VitaliaDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        // Validamos la conectividad a SQL Server para informar en la vista de bienvenida
        bool canConnect = false;
        try
        {
            canConnect = _context.Database.CanConnect();
        }
        catch
        {
            canConnect = false;
        }

        ViewBag.CanConnectDb = canConnect;
        return View();
    }

    public IActionResult ModuloPendiente(string nombre)
    {
        ViewBag.ModuloNombre = string.IsNullOrWhiteSpace(nombre) ? "Módulo" : nombre;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
