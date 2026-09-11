using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly BabyShopDbContext _context;

        public ProductController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: /Product
        public async Task<IActionResult> Index(
            string? category,
            string? gender,
            string? search,
            bool? isNew,
            bool? isSale,
            string? sort,
            decimal? minPrice,
            decimal? maxPrice,
            int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            // Lọc theo Danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Slug == category);
            }

            // Lọc theo Giới tính
            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender == gender);
            }

            // Tìm kiếm theo tên hoặc mô tả
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || 
                                         (p.ShortDescription != null && p.ShortDescription.Contains(search)) ||
                                         (p.Sku != null && p.Sku.Contains(search)));
            }

            // Lọc Hàng mới về
            if (isNew.HasValue && isNew.Value)
            {
                query = query.Where(p => p.IsNewArrival);
            }

            // Lọc Khuyến mãi
            if (isSale.HasValue && isSale.Value)
            {
                query = query.Where(p => p.DiscountPrice.HasValue && p.DiscountPrice > 0 && p.DiscountPrice < p.Price);
            }

            // Lọc theo Khoảng giá
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            // Sắp xếp
            switch (sort)
            {
                case "price-asc":
                    query = query.OrderBy(p => p.DiscountPrice ?? p.Price);
                    break;
                case "price-desc":
                    query = query.OrderByDescending(p => p.DiscountPrice ?? p.Price);
                    break;
                case "name-asc":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "popular":
                    query = query.OrderByDescending(p => p.IsBestSeller).ThenByDescending(p => p.CreatedAt);
                    break;
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            int pageSize = 12;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentCategory = category;
            ViewBag.CurrentGender = gender;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentIsNew = isNew;
            ViewBag.CurrentIsSale = isSale;
            ViewBag.CurrentSort = sort;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(products);
        }

        // GET: /Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            // Sản phẩm tương tự (cùng danh mục hoặc giới tính)
            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id != id && p.IsActive && (p.CategoryId == product.CategoryId || p.Gender == product.Gender))
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            // Gợi ý sản phẩm mua kèm Deal Sốc (Add-on Deals)
            var addonProducts = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.Id != id && p.IsActive && p.IsAddonDeal && p.AddonPrice.HasValue && p.AddonPrice.Value > 0)
                .Take(4)
                .ToListAsync();

            ViewBag.AddonProducts = addonProducts;

            return View(product);
        }

        // POST: /Product/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string? customerName, string comment, string? babyInfo)
        {
            // Bắt buộc đăng nhập để đánh giá và chấm điểm sao
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập tài khoản để viết đánh giá và xếp hạng sao cho sản phẩm mẹ nhé!";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Product", new { id = productId }) });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (rating < 1 || rating > 5) rating = 5;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? userId = null;
            bool isVerified = false;

            if (int.TryParse(userIdStr, out int uId))
            {
                userId = uId;
                // Kiểm tra xem khách hàng này đã từng đặt mua sản phẩm này chưa
                isVerified = await _context.OrderDetails
                    .AnyAsync(od => od.ProductId == productId && od.Order != null && od.Order.UserId == uId && od.Order.OrderStatus != "Cancelled");
            }

            var finalCustomerName = customerName;
            if (string.IsNullOrWhiteSpace(finalCustomerName))
            {
                finalCustomerName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Khách Hàng BabyShop";
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                CustomerName = finalCustomerName.Trim(),
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? "Sản phẩm chất vải rất đẹp, mềm mát, con mặc rất vừa vặn!" : comment.Trim(),
                BabyInfo = string.IsNullOrWhiteSpace(babyInfo) ? null : babyInfo.Trim(),
                IsVerifiedPurchase = isVerified,
                IsApproved = true,
                HelpfulCount = 0,
                CreatedAt = DateTime.Now
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn mẹ đã đánh giá sản phẩm! Nhận xét và xếp hạng sao đã được đăng thành công.";
            return RedirectToAction("Details", new { id = productId });
        }
    }
}

