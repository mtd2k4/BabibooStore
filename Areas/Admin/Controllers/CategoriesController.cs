using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly BabyShopDbContext _context;

        public CategoriesController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(int page = 1)
        {
            var query = _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .AsQueryable();

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(categories);
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View(new Category { IsActive = true, DisplayOrder = 0 });
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(category.Slug))
                {
                    category.Slug = category.Name.ToLower().Replace(" ", "-").Replace("/", "-");
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo danh mục \"{category.Name}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Categories.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.Name = category.Name;
                    existing.Slug = category.Slug;
                    existing.Description = category.Description;
                    existing.ImageUrl = category.ImageUrl;
                    existing.DisplayOrder = category.DisplayOrder;
                    existing.IsActive = category.IsActive;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật danh mục \"{category.Name}\"!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == category.Id)) return NotFound();
                    throw;
                }
            }
            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category != null)
            {
                if (category.Products.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục này vì đang có sản phẩm thuộc danh mục!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa danh mục thành công.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
