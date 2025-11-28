using System.ComponentModel.DataAnnotations;

namespace KhmerFestival.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Title { get; set; } = default!;

        [MaxLength(180)]
        public string? Slug { get; set; }

        [MaxLength(500)]
        public string? Summary { get; set; }

        public string? Content { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
