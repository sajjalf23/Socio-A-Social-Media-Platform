using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SocioApp.Models
{ 
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty; 
        public int PostsCount { get; set; }
        public string ProfileImage { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        
        public List<PostViewModel> Posts { get; set; } = new List<PostViewModel>();
    }

}
