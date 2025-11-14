using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace SocioApp.Controllers
{
    public class ProfileController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var profile = new
            {
                Username = "john_doe",
                Bio = "Professional procrastinator. Do not disturb.\nPowered by caffeine and bad decisions ☕️\nI put the 'Pro' in 'Procrastinate.'",
                PostsCount = 8,
                ProfileImage = "~/images/profile.png",
                Posts = new List<string>
                {
                    "~/images/feedimg.png",
                    "~/images/feedimg2.png",
                    "~/images/profile.png",
                    "~/images/feedimg2.png",
                    "~/images/feedimg.png",
                    "~/images/feedimg2.png",
                    "~/images/feedimg.png",
                    "~/images/profile.png"
                }
            };

            ViewData["Profile"] = profile;
            return View();
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var profile = new
            {
                FullName = "John Doe",
                Username = "john_doe",
                Bio = "Professional procrastinator ☕️ | Powered by caffeine and chaos",
                Location = "Los Angeles, CA",
                Website = "https://johndoe.me",
                ProfileImageUrl = "~/images/profile.png"
            };

            ViewData["Profile"] = profile;
            return View();
        }

        [HttpPost]
        public IActionResult Edit(string fullname, string username, string bio, string location, string website)
        {
            TempData["Message"] = " Profile updated successfully!";
            return RedirectToAction("Index");
        }
    }
}
