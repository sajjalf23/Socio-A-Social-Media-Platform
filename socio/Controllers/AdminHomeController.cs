using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using SocioApp.Services;
using SocioApp.Models;
using Azure;
namespace socio.Controllers
{

    [Authorize(policy: "AdminPages")]
    public class AdminHomeController : Controller
    {

        private readonly IAdminService _adminService;
        private readonly IProfileService _profileService;

        private readonly ICommentService _commentService;
        public AdminHomeController(IAdminService adminService, IProfileService profileService, ICommentService c)
        {
            _adminService = adminService;
            _profileService = profileService;
            _commentService = c;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["UsersLast7Days"] = await _adminService.GetUsersLast7DaysAsync();
            ViewData["PostsLast7Days"] = await _adminService.GetPostsLast7DaysAsync();
            ViewData["CommentsLast7Days"] = await _adminService.GetCommentsLast7DaysAsync();
            ViewData["LikesLast7Days"] = await _adminService.GetLikesLast7DaysAsync();
            var totals = await _adminService.GetTotalCountsAsync();
            ViewData["TotalUsers"] = totals["TotalUsers"];
            ViewData["TotalPosts"] = totals["TotalPosts"];
            ViewData["TotalComments"] = totals["TotalComments"];
            ViewData["TotalLikes"] = totals["TotalLikes"];

            return View();
        }

        public async Task<IActionResult> AllUsers()
        {
            ViewData["Title"] = "All Users";

            try
            {
                var users = await _profileService.GetAllUserforadmin();

                var model = users.Select(user => new ProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    ProfileImage = user.ProfileImage ?? "~/images/defaultprofile.png",
                    PostsCount = user.PostsCount,
                    Email = user.Email,
                    IsBanned = user.IsBanned,
                    Bio = user.Bio
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Redirect("/AdminHome");
            }
        }



        // [HttpGet]
        // public IActionResult Post(int id = 1)
        // {
        //     var post = new
        //     {
        //         Id = id,
        //         Username = "john_doe",
        //         ProfileImage = "~/images/profile.png",
        //         PostImage = "~/images/feedimg2.png",
        //         TimeAgo = "2 days ago",
        //         Likes = 125,
        //         Content = "Sunday vibes  Coffee, music, and a lazy afternoon.",
        //         Comments = new List<dynamic> {
        //             new { Username="amy_21", Text="Love this! " },
        //             new { Username="mike_dev", Text="Perfect chill day setup!" },
        //             new { Username="alex", Text="Great photo " },
        //             new { Username="sarah", Text="I need this kind of Sunday!" }
        //         }
        //     };

        //     ViewData["Post"] = post!;
        //     return View();
        // }

        // [HttpGet]
        // public IActionResult Profile()
        // {
        //     var profile = new
        //     {
        //         Username = "john_doe",
        //         Bio = "Professional procrastinator. Do not disturb.\nPowered by caffeine and bad decisions ☕️\nI put the 'Pro' in 'Procrastinate.'",
        //         PostsCount = 8,
        //         ProfileImage = "~/images/profile.png",
        //         Posts = new List<string>
        //         {
        //             "~/images/feedimg.png",
        //             "~/images/feedimg2.png",
        //             "~/images/profile.png",
        //             "~/images/feedimg2.png",
        //             "~/images/feedimg.png",
        //             "~/images/feedimg2.png",
        //             "~/images/feedimg.png",
        //             "~/images/profile.png"
        //         }
        //     };

        //     ViewData["Profile"] = profile;
        //     return View();
        // }


        [HttpGet("AdminHome/Comments/{userId}")]
        public async Task<IActionResult> Comments(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("UserId is required");

            var comments = await _commentService.GetCommentsByUserAsync(userId);
            return View(comments);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideUnhideComment(int id)
        {
            bool success = await _commentService.ToggleHideCommentAsync(id);

            if (!success)
                return Json(new { success = false, message = "Failed to toggle comment visibility." });

            return Json(new { success = true });
        }


        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public JsonResult BanUnbanPost(int id)
        // {
        //     var post = _context.Posts.Find(id);
        //     if (post == null) return Json(new { success = false, message = "Post not found" });

        //     post.IsBanned = !post.IsBanned;
        //     _context.SaveChanges();

        //     return Json(new { success = true });
        // }

        [HttpPost]
        public async Task<IActionResult> BanUnbanUser(string id)
        {
            Console.WriteLine($"BanUnbanUser called with ID: {id}");
            if (User.HasClaim("CanBanUser", "true"))
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    Console.WriteLine("Invalid user ID received.");
                    return Json(new { success = false, message = "Invalid user ID." });
                }

                try
                {
                    var isBanned = await _profileService.ToggleBanAsync(id);
                    Console.WriteLine($"User {id} new banned status: {isBanned}");
                    return Json(new { success = true, isBanned, message = "User status updated." });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error toggling ban for user {id}: {ex}");
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return BadRequest("admin only can do this");
        }



        [HttpGet]
        public async Task<IActionResult> Search(string query)
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
