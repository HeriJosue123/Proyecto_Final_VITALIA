using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;

namespace VITALIA.Controllers
{
    [Authorize(Roles = RolesSistema.PersonalClinico)]
    public class HomeController : Controller
    {
        private readonly VitaliaDbContext _context;

        public HomeController(VitaliaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Métricas dinámicas para el Dashboard
            ViewBag.TotalResidentes = await _context.Residentes.CountAsync(r => r.Estado);
            ViewBag.TotalPersonal = await _context.Empleados.CountAsync(e => e.Estado);

            DateTime hoy = DateTime.Today;
            ViewBag.AtencionesHoy = await _context.AtencionDiaria.CountAsync(a => a.FechaHora >= hoy);

            // Conteo total de Alertas
            try
            {
                ViewBag.AlertasPendientes = await _context.Alerta.CountAsync();
            }
            catch
            {
                ViewBag.AlertasPendientes = 0;
            }

            return View();
        }
    }
}