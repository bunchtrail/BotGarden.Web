using Microsoft.AspNetCore.Mvc;

namespace BotGarden.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {

            if (IsValidUser(username, password))
            {
                return this.RedirectToAction("Index", "Home");
            }
            else
            {

                ViewBag.Error = "Неверное имя пользователя или пароль";
                return View("Index");
            }
        }

        private static bool IsValidUser(string username, string password)
        {

            return username == "admin" && password == "admin";
        }
    }
}