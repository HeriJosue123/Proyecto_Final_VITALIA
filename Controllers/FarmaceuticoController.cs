using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;

namespace VITALIA.Controllers
{
    // Solo permitimos el acceso a usuarios con el rol de farmacéutico (con o sin tilde)
    [Authorize(Roles = "Farmacéutico, Farmaceutico")]
    public class FarmaceuticoController : Controller
    {
        private readonly VitaliaDbContext _context;

        public FarmaceuticoController(VitaliaDbContext context)
        {
            _context = context;
        }

        // GET: Farmaceutico/Index
        public IActionResult Index()
        {
            return View();
        }
    }
}