using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Helpers;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public UsersController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1)
        {
            var query = _context.Users
                .Include(u => u.Orders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(search)
                    || u.Username.ToLower().Contains(search)
                    || u.Email.ToLower().Contains(search)
                    || (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active") query = query.Where(u => u.IsActive);
                else if (status == "Locked") query = query.Where(u => !u.IsActive);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var users = await query.OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê nhanh
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.AdminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            ViewBag.CustomerCount = await _context.Users.CountAsync(u => u.Role == "Customer");
            var aWeekAgo = DateTime.Now.AddDays(-7);
            ViewBag.NewThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= aWeekAgo);

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        // GET: Admin/Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            // Thống kê chi tiêu của khách
            decimal totalSpent = user.Orders.Where(o => o.OrderStatus != "Cancelled").Sum(o => o.TotalAmount);
            int totalOrders = user.Orders.Count;
            int completedOrders = user.Orders.Count(o => o.OrderStatus == "Completed");

            ViewBag.TotalSpent = totalSpent;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletedOrders = completedOrders;

            return View(user);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            return View(new User { Role = "Customer", IsActive = true });
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string password)
        {
            user.Username = (user.Username ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("PasswordHash", "Mật khẩu bắt buộc và phải có ít nhất 6 ký tự.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký tài khoản khác.");
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash = PasswordHelper.HashPassword(password);
                user.CreatedAt = DateTime.Now;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản '{user.Username}' ({user.Role})!";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // POST: Admin/Users/ToggleRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRole(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự thay đổi vai trò của chính tài khoản đang đăng nhập!";
                    return RedirectToAction(nameof(Index));
                }

                user.Role = user.Role == "Admin" ? "Customer" : "Admin";
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync("ChangeRole", "User", user.Id, $"Đổi vai trò tài khoản '{user.Username}' thành '{user.Role}'");
                TempData["SuccessMessage"] = $"Đã chuyển vai trò tài khoản '{user.Username}' thành '{user.Role}'!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình!";
                    return RedirectToAction(nameof(Index));
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync(user.IsActive ? "UnlockUser" : "LockUser", "User", user.Id, $"{(user.IsActive ? "Mở khóa" : "Khóa tạm thời")} tài khoản '{user.Username}'");
                TempData["SuccessMessage"] = $"Đã {(user.IsActive ? "mở khóa" : "khóa tạm thời")} tài khoản '{user.Username}'!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin đang thực hiện thao tác!";
                    return RedirectToAction(nameof(Index));
                }

                string name = user.FullName;
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync("Delete", "User", id, $"Xóa tài khoản người dùng: {name} (ID #{id})");
                TempData["SuccessMessage"] = $"Đã xóa người dùng '{name}' khỏi hệ thống.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
