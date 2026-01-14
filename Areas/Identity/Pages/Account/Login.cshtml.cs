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
            Input = new InputModel();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; } = "/";

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/";
        }

        // public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        // {
        //     ReturnUrl = returnUrl ?? "/";

        //     if (Input == null || !ModelState.IsValid)
        //         return Page();

        //     var user = await _userManager.FindByEmailAsync(Input.Email);

        //     if (user == null)
        //     {
        //         ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //         return Page();
        //     }

        //     if (user.IsBanned)
        //     {
        //         ModelState.AddModelError(string.Empty, "Your account has been banned. Contact support.");
        //         return Page();
        //     }

        //     var result = await _signInManager.PasswordSignInAsync(
        //         Input.Email!,
        //         Input.Password
        //         ,isPersistent: false,
        //         lockoutOnFailure: false
        //     );

        //     if (result.Succeeded)
        //     {
        //         var basicUser = new UserCookieModel
        //         {
        //             Id = user.Id,
        //             Email = Input.Email,
        //             UserName = user.UserName!,
        //             Name = user.Name,
        //             Bio = user.Bio,
        //             ProfilePicture = user.ProfilePicture
        //         };

        //         string userJson = JsonSerializer.Serialize(basicUser);

        //         HttpContext.Response.Cookies.Append(
        //             "user",
        //             userJson,
        //             new CookieOptions
        //             {
        //                 HttpOnly = true,    
        //                 Secure = true,      
        //                 Expires = DateTimeOffset.UtcNow.AddDays(7),
        //                 SameSite = SameSiteMode.Lax 
        //             }

        //         );

        //         Console.WriteLine("User logged in: " + userJson);
        //         return RedirectToAction("Index", "UserHome");
        //     }


        //     ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //     return Page();
        // }

        // [BindProperty]
        // public LoginInputModel Input { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/UserHome";

            //  Validate input
            if (Input == null || !ModelState.IsValid)
            {
                Console.WriteLine("ModelState invalid or Input is null.");
                return Page();
            }

            // 2️⃣ Find user by email
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                Console.WriteLine($"User not found for email: {Input.Email}");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            //  Check if user is banned
            if (user.IsBanned)
            {
                Console.WriteLine($"User {user.Email} is banned.");
                ModelState.AddModelError(string.Empty, "Your account has been banned. Contact support.");
                return Page();
            }
            // Console.WriteLine(user);
            //  Attempt sign-in
            var result = await _signInManager.PasswordSignInAsync(
                user,       
                Input.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (!result.Succeeded)
            {
                Console.WriteLine($"SignIn failed for {Input.Email}. Result: {result}");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Create user cookie
            var basicUser = new UserCookieModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName ?? string.Empty,
                Name = user.Name ?? string.Empty,
                Bio = user.Bio ?? string.Empty,
                ProfilePicture = user.ProfilePicture ?? string.Empty
            };

            string userJson = JsonSerializer.Serialize(basicUser);

            HttpContext.Response.Cookies.Append(
                "user",
                userJson,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Contains("Development"),
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Lax
                }
            );

            Console.WriteLine("User logged in successfully: " + userJson);

            // Redirect to homepage or returnUrl
            return LocalRedirect(ReturnUrl);
        }

    }
}
