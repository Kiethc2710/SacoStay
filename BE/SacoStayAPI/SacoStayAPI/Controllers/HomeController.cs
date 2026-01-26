using Microsoft.AspNetCore.Mvc;

namespace SacoStayAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
