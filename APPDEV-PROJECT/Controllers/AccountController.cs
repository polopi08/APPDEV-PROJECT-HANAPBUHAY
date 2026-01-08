using Microsoft.AspNetCore.Mvc;

namespace APPDEV_PROJECT.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult LoginPage()
        {
            return View();
        }

        public IActionResult RegisterPage()
        {
            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}