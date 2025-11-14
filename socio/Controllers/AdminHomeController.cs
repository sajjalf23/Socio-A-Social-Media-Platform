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
            ViewData["CommentsLast7Days"] = new List<int> { 800, 820, 830, 850, 870, 900, 920 };
            ViewData["LikesLast7Days"] = new List<int> { 200, 1250, 1900, 1350, 1400, 1550, 1500 };

            return View();
        }

        public IActionResult AllUsers()
        {
            return View();
        }
    }
}
