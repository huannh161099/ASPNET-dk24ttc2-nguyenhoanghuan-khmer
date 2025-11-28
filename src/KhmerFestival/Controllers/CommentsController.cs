using KhmerFestival.Data;
using KhmerFestival.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhmerFestival.Controllers
{
    [Authorize] 
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;

        public CommentsController(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int postId, int? parentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Err"] = "Nội dung bình luận không được trống.";
                return await BackToPost(postId);
            }

            var userId = _http.HttpContext!.User?.Claims
                .FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var post = await _db.Posts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId && p.IsPublished);
            if (post == null) return NotFound();

            if (parentId.HasValue)
            {
                var parent = await _db.Comments.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == parentId.Value && c.PostId == postId);
                if (parent == null)
                {
                    TempData["Err"] = "Bình luận gốc không tồn tại.";
                    return RedirectToAction("Details", "Posts", new { slug = post.Slug });
                }
            }

            var cmt = new Comment
            {
                PostId = postId,
                ParentId = parentId,
                Content = content.Trim(),
                UserId = userId!,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(cmt);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Đã gửi bình luận.";
            return RedirectToAction("Details", "Posts", new { slug = post.Slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cmt = await _db.Comments.Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (cmt == null) return NotFound();

            var userId = _http.HttpContext!.User?.Claims
                .FirstOrDefault(c => c.Type.Contains("nameidentifier"))?.Value;
            var isOwner = cmt.UserId == userId;
            var isAdmin = User.IsInRole("Admin");
            if (!isOwner && !isAdmin) return Forbid();

            

            _db.Comments.Remove(cmt); 
            await _db.SaveChangesAsync();

            return RedirectToAction("Details", "Posts", new { slug = cmt.Post!.Slug });
        }

        private async Task<IActionResult> BackToPost(int postId)
        {
            var slug = await _db.Posts.Where(p => p.Id == postId)
                .Select(p => p.Slug).FirstOrDefaultAsync();
            if (slug == null) return NotFound();
            return RedirectToAction("Details", "Posts", new { slug });
        }
    }
}
