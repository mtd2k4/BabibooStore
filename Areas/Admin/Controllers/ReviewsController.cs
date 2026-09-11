using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReviewsController : Controller
    {
        private readonly BabyShopDbContext _context;

        public ReviewsController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reviews
        public async Task<IActionResult> Index(string? search, int? rating, bool? isApproved, int? productId, int page = 1)
        {
            var query = _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(r => r.CustomerName.ToLower().Contains(search)
                    || r.Comment.ToLower().Contains(search)
                    || (r.CustomerPhone != null && r.CustomerPhone.Contains(search))
                    || (r.Product != null && r.Product.Name.ToLower().Contains(search)));
            }

            if (rating.HasValue && rating.Value > 0)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            if (isApproved.HasValue)
            {
                query = query.Where(r => r.IsApproved == isApproved.Value);
            }

            if (productId.HasValue && productId.Value > 0)
            {
                query = query.Where(r => r.ProductId == productId.Value);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var reviews = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê nhanh
            int totalReviews = await _context.ProductReviews.CountAsync();
            double avgRating = totalReviews > 0 ? await _context.ProductReviews.AverageAsync(r => (double)r.Rating) : 5.0;
            int fiveStarCount = await _context.ProductReviews.CountAsync(r => r.Rating == 5);
            int pendingCount = await _context.ProductReviews.CountAsync(r => !r.IsApproved);

            ViewBag.TotalReviews = totalReviews;
            ViewBag.AverageRating = Math.Round(avgRating, 1);
            ViewBag.FiveStarCount = fiveStarCount;
            ViewBag.PendingCount = pendingCount;

            ViewBag.Search = search;
            ViewBag.Rating = rating;
            ViewBag.IsApproved = isApproved;
            ViewBag.ProductId = productId;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            ViewBag.Products = new SelectList(await _context.Products.OrderBy(p => p.Name).Select(p => new { p.Id, p.Name }).ToListAsync(), "Id", "Name", productId);

            return View(reviews);
        }

        // POST: Admin/Reviews/ToggleApprove/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApprove(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = !review.IsApproved;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã {(review.IsApproved ? "duyệt hiển thị" : "ẩn")} đánh giá của khách '{review.CustomerName}'!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Reviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa đánh giá khỏi hệ thống.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
