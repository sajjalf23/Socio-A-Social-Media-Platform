using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace SocioApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
        public bool IsBanned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
