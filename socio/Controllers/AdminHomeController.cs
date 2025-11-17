using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace socio.Controllers
{
    public class AdminHomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["UsersLast7Days"] = new List<int> { 100, 120, 130, 150, 170, 200, 220 };
            ViewData["PostsLast7Days"] = new List<int> { 400, 380, 420, 450, 470, 500, 520 };
            ViewData["CommentsLast7Days"] = new List<int> { 800, 7000, 830, 120, 870, 1000, 920 };
            ViewData["LikesLast7Days"] = new List<int> { 200, 1250, 1900, 1350, 1400, 1550, 1500 };

            return View();
        }

        public IActionResult AllUsers()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Post(int id = 1)
        {
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
            return View();
        }

        [HttpGet]
        public IActionResult Profile()
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
        public IActionResult Comments(string username)
        {
            var comments = new List<dynamic>
    {
        new { Id = 1, PostId = 101, PostTitle = "Sunday Vibes", Text = "Love this! 😍", TimeAgo = "2 days ago" },
        new { Id = 2, PostId = 102, PostTitle = "Morning Run", Text = "Great job!", TimeAgo = "5 days ago" },
        new { Id = 3, PostId = 103, PostTitle = "Coffee Time", Text = "Yummy!", TimeAgo = "1 week ago" }
    };

            ViewData["UserComments"] = comments;
            return View();
        }

        [HttpPost]
        public IActionResult DeleteComment(int id)
        {
            return RedirectToAction("AllUsers");
        }

        [HttpPost]
        public IActionResult DeletePost(int id)
        {
            return RedirectToAction("AllUsers");
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            return RedirectToAction("AllUsers");
        }

         private static List<dynamic> Users = new List<dynamic> {
            new { Id = 1, Username = "john_doe", ProfileImage = "~/images/profile.png", PostCount = 12 },
            new { Id = 2, Username = "sara_khan", ProfileImage = "~/images/profile.png", PostCount = 8 },
            new { Id = 3, Username = "ali_ahmed", ProfileImage = "~/images/profile.png", PostCount = 5 },
            new { Id = 4, Username = "mina_yusuf", ProfileImage = "~/images/profile.png", PostCount = 7 },
            new { Id = 5, Username = "khalid_ali", ProfileImage = "~/images/profile.png", PostCount = 9 },
            new { Id = 6, Username = "nina_ahmad", ProfileImage = "~/images/profile.png", PostCount = 6 }
        };

        // GET: Search Page
        [HttpGet]
        public IActionResult Search()
        {
            // Show all users initially
            ViewData["Users"] = Users;
            return View();
        }

        [HttpGet]
        public IActionResult SearchResults(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ViewData["Users"] = Users;
            }
            else
            {
                var filteredUsers = Users
                    .Where(u => u.Username.ToLower().Contains(query.ToLower()))
                    .ToList();
                ViewData["Users"] = filteredUsers;
            }

            ViewData["SearchQuery"] = query;
            return View("Search"); 
        }

    }
}
