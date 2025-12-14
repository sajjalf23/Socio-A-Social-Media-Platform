using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SocioApp.Data;
using SocioApp.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using SocioApp.Services;
using SocioApp.Hubs;


namespace SocioApp.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPostService _postService;

        private readonly IProfileService _profileService;

        private readonly ICommentService _commentService;

        private readonly IHubContext<NotificationHub> _hubContext;

        public PostController(UserManager<ApplicationUser> userManager, IPostService postService,
        ICommentService commentService, IProfileService profileService, IHubContext<NotificationHub> hubContext)
        {
            _userManager = userManager;
            _postService = postService;
            _commentService = commentService;
            _profileService = profileService;
            _hubContext = hubContext;
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
            return RedirectToAction("Index", "UserHome");

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

             return RedirectToAction("Index", "UserHome");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var success = await _postService.DeletePostAsync(id, user.Id);

            TempData[success ? "Message" : "Error"] =
                success ? "Post deleted successfully!" : "Could not delete post.";

             return RedirectToAction("Index", "UserHome");
        }

        [HttpPost]
        public async Task<IActionResult> Like(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });
            Console.WriteLine(user);
            var result = await _postService.LikePostAsync(postId, user.Id);
            Console.WriteLine(result);
            var post = await _postService.GetPostByIdAsync(postId);
            var userReaction = await _postService.GetUserReactionAsync(postId, user.Id);
            Console.WriteLine(post);
            Console.WriteLine(userReaction);
            return Json(new
            {
                success = result,
                likes = post?.LikesCount,
                dislikes = post?.DislikesCount,
                liked = userReaction == "Like",
                disliked = userReaction == "Dislike"
            });
        }


        [HttpPost]
        public async Task<IActionResult> DisLike(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var result = await _postService.DislikePostAsync(postId, user.Id);

            var post = await _postService.GetPostByIdAsync(postId);
            var userReaction = await _postService.GetUserReactionAsync(postId, user.Id);

            return Json(new
            {
                success = result,
                likes = post?.LikesCount,
                dislikes = post?.DislikesCount,
                liked = userReaction == "Like",
                disliked = userReaction == "Dislike"
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetUserReaction(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var userReaction = await _postService.GetUserReactionAsync(postId, user.Id);
            var post = await _postService.GetPostByIdAsync(postId);

            return Json(new
            {
                success = true,
                likes = post?.LikesCount ?? 0,
                dislikes = post?.DislikesCount ?? 0,
                liked = userReaction == "Like",
                disliked = userReaction == "Dislike"
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int postId, string content,string postownerid)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false, message = "Comment cannot be empty." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            try
            {
                var comment = new Comment
                {
                    PostId = postId,
                    UserId = user.Id,
                    Content = content
                };
                comment = await _commentService.AddCommentAsync(comment);
                if (postownerid != null && postownerid != user.Id)
                {
                    var message = $"<a href='/Profile/{user.Id}'>{user.UserName}</a> commented: \"{content}\" on your <a href='/Post/Index/{postId}'>post</a>";
                    await _hubContext.Clients.User(postownerid.ToString())
                        .SendAsync("ReceiveNotification", message);
                }
                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        comment.CommentId,
                        comment.UserId,
                        CommentUserName = user.UserName, // This is important
                        comment.Content,
                        CreatedAt = comment.CreatedAt.ToString("g")
                    }
                });

            }
            catch
            {
                return Json(new { success = false, message = "Error adding comment." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            try
            {
                var success = await _commentService.DeleteCommentAsync(commentId, user.Id);
                return Json(new { success });
            }
            catch
            {
                return Json(new { success = false, message = "Error deleting comment." });
            }
        }


    }
}
