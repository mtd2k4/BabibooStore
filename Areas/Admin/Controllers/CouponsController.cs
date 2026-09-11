using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public CouponsController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/Coupons
        public async Task<IActionResult> Index(string? search, string? type, string? status, int page = 1)
        {
            var query = _context.Coupons.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c => c.Code.ToLower().Contains(search) || c.Title.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (type == "Percentage")
                    query = query.Where(c => c.DiscountType == DiscountType.Percentage);
                else if (type == "FixedAmount")
                    query = query.Where(c => c.DiscountType == DiscountType.FixedAmount);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var now = DateTime.Now;
                if (status == "Active")
                    query = query.Where(c => c.IsActive && (c.ExpiryDate == null || c.ExpiryDate >= now));
                else if (status == "Expired")
                    query = query.Where(c => c.ExpiryDate != null && c.ExpiryDate < now);
                else if (status == "Disabled")
                    query = query.Where(c => !c.IsActive);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            page = Math.Max(1, page);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var coupons = await query.OrderByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê nhanh
            ViewBag.TotalCoupons = await _context.Coupons.CountAsync();
            ViewBag.ActiveCoupons = await _context.Coupons.CountAsync(c => c.IsActive && (c.ExpiryDate == null || c.ExpiryDate >= DateTime.Now));
            ViewBag.TotalUsed = await _context.Coupons.SumAsync(c => c.UsedCount);

            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(coupons);
        }

        // GET: Admin/Coupons/Create
        public IActionResult Create()
        {
            var coupon = new Coupon
            {
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                MinOrderAmount = 0,
                MaxDiscountAmount = 50000,
                UsageLimit = 500,
                StartDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddMonths(1),
                IsActive = true
            };
            return View(coupon);
        }

        // POST: Admin/Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            coupon.Code = (coupon.Code ?? "").Trim().ToUpper();

            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
            {
                ModelState.AddModelError("Code", $"Mã voucher '{coupon.Code}' đã tồn tại trong hệ thống. Vui lòng chọn mã khác.");
            }

            if (coupon.DiscountType == DiscountType.Percentage && (coupon.DiscountValue <= 0 || coupon.DiscountValue > 100))
            {
                ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm phải nằm trong khoảng từ 1% đến 100%.");
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount && coupon.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Số tiền giảm phải lớn hơn 0 VNĐ.");
            }

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value <= coupon.StartDate)
            {
                ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải sau ngày bắt đầu.");
            }

            if (ModelState.IsValid)
            {
                _context.Coupons.Add(coupon);
                await _context.SaveChangesAsync();

                // Nếu là mã Săn Giờ Vàng (IsFlashHunt), tự động đồng bộ sẵn vào Kho Voucher của tất cả khách hàng
                if (coupon.IsFlashHunt)
                {
                    var customerIds = await _context.Users
                        .Where(u => u.Role != "Admin" && u.IsActive)
                        .Select(u => u.Id)
                        .ToListAsync();

                    var toAdd = customerIds.Select(uid => new UserCoupon
                    {
                        UserId = uid,
                        CouponId = coupon.Id,
                        CollectedAt = DateTime.Now,
                        ExpiryDate = coupon.ExpiryDate ?? DateTime.Now.AddDays(30),
                        IsUsed = false
                    });

                    _context.UserCoupons.AddRange(toAdd);
                    await _context.SaveChangesAsync();
                }

                await _auditLogService.LogAsync("Create", "Coupon", coupon.Id, $"Tạo voucher khuyến mãi: {coupon.Code}, Loại: {coupon.DiscountType}, Trị giá: {coupon.DiscountValue}");
                TempData["SuccessMessage"] = $"Đã tạo thành công voucher khuyến mãi '{coupon.Code}'!{(coupon.IsFlashHunt ? " (Đã đồng bộ sẵn vào kho của tất cả khách hàng)" : "")}";
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        // GET: Admin/Coupons/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá yêu cầu.";
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        // POST: Admin/Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return NotFound();
            }

            coupon.Code = (coupon.Code ?? "").Trim().ToUpper();

            if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code && c.Id != id))
            {
                ModelState.AddModelError("Code", $"Mã voucher '{coupon.Code}' đã được sử dụng bởi một chương trình khác.");
            }

            if (coupon.DiscountType == DiscountType.Percentage && (coupon.DiscountValue <= 0 || coupon.DiscountValue > 100))
            {
                ModelState.AddModelError("DiscountValue", "Giảm theo phần trăm phải nằm trong khoảng từ 1% đến 100%.");
            }
            else if (coupon.DiscountType == DiscountType.FixedAmount && coupon.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Số tiền giảm phải lớn hơn 0 VNĐ.");
            }

            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value <= coupon.StartDate)
            {
                ModelState.AddModelError("ExpiryDate", "Ngày hết hạn phải sau ngày bắt đầu.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();

                    // Nếu là mã Săn Giờ Vàng (IsFlashHunt), đồng bộ thêm cho những khách hàng chưa có
                    if (coupon.IsFlashHunt)
                    {
                        var customerIds = await _context.Users
                            .Where(u => u.Role != "Admin" && u.IsActive)
                            .Select(u => u.Id)
                            .ToListAsync();

                        var existingUserIds = await _context.UserCoupons
                            .Where(uc => uc.CouponId == coupon.Id)
                            .Select(uc => uc.UserId)
                            .ToListAsync();

                        var missingUserIds = customerIds.Except(existingUserIds).ToList();
                        if (missingUserIds.Any())
                        {
                            var toAdd = missingUserIds.Select(uid => new UserCoupon
                            {
                                UserId = uid,
                                CouponId = coupon.Id,
                                CollectedAt = DateTime.Now,
                                ExpiryDate = coupon.ExpiryDate ?? DateTime.Now.AddDays(30),
                                IsUsed = false
                            });
                            _context.UserCoupons.AddRange(toAdd);
                            await _context.SaveChangesAsync();
                        }
                    }

                    await _auditLogService.LogAsync("Update", "Coupon", coupon.Id, $"Cập nhật voucher: {coupon.Code}, Giảm: {coupon.DiscountValue}");
                    TempData["SuccessMessage"] = $"Đã cập nhật thông tin voucher '{coupon.Code}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Coupons.AnyAsync(e => e.Id == coupon.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(coupon);
        }

        // POST: Admin/Coupons/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync("Update", "Coupon", coupon.Id, $"{(coupon.IsActive ? "Kích hoạt" : "Tạm dừng")} voucher '{coupon.Code}'");
                TempData["SuccessMessage"] = $"Đã {(coupon.IsActive ? "kích hoạt" : "tạm dừng")} voucher '{coupon.Code}'!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Coupons/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                string code = coupon.Code;
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync("Delete", "Coupon", id, $"Xóa voucher '{code}' khỏi hệ thống");
                TempData["SuccessMessage"] = $"Đã xóa voucher '{code}' khỏi hệ thống.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
