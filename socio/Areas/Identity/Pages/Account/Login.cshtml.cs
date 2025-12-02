using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocioApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Text.Json;
using System;

namespace SocioApp.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            Input = new InputModel(); // Initialize to avoid CS8618
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; } = "/"; // Default redirect

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            public string Email { get; set; } = string.Empty; // Initialize to avoid CS8618

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty; // Initialize to avoid CS8618
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";

            if (Input == null || !ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            if (user.IsBanned)
            {
                ModelState.AddModelError(string.Empty, "Your account has been banned. Contact support.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                Input.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                // Only store essential, non-sensitive info
                var basicUser = new UserCookieModel
                {
                    Id = user.Id,
                    Email = Input.Email,
                    UserName = user.UserName!,
                    Name = user.Name,
                    Bio = user.Bio,
                    ProfilePicture = user.ProfilePicture
                };

                string userJson = JsonSerializer.Serialize(basicUser);

                HttpContext.Response.Cookies.Append(
                    "user",
                    userJson,
                    new CookieOptions
                    {
                        HttpOnly = true,    // prevents JS access
                        Secure = true,      // only over HTTPS
                        Expires = DateTimeOffset.UtcNow.AddDays(7),
                        SameSite = SameSiteMode.Lax 
                    }

                );

                Console.WriteLine("User logged in: " + userJson);
                return RedirectToAction("Index", "UserHome");
            }


            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
