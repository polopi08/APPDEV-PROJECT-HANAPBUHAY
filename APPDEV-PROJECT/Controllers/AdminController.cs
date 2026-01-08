using Microsoft.AspNetCore.Mvc;

namespace APPDEV_PROJECT.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult DashboardPage_A()
        {
            return View();
        }

        public IActionResult AppMan_A()
        {
            return View();
        }

        public IActionResult Report_A()
        {
            return View();
        }

        public IActionResult ViewDetails_A()
        {
            return View();
        }
    }
}