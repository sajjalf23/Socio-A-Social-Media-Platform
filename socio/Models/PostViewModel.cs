using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SocioApp.Models
{
    public class PostViewModel
    {
        public IFormFile Image { get; set; }
        public string Caption { get; set; }
        public string Location { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Tags { get; set; }
    }
}
