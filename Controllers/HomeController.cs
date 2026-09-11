using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(BabyShopDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var featuredProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsFeatured)
                .Take(8)
                .ToListAsync();

            var newArrivals = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsNewArrival)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            var bestSellers = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsBestSeller)
                .Take(8)
                .ToListAsync();

            var girlProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Gender == "Bé gái")
                .Take(4)
                .ToListAsync();

            var boyProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Gender == "Bé trai")
                .Take(4)
                .ToListAsync();

            var newbornProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Gender == "Sơ sinh")
                .Take(4)
                .ToListAsync();

            var now = DateTime.Now;
            var flashSaleProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.IsFlashSale && p.FlashSalePrice.HasValue && p.FlashSalePrice.Value > 0 && (!p.FlashSaleEndTime.HasValue || p.FlashSaleEndTime.Value > now))
                .OrderByDescending(p => p.FlashSaleSold)
                .Take(4)
                .ToListAsync();

            // Lấy danh sách voucher ưu đãi đang kích hoạt để hiển thị trên popup trang chủ (chỉ mã công khai)
            ViewBag.ActiveCoupons = await _context.Coupons
                .Where(c => !c.IsForYou && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate.Value >= now))
                .OrderBy(c => c.MinOrderAmount)
                .Take(3)
                .ToListAsync();

            // Lấy danh sách ID voucher đã thu thập nếu người dùng đã đăng nhập
            var userCollectedCouponIds = new HashSet<int>();
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int uid))
                {
                    userCollectedCouponIds = (await _context.UserCoupons
                        .Where(uc => uc.UserId == uid)
                        .Select(uc => uc.CouponId)
                        .ToListAsync()).ToHashSet();
                }
            }
            ViewBag.UserCollectedCouponIds = userCollectedCouponIds;

            var viewModel = new HomeViewModel
            {
                Categories = categories,
                FeaturedProducts = featuredProducts,
                NewArrivals = newArrivals,
                BestSellers = bestSellers,
                GirlProducts = girlProducts,
                BoyProducts = boyProducts,
                NewbornProducts = newbornProducts,
                FlashSaleProducts = flashSaleProducts
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
