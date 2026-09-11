using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StockReceiptsController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public StockReceiptsController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/StockReceipts
        public async Task<IActionResult> Index(string? search, string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var query = _context.StockReceipts
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.Product)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.ProductVariant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(r => r.ReceiptCode.ToLower().Contains(search) 
                    || r.SupplierName.ToLower().Contains(search) 
                    || (r.SupplierPhone != null && r.SupplierPhone.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var receipts = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê nhanh
            ViewBag.TotalReceipts = await _context.StockReceipts.CountAsync();
            ViewBag.TotalCost = await _context.StockReceipts.Where(r => r.Status == "Completed").SumAsync(r => r.TotalAmount);
            ViewBag.TotalQuantity = await _context.StockReceiptDetails
                .Where(d => d.StockReceipt != null && d.StockReceipt.Status == "Completed")
                .SumAsync(d => d.Quantity);

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(receipts);
        }

        private async Task<string> GetProductsJsonAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Sku = p.Sku ?? ("BS-" + p.Id),
                    p.Price,
                    p.MainImageUrl,
                    CategoryName = p.Category != null ? p.Category.Name : "Thời trang trẻ em",
                    Variants = p.Variants.Select(v => new
                    {
                        v.Id,
                        v.Size,
                        v.Color,
                        ColorHex = !string.IsNullOrEmpty(v.ColorHex) ? v.ColorHex : "#C57B57",
                        ImageUrl = !string.IsNullOrEmpty(v.ImageUrl) ? v.ImageUrl : p.MainImageUrl,
                        v.StockQuantity,
                        Sku = v.Sku ?? ""
                    }).ToList()
                })
                .ToListAsync();

            return System.Text.Json.JsonSerializer.Serialize(products);
        }

        // GET: Admin/StockReceipts/Create
        public async Task<IActionResult> Create(int? productId, int? variantId)
        {
            ViewBag.ProductsJson = await GetProductsJsonAsync();
            ViewBag.PreselectProductId = productId;
            ViewBag.PreselectVariantId = variantId;
            return View();
        }

        // POST: Admin/StockReceipts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockReceipt receipt, List<StockReceiptDetail> items)
        {
            if (string.IsNullOrWhiteSpace(receipt.ReceiptCode))
            {
                receipt.ReceiptCode = "PN-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
            }
            ModelState.Remove(nameof(receipt.ReceiptCode));

            if (items == null || !items.Any())
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất một mặt hàng vào phiếu nhập kho.");
            }

            if (ModelState.IsValid && items != null && items.Any())
            {
                receipt.CreatedAt = DateTime.Now;
                receipt.CreatedBy = User.Identity?.Name ?? "Quản Trị Viên";
                receipt.Status = "Completed";

                decimal total = 0;

                foreach (var item in items)
                {
                    if (item.Quantity <= 0) item.Quantity = 1;
                    if (item.ImportPrice < 0) item.ImportPrice = 0;
                    item.TotalPrice = item.Quantity * item.ImportPrice;
                    total += item.TotalPrice;

                    // Cập nhật tăng số lượng tồn kho của ProductVariant
                    if (item.ProductVariantId.HasValue && item.ProductVariantId.Value > 0)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity += item.Quantity;
                            if (string.IsNullOrEmpty(item.Color)) item.Color = variant.Color;
                            if (string.IsNullOrEmpty(item.Size)) item.Size = variant.Size;
                        }
                    }
                    else if (item.ProductId > 0)
                    {
                        // Nếu chưa chọn biến thể cụ thể, cập nhật biến thể đầu tiên của sản phẩm
                        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == item.ProductId);
                        if (variant != null)
                        {
                            variant.StockQuantity += item.Quantity;
                            item.ProductVariantId = variant.Id;
                            if (string.IsNullOrEmpty(item.Color)) item.Color = variant.Color;
                            if (string.IsNullOrEmpty(item.Size)) item.Size = variant.Size;
                        }
                    }

                    receipt.ReceiptDetails.Add(item);
                }

                receipt.TotalAmount = total;
                _context.StockReceipts.Add(receipt);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync("Restock", "StockReceipt", receipt.Id, $"Tạo phiếu nhập kho '{receipt.ReceiptCode}', NCC: {receipt.SupplierName}, Tổng tiền: {receipt.TotalAmount:N0}đ");

                TempData["SuccessMessage"] = $"Đã tạo thành công phiếu nhập kho '{receipt.ReceiptCode}' và tự động cập nhật số lượng tồn kho!";
                return RedirectToAction(nameof(Details), new { id = receipt.Id });
            }

            ViewBag.ProductsJson = await GetProductsJsonAsync();
            return View(receipt);
        }

        // GET: Admin/StockReceipts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var receipt = await _context.StockReceipts
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.Product)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập kho yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            return View(receipt);
        }

        // POST: Admin/StockReceipts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var receipt = await _context.StockReceipts
                .Include(r => r.ReceiptDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt != null)
            {
                string code = receipt.ReceiptCode;
                _context.StockReceipts.Remove(receipt);
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync("Delete", "StockReceipt", id, $"Xóa phiếu nhập kho: {code}");
                TempData["SuccessMessage"] = $"Đã xóa phiếu nhập '{code}' thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/StockReceipts/PrintReport
        public async Task<IActionResult> PrintReport(DateTime? fromDate, DateTime? toDate, string? status)
        {
            var query = _context.StockReceipts
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var receipts = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Status = status;
            ViewBag.PrintedAt = DateTime.Now;

            return View(receipts);
        }
    }
}
