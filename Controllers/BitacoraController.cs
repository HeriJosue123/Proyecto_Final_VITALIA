using Microsoft.AspNetCore.Mvc;

namespace VITALIA.Controllers
{
    public class BitacoraController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}