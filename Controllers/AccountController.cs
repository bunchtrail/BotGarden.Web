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
            // Здесь должна быть логика проверки учетных данных пользователя.
            // Если учетные данные верны:
            if (IsValidUser(username, password))
            {
                return this.RedirectToAction("Index", "Home");
            }
            else
            {
                // Если учетные данные неверны, верните пользователя обратно на страницу входа с сообщением об ошибке.
                ViewBag.Error = "Неверное имя пользователя или пароль";
                return View("Index");
            }
        }

        private static bool IsValidUser(string username, string password)
        {
            // Замените эту функцию на свою собственную логику проверки учетных данных.
            // Это просто пример.
            return username == "admin" && password == "admin";
        }
    }
}