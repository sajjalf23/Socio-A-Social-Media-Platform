using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocioApp.Models;
using Microsoft.AspNetCore.Identity;


namespace SocioApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {}
         public DbSet<Post> Posts { get; set; } = null!; 
         public DbSet<Comment> Comments { get; set; } = null!;
    }
}
