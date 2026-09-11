using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public OrdersController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(string? status, int page = 1)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var orders = await query.OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, bool? isPaid)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                string oldStatus = order.OrderStatus;
                order.OrderStatus = status;
                if (isPaid.HasValue)
                {
                    order.IsPaid = isPaid.Value;
                }

                // Tự động tích điểm Baby Club khi đơn hàng hoàn thành (Completed)
                if (status == "Completed" && oldStatus != "Completed" && order.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(order.UserId.Value);
                    if (user != null)
                    {
                        bool alreadyAwarded = await _context.PointTransactions.AnyAsync(pt => pt.OrderId == order.Id && pt.Type == "Earn");
                        if (!alreadyAwarded)
                        {
                            // Hệ số nhân theo hạng thành viên
                            decimal multiplier = 1.0m;
                            if (user.MemberTier != null)
                            {
                                if (user.MemberTier.Contains("Kim Cương")) multiplier = 2.0m;
                                else if (user.MemberTier.Contains("Vàng")) multiplier = 1.5m;
                                else if (user.MemberTier.Contains("Bạc")) multiplier = 1.2m;
                            }

                            // Tỉ lệ tích luỹ chuẩn: 1.000 VNĐ = 1 điểm (đơn hàng từ 100k hoàn về 100 điểm base)
                            int pointsEarned = (int)Math.Floor((order.TotalAmount / 1000m) * multiplier);
                            user.TotalSpent += order.TotalAmount;

                            // Tự động cập nhật hạng thành viên và thời hạn (Kim Cương: trọn đời; các hạng khác: 12 tháng)
                            if (user.TotalSpent >= 10000000)
                            {
                                user.MemberTier = "Thành viên hạng Kim Cương";
                                user.TierExpiryDate = null; // Riêng hạng Kim Cương áp dụng giảm trọn đời (vĩnh viễn)
                            }
                            else if (user.TotalSpent >= 5000000)
                            {
                                user.MemberTier = "Thành viên hạng Vàng";
                                user.TierExpiryDate = DateTime.Now.AddYears(1);
                            }
                            else if (user.TotalSpent >= 2000000)
                            {
                                user.MemberTier = "Thành viên hạng Bạc";
                                user.TierExpiryDate = DateTime.Now.AddYears(1);
                            }
                            else
                            {
                                user.MemberTier = "Thành viên hạng Đồng";
                                user.TierExpiryDate = DateTime.Now.AddYears(1);
                            }

                            if (pointsEarned > 0)
                            {
                                user.RewardPoints += pointsEarned;
                                _context.PointTransactions.Add(new PointTransaction
                                {
                                    UserId = user.Id,
                                    OrderId = order.Id,
                                    PointsChanged = pointsEarned,
                                    Type = "Earn",
                                    Description = $"Tích {pointsEarned} điểm Baby Club từ đơn hàng giao thành công #{order.OrderCode}",
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Ghi nhận Audit Log
                await _auditLogService.LogAsync(
                    "ChangeStatus",
                    "Order",
                    order.Id,
                    $"Cập nhật trạng thái đơn #{order.OrderCode} từ '{oldStatus}' sang '{status}', Thanh toán: {(order.IsPaid ? "Đã thanh toán" : "Chưa")}"
                );

                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng {order.OrderCode} thành '{status}'!";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET/POST: Admin/Orders/PrintPickingList
        public async Task<IActionResult> PrintPickingList(string? ids)
        {
            var idList = new List<int>();
            if (!string.IsNullOrEmpty(ids))
            {
                idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int val) ? val : 0)
                    .Where(val => val > 0)
                    .ToList();
            }

            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (idList.Any())
            {
                query = query.Where(o => idList.Contains(o.Id));
            }
            else
            {
                // Mặc định lấy các đơn đang chờ chuẩn bị hàng (Processing hoặc Pending)
                query = query.Where(o => o.OrderStatus == "Processing" || o.OrderStatus == "Pending").Take(30);
            }

            var orders = await query.ToListAsync();
            ViewBag.SelectedOrderCount = orders.Count;
            ViewBag.OrderCodes = string.Join(", ", orders.Select(o => o.OrderCode));

            // Gom nhóm mặt hàng cần nhặt theo Tên, Màu, Size
            var groupedItems = orders.SelectMany(o => o.OrderDetails)
                .GroupBy(d => new { d.ProductName, d.Color, d.Size, d.ProductImage })
                .Select(g => new PickingItemViewModel
                {
                    ProductName = g.Key.ProductName,
                    Color = g.Key.Color,
                    Size = g.Key.Size,
                    ProductImage = g.Key.ProductImage,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    UnitPrice = g.First().UnitPrice
                })
                .OrderBy(x => x.ProductName)
                .ToList();

            return View("PickingList", groupedItems);
        }

        // GET: Admin/Orders/PrintWaybill/5?format=k80
        public async Task<IActionResult> PrintWaybill(int id, string format = "k80")
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            ViewBag.Format = format; // "k80" hoặc "a5"
            return View("Waybill", order);
        }

        // GET: Admin/Orders/PrintReport
        public async Task<IActionResult> PrintReport(DateTime? fromDate, DateTime? toDate, string? status)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Status = status;
            ViewBag.TotalRevenue = orders.Where(o => o.OrderStatus != "Cancelled").Sum(o => o.TotalAmount);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.CompletedOrders = orders.Count(o => o.OrderStatus == "Completed");

            return View(orders);
        }
    }

    public class PickingItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? ProductImage { get; set; }
        public int TotalQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
