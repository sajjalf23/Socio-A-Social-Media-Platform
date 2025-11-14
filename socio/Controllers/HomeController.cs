using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace socio.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            
            var posts = new List<dynamic>
            {
                new {
                    Username = "john_doe",
                    ProfileImage = "~/images/profile.png",
                    PostImage = "~/images/feedimg2.png",
                    TimeAgo = "2 days ago",
                    Likes = "9000 likes",
                    Content = "Just chilling!",
                    Comments = new List<dynamic> {
                        new { Username = "David", Text = "Looking Awesome" }
                    }
                },
                new {
                    Username = "john_doe",
                    ProfileImage = "~/images/profile.png",
                    PostImage = "~/images/feedimg.png",
                    TimeAgo = "2 days ago",
                    Likes = "1,300 likes",
                    Content = "Mumdani Won NewYork Mayoral Election!",
                    Comments = new List<dynamic> {
                        new { Username = "maria", Text = "Yeh! defeated billionaires 🔥" }
                    }
                }
            };

            ViewData["Posts"] = posts;
            return View();
        }
    }
}
