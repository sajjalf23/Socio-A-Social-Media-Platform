using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SocioApp.Data;
using SocioApp.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using SocioApp.Services;


namespace SocioApp.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPostService _postService;

        private readonly IProfileService _profileService;

        private readonly ICommentService _commentService;

        public PostController(UserManager<ApplicationUser> userManager, IPostService postService,
        ICommentService commentService, IProfileService profileService)
        {
            _userManager = userManager;
            _postService = postService;
            _commentService = commentService;
            _profileService = profileService;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int id = 1)
        {
            var post = await _postService.GetPostByIdAsync(id);
            if (post == null)
                return NotFound();
            var comments = await _commentService.GetCommentsAsync(id);
            if (HttpContext.Request.Cookies.TryGetValue("user", out var userJson))
            {
                var userInfo = JsonSerializer.Deserialize<UserCookieModel>(userJson);
                Console.WriteLine(userInfo);
                ViewBag.Display = new
                {
                    Post = post,
                    CurrentUser = userInfo!,
                    Comments = comments,
                    postuser = await _profileService.GetProfilebyidAsync(post.UserId)
                };

                return View();
            }
            return Redirect("/Login");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string caption, IFormFile? imagefile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (string.IsNullOrWhiteSpace(caption) && (imagefile == null || imagefile.Length == 0))
            {
                TempData["Error"] = "Post must contain text or an image.";
                return RedirectToAction("Create");
            }

            await _postService.CreatePostAsync(user.Id, caption, imagefile);

            TempData["Message"] = " Post created successfully!";
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _postService.GetPostByIdAsync(id);

            if (post == null)
                return NotFound();

            ViewData["Post"] = new
            {
                Id = post.PostId,
                Caption = post.Content,
                ImageUrl = post.MediaUrl ?? "~/images/default.png"
            };

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int postId, string caption, IFormFile? imagefile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var success = await _postService.EditPostAsync(postId, user.Id, caption, imagefile);

            TempData[success ? "Message" : "Error"] =
                success ? "Post updated successfully!" : "Could not update post.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var success = await _postService.DeletePostAsync(id, user.Id);

            TempData[success ? "Message" : "Error"] =
                success ? "Post deleted successfully!" : "Could not delete post.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Like(int postId)
        {
            try
            {
                var result = await _postService.LikePostAsync(postId);
                return Json(new { success = result });
            }
            catch
            {
                return Json(new
                { success = false, message = "An error occurred while liking the post." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DisLike(int postId)
        {
            try
            {
                var result = await _postService.DislikePostAsync(postId);
                return Json(new { success = result });
            }
            catch
            {
                return Json(new 
                { success = false, message = "An error occurred while disliking the post." });
            }
        }


    }
}
