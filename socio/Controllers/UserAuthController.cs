using Microsoft.AspNetCore.Mvc;

namespace SocioApp.Controllers
{
    public class UserAuthController : Controller
    {
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            string dummyUsername = "admin";
            string dummyPassword = "12345";

            if (username == dummyUsername && password == dummyPassword)
                return RedirectToAction("Index", "Home");

            ViewBag.Message = "Invalid username or password.";
            return View();
        }

        public IActionResult SignUp() => View();

        [HttpPost]
        public IActionResult SignUp(string fullName, string username, string email, string password)
        {
            ViewBag.Message = $"Welcome, {fullName}! Your account has been created (dummy).";
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            ViewBag.Message = $"If {email} is registered, a reset link has been sent.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            ViewBag.Message = "Password has been reset successfully.";
            return View();
        }


    }
}
