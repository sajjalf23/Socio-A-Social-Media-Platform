using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SocioApp.Models;
using System.Security.Claims;  
using System.Linq;   

namespace SocioApp.Authorization
{
    public class EditPostAuthorizationHandler : 
        AuthorizationHandler<EditPostRequirement, Post>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EditPostAuthorizationHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            EditPostRequirement requirement,
            Post post)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (userId == null)
            {
                context.Fail();
                return;
            }

            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            if (post.UserId == userId)
            {
                context.Succeed(requirement);
                return;
            }

            if (context.User.HasClaim("CanEditAllPosts", "true"))
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
        }
    }
}