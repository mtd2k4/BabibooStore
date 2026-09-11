using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class WishlistController : Controller
    {
        private readonly BabyShopDbContext _context;

        public WishlistController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: /yeu-thich hoặc /Wishlist
        [Route("yeu-thich")]
        [Route("Wishlist")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Sản Phẩm Yêu Thích - BabyShop Studio Kids";
            return View();
        }

        // POST: /Wishlist/GetProducts
        // Nhận mảng IDs từ localStorage của trình duyệt và trả về chi tiết sản phẩm
        [HttpPost]
        public async Task<IActionResult> GetProducts([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = true, data = new List<object>() });
            }

            // Giới hạn tối đa 50 sản phẩm yêu thích
            var cleanIds = ids.Distinct().Take(50).ToList();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => cleanIds.Contains(p.Id) && p.IsActive)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    category = p.Category != null ? p.Category.Name : "Bộ Sưu Tập",
                    gender = p.Gender,
                    price = p.Price,
                    discountPrice = p.DiscountPrice,
                    currentPrice = p.CurrentPrice,
                    discountPercentage = p.DiscountPercentage,
                    isNew = p.IsNewArrival,
                    imageUrl = string.IsNullOrEmpty(p.MainImageUrl) 
                        ? "https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=600&q=80" 
                        : p.MainImageUrl
                })
                .ToListAsync();

            // Sắp xếp theo thứ tự ID gửi lên (mới thêm lên đầu)
            var sortedProducts = products
                .OrderBy(p => cleanIds.IndexOf(p.id))
                .ToList();

            return Json(new { success = true, data = sortedProducts });
        }
    }
}
