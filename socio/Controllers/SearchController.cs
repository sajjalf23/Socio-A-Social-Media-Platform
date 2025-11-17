using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SocioApp.Controllers
{
    public class SearchController : Controller
    {
        private List<dynamic> _allUsers = new List<dynamic>
        {
            new { Id = 1, Username = "john_doe", ProfileImage = "~/images/profile.png", PostCount = 12 },
            new { Id = 2, Username = "sara_khan", ProfileImage = "~/images/profile.png", PostCount = 8 },
            new { Id = 3, Username = "ali_ahmed", ProfileImage = "~/images/profile.png", PostCount = 5 },
            new { Id = 4, Username = "mina_yusuf", ProfileImage = "~/images/profile.png", PostCount = 7 },
            new { Id = 5, Username = "khalid_ali", ProfileImage = "~/images/profile.png", PostCount = 9 },
            new { Id = 6, Username = "nina_ahmad", ProfileImage = "~/images/profile.png", PostCount = 6 }
        };

        [HttpGet]
        public IActionResult Index(string query)
        {
            List<dynamic> users;

            if (string.IsNullOrWhiteSpace(query))
            {
                users = new List<dynamic>();
            }
            else
            {
                users = _allUsers
                    .Where(u => u.Username.ToLower().Contains(query.Trim().ToLower()))
                    .ToList();
            }

            ViewData["Users"] = users;
            ViewData["SearchQuery"] = query;

            return View(); 
        }
    }
}
