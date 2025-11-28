using KhmerFestival.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhmerFestival.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public UsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
        {
            _users = users;
            _roles = roles;
        }

        
        public class UserRow
        {
            public string Id { get; set; } = default!;
            public string Email { get; set; } = default!;
            public string? DisplayName { get; set; }
            public IList<string> Roles { get; set; } = new List<string>();
        }

        
        public async Task<IActionResult> Index()
        {
            
            var users = await _users.Users
                .AsNoTracking()
                .ToListAsync();

            
            var list = new List<UserRow>();
            foreach (var u in users)
            {
                var roles = await _users.GetRolesAsync(u);
                list.Add(new UserRow
                {
                    Id = u.Id,
                    Email = u.Email!,
                    DisplayName = u.DisplayName,
                    Roles = roles
                });
            }

            return View(list);
        }

        
        [HttpGet]
        public IActionResult Create() => View();

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string email, string password, string? displayName, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Err"] = "Email và mật khẩu là bắt buộc.";
                return View();
            }

            var exists = await _users.FindByEmailAsync(email);
            if (exists != null)
            {
                TempData["Err"] = "Email đã tồn tại.";
                return View();
            }

            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = displayName };
            var created = await _users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                TempData["Err"] = string.Join("; ", created.Errors.Select(e => e.Description));
                return View();
            }

            
            if (isAdmin)
            {
                if (!await _roles.RoleExistsAsync("Admin"))
                    await _roles.CreateAsync(new IdentityRole("Admin"));

                await _users.AddToRoleAsync(user, "Admin");
            }

            TempData["Ok"] = "Tạo người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var meId = _users.GetUserId(User);
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isAdmin = await _users.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                
                if (meId == id)
                {
                    TempData["Err"] = "Bạn không thể tự gỡ quyền Admin của chính mình.";
                    return RedirectToAction(nameof(Index));
                }
                await _users.RemoveFromRoleAsync(user, "Admin");
                TempData["Ok"] = $"Đã gỡ quyền Admin của {user.Email}.";
            }
            else
            {
                if (!await _roles.RoleExistsAsync("Admin"))
                    await _roles.CreateAsync(new IdentityRole("Admin"));

                await _users.AddToRoleAsync(user, "Admin");
                TempData["Ok"] = $"Đã cấp quyền Admin cho {user.Email}.";
            }
            return RedirectToAction(nameof(Index));
        }

        
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.UserEmail = user.Email;
            ViewBag.UserId = user.Id;
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Err"] = "Mật khẩu mới không được trống.";
                return RedirectToAction(nameof(ResetPassword), new { id });
            }

            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var result = await _users.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                TempData["Err"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(ResetPassword), new { id });
            }

            TempData["Ok"] = "Đã đặt lại mật khẩu.";
            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var meId = _users.GetUserId(User);
            if (meId == id)
            {
                TempData["Err"] = "Bạn không thể xoá chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _users.FindByIdAsync(id);
            if (user != null)
            {
                await _users.DeleteAsync(user);
                TempData["Ok"] = "Đã xoá người dùng.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
