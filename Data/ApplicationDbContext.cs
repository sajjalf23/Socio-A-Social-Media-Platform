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
        { }
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<PostReaction> PostReactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Disable cascade delete for Comments → Posts
            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments) // Make sure Post class has ICollection<Comment> Comments { get; set; }
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict); // <- prevents multiple cascade paths error

            // Optional: keep cascade delete for Comments → User
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
