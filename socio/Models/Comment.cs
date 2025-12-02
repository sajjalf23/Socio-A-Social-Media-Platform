using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocioApp.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public int PostId { get; set; }

        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsHidden { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string? CommentUserName { get; set; }
    }
}
