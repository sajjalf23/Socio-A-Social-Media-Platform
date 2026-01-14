using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace SocioApp.Models 
{
    public class UserCookieModel
    { 
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public string? UserName { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
    }
}
