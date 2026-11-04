using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;

namespace VITALIA.Areas.RRHH.Controllers
{
    [Area("RRHH")]
    [Authorize(Roles = "Administrador")]
    public class DashboardController : Controller
    {
        private readonly VitaliaDbContext _context;

        public DashboardController(VitaliaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;
            var limiteVencimiento = hoy.AddDays(30);

            // KPIs Operativos
            ViewBag.TotalEmpleados = await _context.Empleados.CountAsync(e => e.Estado);
            ViewBag.PermisosPendientes = await _context.SolicitudesPermiso.CountAsync(p => p.Estado == "Pendiente");
            ViewBag.ContratosActivos = await _context.ContratosEmpleado.CountAsync(c => c.Activo);

            // KPIs Financieros (Montos acumulados en estado Pendiente)
            ViewBag.MontoNominaPendiente = await _context.Nominas
                .Where(n => n.Estado == "Pendiente")
                .SumAsync(n => (decimal?)n.SalarioNeto) ?? 0;

            ViewBag.MontoServiciosPendientes = await _context.PersonalExterno
                .Where(p => p.Activo && p.EstadoPago == "Pendiente")
                .SumAsync(p => p.MontoServicio) ?? 0;

            // Alerta: Contratos temporales por vencer en los próximos 30 días
            ViewBag.ContratosPorVencer = await _context.ContratosEmpleado
                .Include(c => c.IdEmpleadoNavigation)
                .Where(c => c.Activo && c.FechaFin.HasValue && c.FechaFin.Value >= hoy && c.FechaFin.Value <= limiteVencimiento)
                .OrderBy(c => c.FechaFin)
                .ToListAsync();

            return View();
        }
    }
}