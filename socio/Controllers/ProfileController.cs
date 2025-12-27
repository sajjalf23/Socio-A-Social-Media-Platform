using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SocioApp.Services;
using System.Text.Json;
using System.Threading.Tasks;
using SocioApp.Models;

namespace SocioApp.Controllers
{
    [Authorize (policy:"UserPages")]

    [Route("Profile")]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("")]
        public async Task<IActionResult> MyProfile()
        {
            if (!HttpContext.Request.Cookies.TryGetValue("user", out var userJson))
                return Redirect("/Login");

            var userInfo = JsonSerializer.Deserialize<UserCookieModel>(userJson);
            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Id))
                return Redirect("/Login");

            var profile = await _profileService.GetProfileAsync(userInfo.Id.Trim());
            if (profile == null)
                return NotFound();

            profile.ProfileImage = profile?.ProfileImage ?? "/images/defaultprofile.png";
            ViewData["Profile"] = profile;
            return View("Index");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ViewProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            if (id.Equals("MyProfile", System.StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(MyProfile));

            var profile = await _profileService.GetProfileAsync(id.Trim());
            if (profile == null)
                return NotFound();

            profile.ProfileImage = profile?.ProfileImage ?? "/images/defaultprofile.png";
            ViewData["Profile"] = profile;
            return View("Index");
        }

        [HttpGet("Edit")]
        public IActionResult Edit()
        {
            if (!HttpContext.Request.Cookies.TryGetValue("user", out var userJson))
                return Redirect("/Login");

            var userInfo = JsonSerializer.Deserialize<UserCookieModel>(userJson);
            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Id))
                return Redirect("/Login");
            var profile = new
            {
                FullName = userInfo.Name,
                Username = userInfo.UserName, 
                Bio = userInfo.Bio ?? "",
                ProfileImageUrl = userInfo.ProfilePicture ,
            };

            ViewData["Profile"] = profile;
            return View();
        }
 
        [HttpPost("Edit")]
        public async Task<IActionResult> Edit(string fullname, string username, string bio, IFormFile? imagefile)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("user", out var userJson))
                return Redirect("/Login");

            var userInfo = JsonSerializer.Deserialize<UserCookieModel>(userJson);
            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Id))
                return Redirect("/Login");

            fullname = fullname?.Trim() ?? "";
            username = username?.Trim() ?? "";
            bio = bio?.Trim() ?? "";

            bool updated = await _profileService.UpdateProfileAsync(
                userInfo.Id.Trim(),
                username,
                fullname,
                bio,
                imagefile,
                userInfo.ProfilePicture ?? ""
            );

            TempData["Message"] = updated ? "Profile updated successfully!" : "Profile update failed!";
            return RedirectToAction(nameof(MyProfile));
        }


    }
}
