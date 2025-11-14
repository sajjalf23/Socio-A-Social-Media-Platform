using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace SocioApp.Controllers
{
    public class PostController : Controller
    {
        [HttpGet]
        public IActionResult Index(int id = 1) // default post id
        {
            // Single post data
            var post = new 
            {
                Id = id,
                Username = "john_doe",
                ProfileImage = "~/images/profile.png",
                PostImage = "~/images/feedimg2.png",
                TimeAgo = "2 days ago",
                Likes = 125,
                Content = "Sunday vibes ☀️ Coffee, music, and a lazy afternoon.",
                Comments = new List<dynamic> {
                    new { Username="amy_21", Text="Love this! 😍" },
                    new { Username="mike_dev", Text="Perfect chill day setup!" },
                    new { Username="alex", Text="Great photo 📸" },
                    new { Username="sarah", Text="I need this kind of Sunday!" }
                }
            };

            ViewData["Post"] = post;
            ViewData["currentUser"] = "john_doe"; // Simulated current user
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(string caption, string location, string tags)
        {
            TempData["Message"] = "✅ Post created successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var post = new 
            {
                Id = id,
                Caption = "Sunny beach day 🌴☀️",
                Location = "Malibu, California",
                ImageUrl = "~/images/feedimg2.png",
                Tags = new List<string> { "beach", "sunny" }
            };

            ViewData["Post"] = post;
            return View();
        }

        [HttpPost]
        public IActionResult Edit(int id, string caption, string location, string tags)
        {
            TempData["Message"] = "✅ Post updated successfully!";
            return RedirectToAction("Index", new { id });
        }
    }
}
