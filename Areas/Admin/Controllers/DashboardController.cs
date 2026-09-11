using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly BabyShopDbContext _context;

        public DashboardController(BabyShopDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var startOfToday = now.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            // 1. Doanh thu
            var validOrdersQuery = _context.Orders.Where(o => o.OrderStatus != "Cancelled");

            var totalRevenue = await validOrdersQuery.SumAsync(o => o.TotalAmount);
            var todayRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= startOfToday).SumAsync(o => o.TotalAmount);
            var thisMonthRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= startOfMonth).SumAsync(o => o.TotalAmount);
            var lastMonthRevenue = await validOrdersQuery
                .Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfMonth)
                .SumAsync(o => o.TotalAmount);

            double revenueGrowth = 0;
            if (lastMonthRevenue > 0)
            {
                revenueGrowth = Math.Round((double)(thisMonthRevenue - lastMonthRevenue) / (double)lastMonthRevenue * 100, 1);
            }

            // 2. Đơn hàng & Sản phẩm & Người dùng
            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending");
            var shippingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Shipping");
            var completedOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Completed");
            var cancelledOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Cancelled");
            var processingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Processing");

            var totalProducts = await _context.Products.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var totalStockCost = await _context.StockReceipts.Where(r => r.Status == "Completed").SumAsync(r => r.TotalAmount);

            // 3. Cảnh báo tồn kho thấp (StockQuantity <= 5)
            var lowStockVariants = await _context.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.StockQuantity <= 5 && v.Product != null && v.Product.IsActive)
                .OrderBy(v => v.StockQuantity)
                .Take(6)
                .ToListAsync();

            var lowStockCount = await _context.ProductVariants
                .CountAsync(v => v.StockQuantity <= 5 && v.Product != null && v.Product.IsActive);

            // 4. Biểu đồ doanh thu 7 ngày gần nhất
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => startOfToday.AddDays(-6 + i))
                .ToList();

            var last7DaysRevenue = new List<decimal>();
            var last7DaysLabels = new List<string>();

            foreach (var day in last7Days)
            {
                var nextDay = day.AddDays(1);
                var dayTotal = await validOrdersQuery
                    .Where(o => o.CreatedAt >= day && o.CreatedAt < nextDay)
                    .SumAsync(o => o.TotalAmount);

                last7DaysRevenue.Add(dayTotal);
                last7DaysLabels.Add(day.ToString("dd/MM"));
            }

            // 5. Top 5 sản phẩm bán chạy nhất
            var orderDetailsList = await _context.OrderDetails
                .Select(d => new
                {
                    d.ProductId,
                    d.ProductName,
                    d.ProductImage,
                    d.Quantity,
                    d.UnitPrice
                })
                .ToListAsync();

            var topSellingGroup = orderDetailsList
                .GroupBy(d => new { d.ProductId, d.ProductName, d.ProductImage })
                .Select(g => new TopSellingProductItem
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ProductImage = g.Key.ProductImage,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            // 6. 5 Đơn hàng gần nhất
            var recentOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Pass ViewBags
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.ThisMonthRevenue = thisMonthRevenue;
            ViewBag.RevenueGrowth = revenueGrowth;

            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ProcessingOrders = processingOrders;
            ViewBag.ShippingOrders = shippingOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CancelledOrders = cancelledOrders;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalStockCost = totalStockCost;
            ViewBag.LowStockCount = lowStockCount;

            ViewBag.Last7DaysLabelsJson = System.Text.Json.JsonSerializer.Serialize(last7DaysLabels);
            ViewBag.Last7DaysRevenueJson = System.Text.Json.JsonSerializer.Serialize(last7DaysRevenue);

            ViewBag.TopSelling = topSellingGroup;
            ViewBag.LowStockVariants = lowStockVariants;

            return View(recentOrders);
        }
    }
}
