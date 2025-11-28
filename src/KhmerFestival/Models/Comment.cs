using System.ComponentModel.DataAnnotations;

namespace KhmerFestival.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required] public int PostId { get; set; }
        public Post? Post { get; set; }

        public int? ParentId { get; set; }
        public Comment? Parent { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        [Required, MaxLength(2000)]
        public string Content { get; set; } = default!;

        [Required] public string UserId { get; set; } = default!;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
