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
    // Permitimos acceso al Administrador, Médico, Enfermero y Farmacéutico para que no bloquee al entrar
    [Authorize(Roles = "Administrador, Medico, Médico, Enfermero, Farmacéutico, Farmaceutico")]
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

            // Solo las alertas que siguen abiertas (Pendiente o En Proceso); las atendidas ya no requieren atención
            try
            {
                ViewBag.AlertasPendientes = await _context.Alerta.CountAsync(a => a.EstadoActual != AlertaEstados.Atendida);
            }
            catch
            {
                ViewBag.AlertasPendientes = 0;
            }

            // Medicamentos activos del catálogo (si la base aún no tiene las tablas de Farmacia, queda en 0)
            try
            {
                ViewBag.TotalMedicamentos = await _context.Medicamentos.CountAsync(m => m.Estado);
            }
            catch
            {
                ViewBag.TotalMedicamentos = 0;
            }

            return View();
        }
    }
}