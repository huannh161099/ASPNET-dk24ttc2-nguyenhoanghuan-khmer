using KhmerFestival.Data;
using KhmerFestival.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace KhmerFestival.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public PostsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            var s = sb.ToString().Normalize(NormalizationForm.FormC);
            return s.Replace("đ", "d").Replace("Đ", "D");
        }

        private static string Slugify(string? input)
        {
            var s = RemoveDiacritics(input ?? "").ToLowerInvariant();
            s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
            s = Regex.Replace(s, @"\s+", "-").Trim('-');
            s = Regex.Replace(s, "-{2,}", "-");
            return s;
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allow = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allow.Contains(ext)) throw new InvalidOperationException("Định dạng ảnh không hợp lệ.");

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, fileName);
            using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            return $"/uploads/{fileName}";
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? ignoreId = null)
        {
            if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = Guid.NewGuid().ToString("N");
            var slug = baseSlug;
            int i = 1;
            while (await _db.Posts.AnyAsync(p => p.Slug == slug && (ignoreId == null || p.Id != ignoreId)))
                slug = $"{baseSlug}-{i++}";
            return slug;
        }

        
        public async Task<IActionResult> Index(int page = 1, string? keyword = null)
        {
            const int pageSize = 10;

            var q = _db.Posts.AsNoTracking().OrderByDescending(p => p.CreatedAt);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                q = q.Where(p => p.Title.Contains(k) || (p.Summary ?? "").Contains(k))
                     .OrderByDescending(p => p.CreatedAt);
            }

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.Keyword = keyword;

            return View(items);
        }

        
        public IActionResult Create()
        {
            var p = new Post { PublishedAt = DateTime.UtcNow };
            return View(p);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(post);

            var slugBase = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
            post.Slug = await EnsureUniqueSlugAsync(slugBase);

            if (imageFile != null)
                post.ImageUrl = await SaveImageAsync(imageFile);

            if (post.IsPublished)
                post.PublishedAt ??= DateTime.UtcNow;
            else
                post.PublishedAt = null;

            post.CreatedAt = DateTime.UtcNow;

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Đã tạo bài viết.";
            return RedirectToAction(nameof(Index));
        }

       
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Posts.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post, IFormFile? imageFile)
        {
            if (id != post.Id) return BadRequest();
            if (!ModelState.IsValid) return View(post);

            var p = await _db.Posts.FindAsync(id);
            if (p == null) return NotFound();

            p.Title = post.Title;
            p.Summary = post.Summary;
            p.Content = post.Content;

            var slugBase = string.IsNullOrWhiteSpace(post.Slug) ? Slugify(post.Title) : Slugify(post.Slug);
            p.Slug = await EnsureUniqueSlugAsync(slugBase, p.Id);

            if (imageFile != null)
                p.ImageUrl = await SaveImageAsync(imageFile);

            p.IsPublished = post.IsPublished;
            p.PublishedAt = post.IsPublished ? (post.PublishedAt ?? DateTime.UtcNow) : null;

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Đã cập nhật bài viết.";
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Posts.FindAsync(id);
            if (p != null)
            {
                _db.Posts.Remove(p);
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Đã xoá bài viết.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
