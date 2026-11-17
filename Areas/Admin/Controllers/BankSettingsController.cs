using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BankSettingsController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public BankSettingsController(BabyShopDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        // GET: Admin/BankSettings
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.BankAccounts
                .OrderByDescending(b => b.IsDefault)
                .ThenByDescending(b => b.IsActive)
                .ThenBy(b => b.Id)
                .ToListAsync();

            ViewBag.DefaultAccount = accounts.FirstOrDefault(b => b.IsDefault && b.IsActive)
                                     ?? accounts.FirstOrDefault(b => b.IsActive);
            return View(accounts);
        }

        // GET: Admin/BankSettings/Create
        public IActionResult Create()
        {
            return View(new BankAccount
            {
                BankCode = "VCB",
                BankName = "Vietcombank (Ngân hàng Ngoại thương Việt Nam)",
                QrTemplate = "compact2",
                IsActive = true,
                IsDefault = false,
                Note = "Mẹ vui lòng giữ nguyên nội dung chuyển khoản là mã đơn hàng để hệ thống tự động gạch nợ và xác nhận đơn trong 1-3 phút nhé!"
            });
        }

        // POST: Admin/BankSettings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankAccount model)
        {
            if (ModelState.IsValid)
            {
                model.BankCode = model.BankCode.Trim().ToUpperInvariant();
                model.BankName = model.BankName.Trim();
                model.AccountNumber = model.AccountNumber.Trim();
                model.AccountHolder = model.AccountHolder.Trim().ToUpperInvariant();
                model.Branch = string.IsNullOrWhiteSpace(model.Branch) ? null : model.Branch.Trim();
                model.QrTemplate = string.IsNullOrWhiteSpace(model.QrTemplate) ? "compact2" : model.QrTemplate.Trim();
                model.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                bool hasAny = await _context.BankAccounts.AnyAsync();
                if (!hasAny)
                {
                    model.IsDefault = true;
                    model.IsActive = true;
                }

                if (model.IsDefault)
                {
                    model.IsActive = true;
                    var others = await _context.BankAccounts.Where(b => b.IsDefault).ToListAsync();
                    foreach (var other in others)
                    {
                        other.IsDefault = false;
                        other.UpdatedAt = DateTime.Now;
                    }
                }

                _context.BankAccounts.Add(model);
                await _context.SaveChangesAsync();

                await EnsureActiveDefaultExistsAsync();

                await _auditLogService.LogAsync(
                    "Create",
                    "BankAccount",
                    model.Id,
                    $"Thêm cấu hình ngân hàng: {model.BankCode} - STK {model.AccountNumber} ({model.AccountHolder})");

                TempData["SuccessMessage"] = $"Đã thêm cấu hình ngân hàng {model.BankName} ({model.AccountNumber}) thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/BankSettings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.BankAccounts.FindAsync(id);
            if (account == null) return NotFound();

            return View(account);
        }

        // POST: Admin/BankSettings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BankAccount model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _context.BankAccounts.FindAsync(id);
                if (existing == null) return NotFound();

                string oldInfo = $"{existing.BankCode} - {existing.AccountNumber} ({existing.AccountHolder})";

                existing.BankCode = model.BankCode.Trim().ToUpperInvariant();
                existing.BankName = model.BankName.Trim();
                existing.AccountNumber = model.AccountNumber.Trim();
                existing.AccountHolder = model.AccountHolder.Trim().ToUpperInvariant();
                existing.Branch = string.IsNullOrWhiteSpace(model.Branch) ? null : model.Branch.Trim();
                existing.QrTemplate = string.IsNullOrWhiteSpace(model.QrTemplate) ? "compact2" : model.QrTemplate.Trim();
                existing.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
                existing.IsActive = model.IsActive;
                existing.IsDefault = model.IsDefault;
                existing.UpdatedAt = DateTime.Now;

                if (existing.IsDefault)
                {
                    existing.IsActive = true;
                    var others = await _context.BankAccounts.Where(b => b.Id != id && b.IsDefault).ToListAsync();
                    foreach (var other in others)
                    {
                        other.IsDefault = false;
                        other.UpdatedAt = DateTime.Now;
                    }
                }
                else if (!existing.IsActive)
                {
                    existing.IsDefault = false;
                }

                await _context.SaveChangesAsync();
                await EnsureActiveDefaultExistsAsync();

                await _auditLogService.LogAsync(
                    "Update",
                    "BankAccount",
                    existing.Id,
                    $"Cập nhật cấu hình ngân hàng từ [{oldInfo}] sang [{existing.BankCode} - {existing.AccountNumber} ({existing.AccountHolder})]");

                TempData["SuccessMessage"] = $"Đã cập nhật cấu hình ngân hàng {existing.BankName} ({existing.AccountNumber})!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Admin/BankSettings/SetDefault/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var target = await _context.BankAccounts.FindAsync(id);
            if (target == null) return NotFound();

            var allAccounts = await _context.BankAccounts.ToListAsync();
            foreach (var acc in allAccounts)
            {
                acc.IsDefault = (acc.Id == id);
                if (acc.Id == id)
                {
                    acc.IsActive = true;
                }
                acc.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                "Update",
                "BankAccount",
                target.Id,
                $"Đặt ngân hàng mặc định nhận tiền VietQR: {target.BankCode} - STK {target.AccountNumber}");

            TempData["SuccessMessage"] = $"Đã đặt {target.BankName} ({target.AccountNumber}) làm tài khoản nhận thanh toán VietQR mặc định!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/BankSettings/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var target = await _context.BankAccounts.FindAsync(id);
            if (target == null) return NotFound();

            target.IsActive = !target.IsActive;
            if (!target.IsActive)
            {
                target.IsDefault = false;
            }
            target.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await EnsureActiveDefaultExistsAsync();

            await _auditLogService.LogAsync(
                "ChangeStatus",
                "BankAccount",
                target.Id,
                $"{(target.IsActive ? "Kích hoạt" : "Tạm ngưng")} tài khoản ngân hàng {target.BankCode} - STK {target.AccountNumber}");

            TempData["SuccessMessage"] = $"Đã {(target.IsActive ? "kích hoạt" : "tạm ngưng")} tài khoản {target.BankName} ({target.AccountNumber}).";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/BankSettings/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int count = await _context.BankAccounts.CountAsync();
            if (count <= 1)
            {
                TempData["ErrorMessage"] = "Hệ thống cần giữ lại ít nhất 1 tài khoản ngân hàng cấu hình. Bạn có thể bấm Sửa để thay đổi thông tin hoặc Tạm ngưng nhận tiền!";
                return RedirectToAction(nameof(Index));
            }

            var target = await _context.BankAccounts.FindAsync(id);
            if (target != null)
            {
                string summary = $"{target.BankCode} - {target.AccountNumber} ({target.AccountHolder})";
                _context.BankAccounts.Remove(target);
                await _context.SaveChangesAsync();
                await EnsureActiveDefaultExistsAsync();

                await _auditLogService.LogAsync("Delete", "BankAccount", id, $"Xóa cấu hình ngân hàng: {summary}");
                TempData["SuccessMessage"] = $"Đã xóa cấu hình ngân hàng {summary}.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task EnsureActiveDefaultExistsAsync()
        {
            bool hasDefault = await _context.BankAccounts.AnyAsync(b => b.IsActive && b.IsDefault);
            if (!hasDefault)
            {
                var firstActive = await _context.BankAccounts
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Id)
                    .FirstOrDefaultAsync();

                if (firstActive != null)
                {
                    firstActive.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
