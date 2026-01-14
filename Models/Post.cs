using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SocioApp.Models
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? MediaUrl { get; set; }

        public bool IsHidden { get; set; } = false;

        public int LikesCount { get; set; } = 0;

        public int DislikesCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

         public virtual ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    }
}
