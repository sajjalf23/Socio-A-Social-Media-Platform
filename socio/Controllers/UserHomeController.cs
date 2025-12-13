
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SocioApp.Models;
using SocioApp.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocioApp.Controllers
{
    [Authorize]
    public class UserHomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPostService _postService;
        private readonly IProfileService _profileService;
        private readonly ICommentService _commentService;

        public UserHomeController(
            UserManager<ApplicationUser> userManager,
            IPostService postService,
            IProfileService profileService,
            ICommentService commentService)
        {
            _userManager = userManager;
            _postService = postService;
            _profileService = profileService;
            _commentService = commentService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var postIds = await _postService.GetAllPostsAsync();
            var postsView = new List<dynamic>();

            foreach (var row in postIds)
            {
                int postId = row.PostId;

                var post = await _postService.GetPostByIdAsync(postId);
                if (post == null) continue;

                var profile = await _profileService.GetProfilebyidAsync(post.UserId);
                var comments = await _commentService.GetCommentsAsync(post.PostId);
 
                postsView.Add(new
                {
                    PostId = post.PostId,
                    Username = profile?.Username ?? "Unknown",
                    ProfileImage = !string.IsNullOrWhiteSpace(profile?.ProfileImage)
                        ? profile.ProfileImage
                        : "/images/defaultprofile.png",

                    PostImage = !string.IsNullOrWhiteSpace(post.MediaUrl)
                        ? post.MediaUrl
                        : "/images/noimage.png",

                    TimeAgo = GetTimeAgo(post.CreatedAt),
                    Likes = post.LikesCount,
                    Content = post.Content,
                    Comments = comments.Select(c => new
                    {
                        Username = c.CommentUserName,
                        Text = c.Content
                    }).ToList()
                });
            }

            ViewData["Posts"] = postsView;
            return View();
        }

        private string GetTimeAgo(DateTime date)
        {
            var ts = DateTime.Now - date;
            if (ts.TotalDays > 365) return $"{(int)(ts.TotalDays / 365)} years ago";
            if (ts.TotalDays > 30) return $"{(int)(ts.TotalDays / 30)} months ago";
            if (ts.TotalDays > 7) return $"{(int)(ts.TotalDays / 7)} weeks ago";
            if (ts.TotalDays > 1) return $"{(int)ts.TotalDays} days ago";
            if (ts.TotalHours > 1) return $"{(int)ts.TotalHours} hours ago";
            if (ts.TotalMinutes > 1) return $"{(int)ts.TotalMinutes} minutes ago";
            return "just now";
        }
    }
}
