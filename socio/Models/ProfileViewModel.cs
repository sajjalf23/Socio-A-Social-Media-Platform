using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SocioApp.Models
{
    public class ProfileViewModel
    {
        public string Username { get; set; }             
        public string Bio { get; set; }                  
        public int PostsCount { get; set; }              
        public string ProfileImage { get; set; }         
        public IFormFile ProfileImageFile { get; set; }  
        public List<string> Posts { get; set; }          
    }
}
