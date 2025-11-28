using KhmerFestival.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhmerFestival.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PostsController(ApplicationDbContext db) => _db = db;

        
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(int page = 1, string? q = null)
        {
            const int pageSize = 9;

            var query = _db.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim();
                query = query.Where(p => p.Title.Contains(k) || (p.Summary ?? "").Contains(k))
                             .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);
            }

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Query = q ?? "";

            return View(items);
        }

        [HttpGet("/bai-viet/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var post = await _db.Posts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug);
            if (post == null) return NotFound();

            var comments = await _db.Comments.AsNoTracking()
                .Where(c => c.PostId == post.Id && !c.IsDeleted)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Comments = comments;

            var related = await _db.Posts.AsNoTracking()
                .Where(p => p.IsPublished && p.Id != post.Id)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Take(4).ToListAsync();

            ViewBag.Related = related;
            return View(post);
        }
    }
}
