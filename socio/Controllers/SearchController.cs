using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SocioApp.Services;
using SocioApp.Models;



namespace SocioApp.Controllers
{

    [Authorize(policy:"UserPages")]
    public class SearchController : Controller
    {
         private readonly IProfileService _profileService;

        public SearchController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        // private List<dynamic> _allUsers = new List<dynamic>
        // {
        //     new { Id = 1, Username = "john_doe", ProfileImage = "~/images/profile.png", PostCount = 12 },
        //     new { Id = 2, Username = "sara_khan", ProfileImage = "~/images/profile.png", PostCount = 8 },
        //     new { Id = 3, Username = "ali_ahmed", ProfileImage = "~/images/profile.png", PostCount = 5 },
        //     new { Id = 4, Username = "mina_yusuf", ProfileImage = "~/images/profile.png", PostCount = 7 },
        //     new { Id = 5, Username = "khalid_ali", ProfileImage = "~/images/profile.png", PostCount = 9 },
        //     new { Id = 6, Username = "nina_ahmad", ProfileImage = "~/images/profile.png", PostCount = 6 }
        // };

        // [HttpGet]
        // public IActionResult Index(string query)
        // {
        //     List<dynamic> users;

        //     if (string.IsNullOrWhiteSpace(query))
        //     {
        //         users = new List<dynamic>();
        //     }
        //     else
        //     {
        //         users = _allUsers
        //             .Where(u => u.Username.ToLower().Contains(query.Trim().ToLower()))
        //             .ToList();
        //     }

        //     ViewData["Users"] = users;
        //     ViewData["SearchQuery"] = query;

        //     return View(); 
        // }
        [HttpGet]
        public async Task<IActionResult> Index(string query)
        {
            List<ProfileViewModel> users;

            if (string.IsNullOrWhiteSpace(query))
            {
                users = new List<ProfileViewModel>();
            }
            else
            {
                users = (await _profileService.SearchUsersAsync(query)).ToList();
            }

            ViewData["SearchQuery"] = query;

            return View(users);
        }

    }
}
