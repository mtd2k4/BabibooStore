using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReturnRequestsController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public ReturnRequestsController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/ReturnRequests
        public async Task<IActionResult> Index(string? status, string? faultSource, string? search, int page = 1)
        {
            var query = _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (!string.IsNullOrEmpty(faultSource))
            {
                query = query.Where(r => r.FaultSource == faultSource);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r => r.ReturnCode.ToLower().Contains(term) ||
                                         (r.Order != null && r.Order.OrderCode.ToLower().Contains(term)) ||
                                         (r.CustomerName != null && r.CustomerName.ToLower().Contains(term)) ||
                                         (r.CustomerPhone != null && r.CustomerPhone.Contains(term)));
            }

            // Stats for filter badges
            ViewBag.TotalPending = await _context.ReturnRequests.CountAsync(r => r.Status == "Pending");
            ViewBag.TotalShopFault = await _context.ReturnRequests.CountAsync(r => r.FaultSource == "ShopFault");
            ViewBag.TotalCarrierFault = await _context.ReturnRequests.CountAsync(r => r.FaultSource == "CarrierFault");
            ViewBag.TotalCustomer = await _context.ReturnRequests.CountAsync(r => r.FaultSource == "CustomerRequest");

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var items = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentFaultSource = faultSource;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(items);
        }

        // GET: Admin/ReturnRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var req = await _context.ReturnRequests
                .Include(r => r.Order!)
                    .ThenInclude(o => o.OrderDetails)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null) return NotFound();

            return View(req);
        }

        // POST: Admin/ReturnRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var req = await _context.ReturnRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null) return NotFound();

            req.Status = "Approved";
            req.AdminNote = adminNote;
            req.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "ApproveReturn",
                "ReturnRequest",
                req.Id,
                $"Duyệt yêu cầu đổi/trả {req.ReturnCode} (Đơn #{req.Order?.OrderCode ?? req.OrderId.ToString()}). Ghi chú: {adminNote ?? "Không có"}"
            );

            TempData["SuccessMessage"] = $"Đã duyệt thành công yêu cầu {req.ReturnCode}! Đội ngũ giao vận sẽ tiến hành liên hệ thu hồi/giao hàng mới.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/ReturnRequests/Processing/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetProcessing(int id, string? adminNote)
        {
            var req = await _context.ReturnRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null) return NotFound();

            req.Status = "Processing";
            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                req.AdminNote = adminNote;
            }
            req.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "ProcessReturn",
                "ReturnRequest",
                req.Id,
                $"Chuyển trạng thái yêu cầu {req.ReturnCode} sang 'Đang xử lý/Thu hồi'. Ghi chú: {adminNote ?? "Không có"}"
            );

            TempData["SuccessMessage"] = $"Đã chuyển yêu cầu {req.ReturnCode} sang trạng thái Đang xử lý / Bưu tá đến lấy.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/ReturnRequests/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string adminNote)
        {
            if (string.IsNullOrWhiteSpace(adminNote))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối để thông báo rõ ràng cho khách hàng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var req = await _context.ReturnRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null) return NotFound();

            req.Status = "Rejected";
            req.AdminNote = adminNote;
            req.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "RejectReturn",
                "ReturnRequest",
                req.Id,
                $"Từ chối yêu cầu đổi/trả {req.ReturnCode}. Lý do: {adminNote}"
            );

            TempData["SuccessMessage"] = $"Đã từ chối yêu cầu {req.ReturnCode} kèm lý do phản hồi cho khách hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/ReturnRequests/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? adminNote)
        {
            var req = await _context.ReturnRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null) return NotFound();

            req.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                req.AdminNote = adminNote;
            }
            req.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "CompleteReturn",
                "ReturnRequest",
                req.Id,
                $"Hoàn tất quy trình đổi/trả tiền/đổi hàng cho mã {req.ReturnCode}. Ghi chú: {adminNote ?? "Không có"}"
            );

            TempData["SuccessMessage"] = $"Yêu cầu {req.ReturnCode} đã được hoàn tất thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
