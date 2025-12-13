using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SocioApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Text.Json;

namespace SocioApp.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }



        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || Input == null)
                return Page();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                Bio = string.Empty,
                Name = Input.FullName,
                ProfilePicture = string.Empty,
                IsBanned = false,
                CreatedAt = System.DateTime.UtcNow,
                UpdatedAt = System.DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            Console.WriteLine("User creation result: " + result.Succeeded);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                }

                await _userManager.AddToRoleAsync(user, "User");

                await _signInManager.SignInAsync(user, isPersistent: false);

                var basicUser = new UserCookieModel
                {
                    Id = user.Id, 
                    Email = user.Email,
                    UserName = user.UserName,
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
                        HttpOnly = true,
                        Secure = true, 
                        Expires = DateTimeOffset.UtcNow.AddDays(7),
                        SameSite = SameSiteMode.Lax 
                    }
                );

                Console.WriteLine("User cookie set: " + userJson);

                return RedirectToAction("Index", "UserHome");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

    }
}
