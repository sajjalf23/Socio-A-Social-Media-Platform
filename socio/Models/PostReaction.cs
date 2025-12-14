using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SocioApp.Models
{
    public class PostReaction
    {
        [Key]
        public int PostReactionId { get; set; }

        [Required]
        public int PostId { get; set; }

        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // "Like", "Dislike", etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}