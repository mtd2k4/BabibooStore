using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public ProductsController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string? search, int? categoryId, string? gender, string? ageRange, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || (p.Sku != null && p.Sku.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(gender))
            {
                query = query.Where(p => p.Gender == gender);
            }

            int pageSize = 10;
            int totalItems;
            int totalPages;
            List<Product> products;

            if (!string.IsNullOrEmpty(ageRange))
            {
                var matchedProducts = (await query.OrderByDescending(p => p.CreatedAt).ToListAsync())
                    .Where(p => WebsiteBabyShop.Helpers.AgeRangeHelper.Matches(p.AgeRange, ageRange))
                    .ToList();

                totalItems = matchedProducts.Count;
                totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
                page = Math.Max(1, Math.Min(page, totalPages));

                products = matchedProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                totalItems = await query.CountAsync();
                totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
                page = Math.Max(1, Math.Min(page, totalPages));

                products = await query.OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.OrderBy(c => c.Id).ToListAsync(), "Id", "Name", categoryId);
            ViewBag.Search = search;
            ViewBag.Gender = gender;
            ViewBag.AgeRange = ageRange;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(products);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Id).ToListAsync(), "Id", "Name");
            return View(new Product { IsActive = true, IsNewArrival = true });
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<ProductVariant>? variants, string? defaultSizes, string? defaultColor)
        {
            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;

                // Tự động tạo SKU nếu chưa có
                if (string.IsNullOrEmpty(product.Sku))
                {
                    product.Sku = "BS-" + new Random().Next(1000, 9999);
                }

                // Tự động tạo slug
                if (string.IsNullOrEmpty(product.Slug))
                {
                    product.Slug = product.Name.ToLower().Replace(" ", "-").Replace("/", "-");
                }

                // 1. Thêm từ danh sách biến thể tùy chỉnh động nếu có
                var validVariants = variants?.Where(v => !string.IsNullOrWhiteSpace(v.Size) && !string.IsNullOrWhiteSpace(v.Color)).ToList();
                if (validVariants != null && validVariants.Any())
                {
                    product.Variants.Clear();
                    int index = 1;
                    foreach (var v in validVariants)
                    {
                        product.Variants.Add(new ProductVariant
                        {
                            Size = v.Size.Trim(),
                            Color = v.Color.Trim(),
                            ColorHex = string.IsNullOrEmpty(v.ColorHex) ? "#C57B57" : v.ColorHex.Trim(),
                            StockQuantity = 0, // Tồn kho ban đầu = 0, sẽ nhập hàng qua module Nhập Kho
                            Sku = string.IsNullOrEmpty(v.Sku) ? $"{product.Sku}-{v.Size.Replace(" ", "")}-{index++}" : v.Sku.Trim(),
                            ImageUrl = string.IsNullOrEmpty(v.ImageUrl) ? product.MainImageUrl : v.ImageUrl.Trim()
                        });
                    }
                }
                else if (!string.IsNullOrEmpty(defaultSizes))
                {
                    // 2. Thêm các biến thể kích cỡ mặc định từ chuỗi
                    var sizes = defaultSizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var color = string.IsNullOrEmpty(defaultColor) ? "Tiêu chuẩn" : defaultColor.Trim();
                    foreach (var s in sizes)
                    {
                        product.Variants.Add(new ProductVariant
                        {
                            Size = s,
                            Color = color,
                            ColorHex = "#C57B57",
                            StockQuantity = 0, // Tồn kho ban đầu = 0, nhập qua Nhập Kho
                            Sku = product.Sku + "-" + s.Replace(" ", ""),
                            ImageUrl = product.MainImageUrl
                        });
                    }
                }
                else
                {
                    // 3. Tạo ít nhất 1 size mặc định
                    product.Variants.Add(new ProductVariant
                    {
                        Size = "Free Size",
                        Color = string.IsNullOrEmpty(defaultColor) ? "Mặc định" : defaultColor.Trim(),
                        ColorHex = "#C57B57",
                        StockQuantity = 0, // Tồn kho ban đầu = 0, nhập qua Nhập Kho
                        Sku = product.Sku + "-FS",
                        ImageUrl = product.MainImageUrl
                    });
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync("Create", "Product", product.Id, $"Tạo sản phẩm mới: {product.Name}, Giá: {product.Price:N0}đ");

                TempData["SuccessMessage"] = $"Đã thêm sản phẩm \"{product.Name}\" với {product.Variants.Count} biến thể thành công!";
                TempData["NewlyCreatedProductId"] = product.Id;
                TempData["NewlyCreatedProductName"] = product.Name;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Id).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Id).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<ProductVariant>? variants)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products
                        .Include(p => p.Variants)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProduct == null) return NotFound();

                    existingProduct.Name = product.Name;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.Price = product.Price;
                    existingProduct.DiscountPrice = product.DiscountPrice;
                    existingProduct.Gender = product.Gender;
                    existingProduct.AgeRange = product.AgeRange;
                    existingProduct.Material = product.Material;
                    existingProduct.CareInstructions = product.CareInstructions;
                    existingProduct.ShortDescription = product.ShortDescription;
                    existingProduct.Description = product.Description;
                    existingProduct.MainImageUrl = product.MainImageUrl;
                    existingProduct.IsFeatured = product.IsFeatured;
                    existingProduct.IsNewArrival = product.IsNewArrival;
                    existingProduct.IsBestSeller = product.IsBestSeller;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.IsFlashSale = product.IsFlashSale;
                    existingProduct.FlashSalePrice = product.FlashSalePrice;
                    existingProduct.FlashSaleQuantity = product.FlashSaleQuantity;
                    existingProduct.FlashSaleSold = product.FlashSaleSold;
                    existingProduct.FlashSaleEndTime = product.FlashSaleEndTime;
                    existingProduct.IsAddonDeal = product.IsAddonDeal;
                    existingProduct.AddonPrice = product.AddonPrice;

                    // Đồng bộ danh sách biến thể
                    var validVariants = variants?.Where(v => !string.IsNullOrWhiteSpace(v.Size) && !string.IsNullOrWhiteSpace(v.Color)).ToList() 
                                        ?? new List<ProductVariant>();

                    var submittedIds = validVariants.Where(v => v.Id > 0).Select(v => v.Id).ToList();

                    // 1. Xóa các biến thể không còn trong danh sách gửi lên
                    var toRemove = existingProduct.Variants.Where(ev => !submittedIds.Contains(ev.Id)).ToList();
                    foreach (var item in toRemove)
                    {
                        _context.ProductVariants.Remove(item);
                    }

                    // 2. Cập nhật hoặc thêm mới các biến thể
                    int newIndex = 1;
                    foreach (var v in validVariants)
                    {
                        if (v.Id > 0)
                        {
                            var existingVariant = existingProduct.Variants.FirstOrDefault(ev => ev.Id == v.Id);
                            if (existingVariant != null)
                            {
                                existingVariant.Size = v.Size.Trim();
                                existingVariant.Color = v.Color.Trim();
                                existingVariant.ColorHex = string.IsNullOrEmpty(v.ColorHex) ? "#C57B57" : v.ColorHex.Trim();
                                // Tồn kho giữ nguyên theo thực tế kho hàng, chỉ thay đổi qua Nhập Kho hoặc Bán Hàng
                                existingVariant.Sku = string.IsNullOrEmpty(v.Sku) ? existingVariant.Sku : v.Sku.Trim();
                                existingVariant.ImageUrl = string.IsNullOrEmpty(v.ImageUrl) ? existingProduct.MainImageUrl : v.ImageUrl.Trim();
                            }
                        }
                        else
                        {
                            // Thêm mới biến thể: tồn kho ban đầu = 0, nhập kho qua module Nhập Kho
                            existingProduct.Variants.Add(new ProductVariant
                            {
                                ProductId = existingProduct.Id,
                                Size = v.Size.Trim(),
                                Color = v.Color.Trim(),
                                ColorHex = string.IsNullOrEmpty(v.ColorHex) ? "#C57B57" : v.ColorHex.Trim(),
                                StockQuantity = 0, // Tồn kho ban đầu = 0, quản lý qua Nhập Kho
                                Sku = string.IsNullOrEmpty(v.Sku) ? $"{existingProduct.Sku}-{v.Size.Replace(" ", "")}-{newIndex++}" : v.Sku.Trim(),
                                ImageUrl = string.IsNullOrEmpty(v.ImageUrl) ? existingProduct.MainImageUrl : v.ImageUrl.Trim()
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await _auditLogService.LogAsync("Update", "Product", product.Id, $"Cập nhật sản phẩm: {product.Name}, Giá: {product.Price:N0}đ, Flash Sale: {product.IsFlashSale}");

                    TempData["SuccessMessage"] = $"Đã cập nhật sản phẩm \"{product.Name}\" và danh sách biến thể thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
                    throw;
                }
            }

            ViewBag.CategoryId = new SelectList(await _context.Categories.OrderBy(c => c.Id).ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                string name = product.Name;
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync("Delete", "Product", id, $"Xóa sản phẩm: {name}");
                TempData["SuccessMessage"] = "Đã xóa sản phẩm thành công.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Products/PrintInventoryReport
        public async Task<IActionResult> PrintInventoryReport(int? categoryId, string? stockStatus)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query.OrderBy(p => p.Category != null ? p.Category.Name : "").ThenBy(p => p.Name).ToListAsync();

            if (stockStatus == "low")
            {
                products = products.Where(p => p.TotalStock <= 5).ToList();
            }
            else if (stockStatus == "out")
            {
                products = products.Where(p => p.TotalStock == 0).ToList();
            }
            else if (stockStatus == "in_stock")
            {
                products = products.Where(p => p.TotalStock > 5).ToList();
            }

            ViewBag.SelectedCategory = categoryId.HasValue ? await _context.Categories.FindAsync(categoryId.Value) : null;
            ViewBag.StockStatus = stockStatus;
            ViewBag.PrintedAt = DateTime.Now;

            return View(products);
        }
    }
}
