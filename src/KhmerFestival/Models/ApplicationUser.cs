using Microsoft.AspNetCore.Identity;

namespace KhmerFestival.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
    }
}
