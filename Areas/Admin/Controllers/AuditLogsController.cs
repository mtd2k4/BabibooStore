using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly BabyShopDbContext _context;

        public AuditLogsController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AuditLogs
        public async Task<IActionResult> Index(string? search, string? actionType, string? entityName, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(a => a.Description.ToLower().Contains(search)
                    || a.UserName.ToLower().Contains(search)
                    || (a.IpAddress != null && a.IpAddress.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(a => a.Action == actionType);
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            int pageSize = 15;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var logs = await query.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê nhanh
            var today = DateTime.Today;
            ViewBag.TotalLogs = await _context.AuditLogs.CountAsync();
            ViewBag.TodayLogs = await _context.AuditLogs.CountAsync(a => a.CreatedAt >= today);
            ViewBag.ActionTypes = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
            ViewBag.EntityNames = await _context.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();

            ViewBag.Search = search;
            ViewBag.ActionType = actionType;
            ViewBag.EntityName = entityName;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(logs);
        }
    }
}
