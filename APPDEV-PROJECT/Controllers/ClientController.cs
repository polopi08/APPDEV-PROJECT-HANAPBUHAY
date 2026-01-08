using Microsoft.AspNetCore.Mvc;

namespace APPDEV_PROJECT.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult SearchPage_C()
        {
            return View();
        }

        public IActionResult InfoPage_C()
        {
            return View();
        }

        public IActionResult ChatPage()
        {
            return View();
        }

        public IActionResult Messages()
        {
            return View("ChatPage");
        }

        public IActionResult Notifications()
        {
            return View("NotifPage");
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}