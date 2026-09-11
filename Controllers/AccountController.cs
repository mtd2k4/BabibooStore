using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Helpers;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly BabyShopDbContext _context;

        public AccountController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);
            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Đăng nhập Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "BabyShopCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(4)
            };

            // Đăng xuất session cũ trước khi lưu session mới
            await HttpContext.SignOutAsync("BabyShopCookieAuth");

            await HttpContext.SignInAsync("BabyShopCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["SuccessMessage"] = $"Chào mừng bạn trở lại, {user.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                Role = "Customer",
                CreatedAt = DateTime.Now,
                IsActive = true,
                MemberTier = "Thành viên hạng Đồng",
                TierExpiryDate = DateTime.Now.AddYears(1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Đăng xuất phiên cũ (nếu có tài khoản admin đang lưu cookie)
            await HttpContext.SignOutAsync("BabyShopCookieAuth");

            // Tự động đăng nhập ngay cho tài khoản mới vừa tạo
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "BabyShopCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync("BabyShopCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["SuccessMessage"] = $"Đăng ký tài khoản thành công! Chào mừng {user.FullName} đến với BabyShop.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("BabyShopCookieAuth");
            TempData["SuccessMessage"] = "Bạn đã đăng xuất tài khoản thành công.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = null;

            if (int.TryParse(userIdStr, out int userId))
            {
                user = await _context.Users.FindAsync(userId);
            }
            else
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            }

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var totalOrders = await _context.Orders.CountAsync(o => o.UserId == user.Id);

            var pointTransactions = await _context.PointTransactions
                .Where(pt => pt.UserId == user.Id)
                .OrderByDescending(pt => pt.CreatedAt)
                .Take(20)
                .ToListAsync();

            // Chuẩn hóa tên hạng thành viên & thời hạn
            string tier = user.MemberTier ?? "Thành viên hạng Đồng";
            if (tier == "Mẹ Đồng") tier = "Thành viên hạng Đồng";
            else if (tier == "Mẹ Bạc") tier = "Thành viên hạng Bạc";
            else if (tier == "Mẹ Vàng") tier = "Thành viên hạng Vàng";
            else if (tier == "Mẹ Kim Cương") tier = "Thành viên hạng Kim Cương";

            bool isLifetime = tier.Contains("Kim Cương");
            int currentDiscountPercent = 3;
            DateTime? tierExpiry = null;

            if (isLifetime)
            {
                currentDiscountPercent = 20;
                tierExpiry = null; // Riêng hạng Kim Cương áp dụng trọn đời (vĩnh viễn)
            }
            else
            {
                tierExpiry = user.TierExpiryDate ?? (user.CreatedAt.AddYears(1) < DateTime.Now ? DateTime.Now.AddYears(1) : user.CreatedAt.AddYears(1));
                if (tier.Contains("Vàng")) currentDiscountPercent = 10;
                else if (tier.Contains("Bạc")) currentDiscountPercent = 8;
                else currentDiscountPercent = 3;
            }

            // Tính toán thông tin hạng tiếp theo, ưu đãi và phần thưởng chào mừng
            string nextTierName = "";
            decimal nextTierSpendThreshold = 0;
            decimal amountNeeded = 0;
            int progressPercent = 0;
            string nextTierRewardBonus = "";
            var nextTierPerks = new List<string>();
            var currentTierPerks = new List<string>();

            if (tier.Contains("Kim Cương"))
            {
                currentTierPerks = new List<string>
                {
                    "Chiết khấu giảm trực tiếp 20% TRỌN ĐỜI trên mọi đơn hàng",
                    "Đặc quyền duy trì hạng Kim Cương vĩnh viễn không bao giờ hết hạn",
                    "Hệ số tích điểm x2.0 (1.000đ = 2 điểm thưởng Baby Club - đơn 100k nhận 200 điểm)",
                    "Miễn phí vận chuyển hỏa tốc 2H toàn quốc không giới hạn",
                    "Chuyên viên tư vấn & Stylist thời trang riêng 1-1 cho bé",
                    "Hộp quà tri ân sinh nhật & sự kiện Baby Diamond Box cao cấp",
                    "Đặc quyền đổi trả sản phẩm tối đa 30 ngày tại nhà"
                };
                nextTierName = "Hạng cao nhất";
                progressPercent = 100;
                amountNeeded = 0;
                nextTierSpendThreshold = 10000000m;
                nextTierRewardBonus = "Bạn đã đạt đỉnh vinh danh cao nhất của Baby Club! Tận hưởng đặc quyền giảm 20% trọn đời.";
            }
            else if (tier.Contains("Vàng"))
            {
                currentTierPerks = new List<string>
                {
                    "Chiết khấu giảm trực tiếp 10% trên mọi đơn hàng (thời hạn 12 tháng)",
                    "Hệ số tích điểm x1.5 (1.000đ = 1.5 điểm thưởng Baby Club - đơn 100k nhận 150 điểm)",
                    "Miễn phí vận chuyển toàn quốc cho đơn từ 100.000đ",
                    "Quà sinh nhật độc quyền mẹ & bé trị giá 300.000đ",
                    "Ưu tiên xử lý đơn hàng và hỗ trợ CSKH 24/7",
                    "Chính sách đổi trả sản phẩm linh hoạt trong 15 ngày"
                };
                nextTierName = "Thành viên hạng Kim Cương";
                nextTierSpendThreshold = 10000000m;
                amountNeeded = Math.Max(0, nextTierSpendThreshold - user.TotalSpent);
                decimal baseTier = 5000000m;
                progressPercent = Math.Clamp((int)(((user.TotalSpent - baseTier) / (nextTierSpendThreshold - baseTier)) * 100), 0, 99);
                nextTierRewardBonus = "+200 điểm Baby Club + Voucher VIP 15% + Quà tặng Baby Diamond Box cao cấp";
                nextTierPerks = new List<string>
                {
                    "Đặc quyền tối thượng: Chiết khấu giảm trực tiếp 20% TRỌN ĐỜI trên mọi đơn hàng",
                    "Duy trì hạng Kim Cương vĩnh viễn không bao giờ hết hạn",
                    "Nâng cấp hệ số tích điểm lên x2.0 (đơn 100k nhận 200 điểm thưởng Baby Club)",
                    "Miễn phí giao hàng hỏa tốc 2H toàn quốc",
                    "Chuyên viên tư vấn dinh dưỡng & stylist riêng 1-1 cho bé",
                    "Đổi trả tối đa 30 ngày tận nơi miễn phí"
                };
            }
            else if (tier.Contains("Bạc"))
            {
                currentTierPerks = new List<string>
                {
                    "Chiết khấu giảm trực tiếp 8% trên mọi đơn hàng (thời hạn 12 tháng)",
                    "Hệ số tích điểm x1.2 (1.000đ = 1.2 điểm thưởng Baby Club - đơn 100k nhận 120 điểm)",
                    "Miễn phí giao hàng cho đơn hàng từ 200.000đ",
                    "Quà tặng bất ngờ nhân dịp sinh nhật của bé",
                    "Chính sách đổi trả sản phẩm linh hoạt trong vòng 7 ngày"
                };
                nextTierName = "Thành viên hạng Vàng";
                nextTierSpendThreshold = 5000000m;
                amountNeeded = Math.Max(0, nextTierSpendThreshold - user.TotalSpent);
                decimal baseTier = 2000000m;
                progressPercent = Math.Clamp((int)(((user.TotalSpent - baseTier) / (nextTierSpendThreshold - baseTier)) * 100), 0, 99);
                nextTierRewardBonus = "+100 điểm Baby Club + Voucher giảm giá 10% (tối đa 150.000đ)";
                nextTierPerks = new List<string>
                {
                    "Nâng mức giảm trực tiếp lên 10% cho mọi đơn hàng (thời hạn 12 tháng)",
                    "Nâng cấp hệ số tích điểm lên x1.5 (đơn 100k nhận 150 điểm thưởng)",
                    "Miễn phí vận chuyển toàn quốc cho đơn từ 100.000đ",
                    "Quà tặng sinh nhật mẹ & bé đặc biệt trị giá 300.000đ",
                    "Ưu tiên hỗ trợ CSKH và thời gian đổi trả nâng lên 15 ngày"
                };
            }
            else
            {
                // Đồng
                tier = "Thành viên hạng Đồng";
                currentTierPerks = new List<string>
                {
                    "Chiết khấu giảm trực tiếp 3% trên mọi đơn hàng (thời hạn 12 tháng)",
                    "Tích điểm tiêu chuẩn: 1.000đ = 1 điểm (đơn từ 100k nhận 100 điểm, tương đương 10.000đ giảm giá khi hoàn tất đơn hàng)",
                    "Không giới hạn số điểm trừ trực tiếp vào đơn hàng kế tiếp (100 điểm = 10.000đ)",
                    "Nhận thông báo sớm các chương trình Flash Sale và siêu ưu đãi"
                };
                nextTierName = "Thành viên hạng Bạc";
                nextTierSpendThreshold = 2000000m;
                amountNeeded = Math.Max(0, nextTierSpendThreshold - user.TotalSpent);
                progressPercent = Math.Clamp((int)((user.TotalSpent / nextTierSpendThreshold) * 100), 0, 99);
                nextTierRewardBonus = "+50 điểm Baby Club + Voucher giảm giá 5% cho đơn hàng tiếp theo";
                nextTierPerks = new List<string>
                {
                    "Nâng mức giảm trực tiếp lên 8% cho mọi đơn hàng (thời hạn 12 tháng)",
                    "Tăng tỷ lệ tích lũy điểm thưởng x1.2 (đơn 100k nhận 120 điểm thưởng)",
                    "Miễn phí vận chuyển cho tất cả đơn hàng từ 200.000đ",
                    "Nhận quà tặng chúc mừng sinh nhật bé yêu mỗi năm",
                    "Đặc quyền đổi trả hàng hóa thuận tiện trong vòng 7 ngày"
                };
            }

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                TotalOrders = totalOrders,
                RewardPoints = user.RewardPoints,
                TotalSpent = user.TotalSpent,
                MemberTier = tier,
                TierExpiryDate = tierExpiry,
                IsLifetimeTier = isLifetime,
                CurrentDiscountPercent = currentDiscountPercent,
                PointTransactions = pointTransactions,
                NextTierName = nextTierName,
                NextTierSpendThreshold = nextTierSpendThreshold,
                AmountNeededForNextTier = amountNeeded,
                ProgressToNextTierPercent = progressPercent,
                NextTierRewardBonus = nextTierRewardBonus,
                NextTierPerks = nextTierPerks,
                CurrentTierPerks = currentTierPerks
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = null;

            if (int.TryParse(userIdStr, out int userId))
            {
                user = await _context.Users.FindAsync(userId);
            }
            else
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            }

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Gán lại thông tin chỉ đọc
            model.Username = user.Username;
            model.Role = user.Role;
            model.CreatedAt = user.CreatedAt;
            model.TotalOrders = await _context.Orders.CountAsync(o => o.UserId == user.Id);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra trùng Email với user khác
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != user.Id))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                return View(model);
            }

            // Đổi mật khẩu nếu người dùng có nhập mật khẩu mới
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại để xác nhận đổi mật khẩu.");
                    return View(model);
                }

                if (!PasswordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                    return View(model);
                }

                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            }

            // Cập nhật thông tin
            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

            await _context.SaveChangesAsync();

            // Cập nhật lại Claims Cookie để đồng bộ FullName hiển thị trên Header
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "BabyShopCookieAuth");
            await HttpContext.SignInAsync("BabyShopCookieAuth", new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ và địa chỉ nhận hàng mặc định thành công!";
            return RedirectToAction("Profile");
        }


        // GET: /Account/MyOrders
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!int.TryParse(userIdStr, out userId))
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                userId = u?.Id ?? 0;
            }

            if (userId > 0)
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                var orderIds = orders.Select(o => o.Id).ToList();
                var returns = await _context.ReturnRequests
                    .Where(r => orderIds.Contains(r.OrderId))
                    .ToListAsync();

                ViewBag.ReturnRequests = returns;

                return View(orders);
            }
            return RedirectToAction("Login");
        }

        // GET: /Account/MyVouchers (Kho Voucher Của Tôi)
        [Authorize]
        public async Task<IActionResult> MyVouchers(string? tab = "available")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!int.TryParse(userIdStr, out userId))
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                userId = u?.Id ?? 0;
            }

            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var now = DateTime.Now;

            // Tự động đồng bộ các mã Săn Giờ Vàng (IsFlashHunt) đang kích hoạt vào kho của người dùng nếu chưa có
            var activeFlashCoupons = await _context.Coupons
                .Where(c => c.IsFlashHunt && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate.Value > now))
                .ToListAsync();

            if (activeFlashCoupons.Any())
            {
                var existingCouponIds = await _context.UserCoupons
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CouponId)
                    .ToListAsync();

                var missingCoupons = activeFlashCoupons.Where(c => !existingCouponIds.Contains(c.Id)).ToList();
                if (missingCoupons.Any())
                {
                    foreach (var fc in missingCoupons)
                    {
                        _context.UserCoupons.Add(new UserCoupon
                        {
                            UserId = userId,
                            CouponId = fc.Id,
                            CollectedAt = now,
                            ExpiryDate = fc.ExpiryDate ?? now.AddDays(30),
                            IsUsed = false
                        });
                    }
                    await _context.SaveChangesAsync();
                }
            }

            var allUserCoupons = await _context.UserCoupons
                .Include(uc => uc.Coupon)
                .Include(uc => uc.Order)
                .Where(uc => uc.UserId == userId)
                .OrderByDescending(uc => uc.CollectedAt)
                .ToListAsync();

            int availableCount = allUserCoupons.Count(uc => !uc.IsUsed && uc.ExpiryDate >= now && (uc.Coupon != null && uc.Coupon.IsActive));
            int usedCount = allUserCoupons.Count(uc => uc.IsUsed);
            int expiredCount = allUserCoupons.Count(uc => !uc.IsUsed && (uc.ExpiryDate < now || (uc.Coupon != null && !uc.Coupon.IsActive)));

            ViewBag.ActiveTab = tab ?? "available";
            ViewBag.AvailableCount = availableCount;
            ViewBag.UsedCount = usedCount;
            ViewBag.ExpiredCount = expiredCount;

            // Danh sách mã Săn Giờ Vàng trong kho
            ViewBag.FlashHuntUserCoupons = allUserCoupons
                .Where(uc => uc.Coupon != null && uc.Coupon.IsFlashHunt)
                .OrderBy(uc => uc.Coupon!.StartDate)
                .ToList();

            // Danh sách gợi ý voucher chưa nhận từ cửa hàng (loại trừ mã Dành Cho Bạn và mã Săn Giờ Vàng đã đồng bộ)
            var collectedCouponIds = allUserCoupons.Select(uc => uc.CouponId).ToHashSet();
            ViewBag.ExploreCoupons = await _context.Coupons
                .Where(c => !c.IsForYou && !c.IsFlashHunt && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate >= now) && !collectedCouponIds.Contains(c.Id))
                .OrderBy(c => c.MinOrderAmount)
                .Take(4)
                .ToListAsync();

            // Kiểm tra trạng thái Hộp Quà 12H Dành Cho Bạn
            var currentUser = await _context.Users.FindAsync(userId);
            bool canClaimForYou = true;
            int forYouRemainingSeconds = 0;
            if (currentUser?.LastForYouVoucherAt != null)
            {
                var nextAvailable = currentUser.LastForYouVoucherAt.Value.AddHours(12);
                if (now < nextAvailable)
                {
                    canClaimForYou = false;
                    forYouRemainingSeconds = (int)Math.Max(0, (nextAvailable - now).TotalSeconds);
                }
            }
            ViewBag.CanClaimForYou = canClaimForYou;
            ViewBag.ForYouRemainingSeconds = forYouRemainingSeconds;
            ViewBag.CurrentForYouUserCoupon = allUserCoupons.FirstOrDefault(uc => uc.Coupon != null && uc.Coupon.IsForYou && !uc.IsUsed && uc.ExpiryDate > now && uc.Coupon.IsActive);

            return View(allUserCoupons);
        }

        // POST: /Account/ClaimVoucherCode (Nhập mã lưu vào kho từ form)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimVoucherCode(string couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["CouponError"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction("MyVouchers");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!int.TryParse(userIdStr, out userId))
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                userId = u?.Id ?? 0;
            }

            if (userId == 0) return RedirectToAction("Login");

            var clean = couponCode.Trim().ToUpper();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == clean);

            if (coupon == null)
            {
                TempData["CouponError"] = $"Mã giảm giá \"{clean}\" không tồn tại trong hệ thống.";
                return RedirectToAction("MyVouchers");
            }

            if (coupon.IsForYou && coupon.AssignedUserId.HasValue && coupon.AssignedUserId.Value != userId)
            {
                TempData["CouponError"] = $"Mã \"{coupon.Code}\" là đặc quyền Dành Cho Bạn của tài khoản khác.";
                return RedirectToAction("MyVouchers");
            }

            bool alreadyCollected = await _context.UserCoupons.AnyAsync(uc => uc.UserId == userId && uc.CouponId == coupon.Id);
            if (alreadyCollected)
            {
                TempData["CouponError"] = $"Bạn đã lưu mã \"{coupon.Code}\" vào Kho Voucher rồi. Mỗi tài khoản chỉ nhận được 1 lần!";
                return RedirectToAction("MyVouchers");
            }

            if (!coupon.IsActive)
            {
                TempData["CouponError"] = $"Mã \"{coupon.Code}\" hiện đang tạm ngưng áp dụng.";
                return RedirectToAction("MyVouchers");
            }

            var now = DateTime.Now;
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < now)
            {
                TempData["CouponError"] = $"Mã \"{coupon.Code}\" đã hết hạn sử dụng vào ngày {coupon.ExpiryDate.Value:dd/MM/yyyy}.";
                return RedirectToAction("MyVouchers");
            }

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                TempData["CouponError"] = $"Mã \"{coupon.Code}\" đã hết số lượt lưu trên toàn hệ thống.";
                return RedirectToAction("MyVouchers");
            }

            DateTime userExpiry = coupon.ExpiryDate ?? now.AddDays(30);
            var userCoupon = new UserCoupon
            {
                UserId = userId,
                CouponId = coupon.Id,
                CollectedAt = now,
                ExpiryDate = userExpiry,
                IsUsed = false
            };
            _context.UserCoupons.Add(userCoupon);
            await _context.SaveChangesAsync();

            TempData["CouponSuccess"] = $"Lưu mã \"{coupon.Code}\" vào Kho Voucher thành công! Hạn dùng đến {userExpiry:dd/MM/yyyy}.";
            return RedirectToAction("MyVouchers");
        }

        // POST: /Account/CollectCouponAjax (Nhận voucher qua AJAX từ Popup hoặc Modal)
        [HttpPost]
        public async Task<IActionResult> CollectCouponAjax([FromBody] CollectCouponAjaxModel? model, [FromForm] int? couponId, [FromForm] string? couponCode)
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập tài khoản để lưu voucher vào Kho Voucher của bạn nhé!", requiresLogin = true });
            }

            int targetCouponId = model?.CouponId ?? couponId ?? 0;
            string? targetCouponCode = model?.CouponCode ?? couponCode;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!int.TryParse(userIdStr, out userId))
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                userId = u?.Id ?? 0;
            }

            if (userId == 0)
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn, vui lòng tải lại trang hoặc đăng nhập lại.", requiresLogin = true });
            }

            Coupon? coupon = null;
            if (targetCouponId > 0)
            {
                coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == targetCouponId);
            }
            else if (!string.IsNullOrWhiteSpace(targetCouponCode))
            {
                var clean = targetCouponCode.Trim().ToUpper();
                coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code.ToUpper() == clean);
            }

            if (coupon == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            }

            if (coupon.IsForYou && coupon.AssignedUserId.HasValue && coupon.AssignedUserId.Value != userId)
            {
                return Json(new { success = false, message = "Mã voucher này là đặc quyền Dành Cho Bạn của tài khoản khác." });
            }

            bool alreadyCollected = await _context.UserCoupons.AnyAsync(uc => uc.UserId == userId && uc.CouponId == coupon.Id);
            if (alreadyCollected)
            {
                return Json(new { 
                    success = false, 
                    message = $"Bạn đã lưu mã \"{coupon.Code}\" vào Kho Voucher trước đó rồi. Mỗi tài khoản chỉ nhận được 1 lần!", 
                    alreadyCollected = true,
                    couponCode = coupon.Code,
                    couponId = coupon.Id
                });
            }

            if (!coupon.IsActive)
            {
                return Json(new { success = false, message = $"Mã \"{coupon.Code}\" hiện đang tạm ngưng áp dụng." });
            }

            var now = DateTime.Now;
            if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < now)
            {
                return Json(new { success = false, message = $"Mã \"{coupon.Code}\" đã hết hạn sử dụng vào ngày {coupon.ExpiryDate.Value:dd/MM/yyyy}." });
            }

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                return Json(new { success = false, message = $"Mã \"{coupon.Code}\" đã hết số lượt lưu trên hệ thống." });
            }

            DateTime userExpiry = coupon.ExpiryDate ?? now.AddDays(30);
            var userCoupon = new UserCoupon
            {
                UserId = userId,
                CouponId = coupon.Id,
                CollectedAt = now,
                ExpiryDate = userExpiry,
                IsUsed = false
            };

            _context.UserCoupons.Add(userCoupon);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"Đã lưu mã \"{coupon.Code}\" vào Kho Voucher của bạn! Thời hạn sử dụng đến {userExpiry:dd/MM/yyyy HH:mm}.",
                couponCode = coupon.Code,
                couponId = coupon.Id,
                expiryDate = userExpiry.ToString("dd/MM/yyyy")
            });
        }

        // GET: /Account/GetForYouStatus (Kiểm tra trạng thái hộp quà 12h và đồng hồ đếm ngược)
        [HttpGet]
        public async Task<IActionResult> GetForYouStatus()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return Json(new {
                    authenticated = false,
                    canClaim = false,
                    message = "Vui lòng đăng nhập để nhận ưu đãi 12H Dành Cho Bạn."
                });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!int.TryParse(userIdStr, out userId))
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                userId = u?.Id ?? 0;
            }

            if (userId == 0)
            {
                return Json(new { authenticated = false, canClaim = false });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { authenticated = false, canClaim = false });
            }

            var now = DateTime.Now;
            bool canClaim = true;
            int remainingSeconds = 0;

            if (user.LastForYouVoucherAt.HasValue)
            {
                var nextAvailable = user.LastForYouVoucherAt.Value.AddHours(12);
                if (now < nextAvailable)
                {
                    canClaim = false;
                    remainingSeconds = (int)Math.Max(0, (nextAvailable - now).TotalSeconds);
                }
            }

            var activeForYou = await _context.UserCoupons
                .Include(uc => uc.Coupon)
                .Where(uc => uc.UserId == userId && uc.Coupon != null && uc.Coupon.IsForYou && !uc.IsUsed && uc.ExpiryDate > now && uc.Coupon.IsActive)
                .OrderByDescending(uc => uc.CollectedAt)
                .Select(uc => new {
                    code = uc.Coupon!.Code,
                    title = uc.Coupon!.Title,
                    description = uc.Coupon!.Description,
                    discountType = uc.Coupon!.DiscountType.ToString(),
                    discountValue = uc.Coupon!.DiscountValue,
                    minOrderAmount = uc.Coupon!.MinOrderAmount,
                    maxDiscountAmount = uc.Coupon!.MaxDiscountAmount,
                    expiryDate = uc.ExpiryDate.ToString("HH:mm dd/MM/yyyy"),
                    remainingSeconds = (int)Math.Max(0, (uc.ExpiryDate - now).TotalSeconds)
                })
                .FirstOrDefaultAsync();

            return Json(new {
                authenticated = true,
                canClaim = canClaim,
                remainingSeconds = remainingSeconds,
                tier = user.MemberTier,
                currentVoucher = activeForYou
            });
        }

        // POST: /Account/ClaimForYouVoucher (Mở hộp quà 12h nhận voucher độc quyền duy nhất cho tài khoản)
        [HttpPost]
        public async Task<IActionResult> ClaimForYouVoucher()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return Json(new {
                    success = false,
                    requiresLogin = true,
                    message = "Vui lòng đăng nhập để mở Hộp Quà 12H Dành Cho Bạn!"
                });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0;
            if (!int.TryParse(userIdStr, out userId))
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                userId = u?.Id ?? 0;
            }

            if (userId == 0)
            {
                return Json(new { success = false, requiresLogin = true, message = "Phiên đăng nhập hết hạn." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản." });
            }

            var now = DateTime.Now;
            if (user.LastForYouVoucherAt.HasValue)
            {
                var nextAvailable = user.LastForYouVoucherAt.Value.AddHours(12);
                if (now < nextAvailable)
                {
                    int waitSeconds = (int)Math.Max(0, (nextAvailable - now).TotalSeconds);
                    var ts = TimeSpan.FromSeconds(waitSeconds);
                    return Json(new {
                        success = false,
                        canClaim = false,
                        remainingSeconds = waitSeconds,
                        message = $"Bạn vừa nhận ưu đãi 12H rồi. Vui lòng quay lại sau {ts:hh\\:mm\\:ss} để nhận lượt tiếp theo nhé!"
                    });
                }
            }

            // Sinh mã ngẫu nhiên độc quyền: FORYOU-{userId}-{random}
            string randSuffix = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
            string code = $"FORYOU-{user.Id}-{randSuffix}";

            string tier = user.MemberTier ?? "Thành viên hạng Đồng";
            var voucherPool = new List<ForYouVoucherPoolItem>
            {
                // Nhóm Phổ Thông (Common)
                new ForYouVoucherPoolItem
                {
                    Title = "Quà May Mắn 10% - Hộp Quà 12H",
                    Description = "Giảm 10% tối đa 40.000đ cho đơn từ 120.000đ. Quà tặng may mắn ngẫu nhiên mỗi 12H cho mẹ & bé!",
                    Type = DiscountType.Percentage,
                    Value = 10,
                    MinOrder = 120000,
                    MaxDiscount = 40000,
                    Rarity = "✨ Phổ Thông",
                    RarityColor = "bg-secondary text-white",
                    Weight = tier.Contains("Kim Cương") ? 5 : tier.Contains("Vàng") ? 15 : 30
                },
                new ForYouVoucherPoolItem
                {
                    Title = "Lộc May Mắn 20K - Hộp Quà 12H",
                    Description = "Giảm ngay 20.000đ cho đơn hàng từ 100.000đ. Quà tặng bất ngờ mở hộp may mắn 12H!",
                    Type = DiscountType.FixedAmount,
                    Value = 20000,
                    MinOrder = 100000,
                    MaxDiscount = null,
                    Rarity = "✨ Phổ Thông",
                    RarityColor = "bg-secondary text-white",
                    Weight = tier.Contains("Kim Cương") ? 5 : tier.Contains("Vàng") ? 15 : 30
                },

                // Nhóm May Mắn (Lucky)
                new ForYouVoucherPoolItem
                {
                    Title = "Ưu Đãi May Mắn 15% - Hộp Quà 12H",
                    Description = "Giảm 15% tối đa 60.000đ cho đơn từ 150.000đ. Chúc mẹ shopping nhiều đồ xinh xắn cho bé!",
                    Type = DiscountType.Percentage,
                    Value = 15,
                    MinOrder = 150000,
                    MaxDiscount = 60000,
                    Rarity = "🍀 May Mắn",
                    RarityColor = "bg-success text-white",
                    Weight = tier.Contains("Kim Cương") ? 15 : tier.Contains("Vàng") ? 25 : 30
                },
                new ForYouVoucherPoolItem
                {
                    Title = "Lộc Đầu Ngày 30K - Hộp Quà 12H",
                    Description = "Giảm ngay 30.000đ cho đơn từ 150.000đ. Quà tặng may mắn độc quyền riêng cho bạn!",
                    Type = DiscountType.FixedAmount,
                    Value = 30000,
                    MinOrder = 150000,
                    MaxDiscount = null,
                    Rarity = "🍀 May Mắn",
                    RarityColor = "bg-success text-white",
                    Weight = tier.Contains("Kim Cương") ? 15 : tier.Contains("Vàng") ? 25 : 30
                },

                // Nhóm Quà Hiếm (Rare)
                new ForYouVoucherPoolItem
                {
                    Title = "Quà Tặng Vàng 20% - Hộp Quà 12H",
                    Description = "Giảm 20% tối đa 90.000đ cho đơn từ 180.000đ. Chúc mừng mẹ mở trúng thẻ ưu đãi Hiếm!",
                    Type = DiscountType.Percentage,
                    Value = 20,
                    MinOrder = 180000,
                    MaxDiscount = 90000,
                    Rarity = "🔥 Quà Hiếm",
                    RarityColor = "bg-warning text-dark",
                    Weight = tier.Contains("Kim Cương") ? 25 : tier.Contains("Vàng") ? 25 : 15
                },
                new ForYouVoucherPoolItem
                {
                    Title = "Lộc Vàng 50K - Hộp Quà 12H",
                    Description = "Giảm ngay 50.000đ cho đơn từ 200.000đ. Chúc mừng mẹ bốc trúng bao lì xì Vàng!",
                    Type = DiscountType.FixedAmount,
                    Value = 50000,
                    MinOrder = 200000,
                    MaxDiscount = null,
                    Rarity = "🔥 Quà Hiếm",
                    RarityColor = "bg-warning text-dark",
                    Weight = tier.Contains("Kim Cương") ? 25 : tier.Contains("Vàng") ? 25 : 15
                },

                // Nhóm Cực Hiếm (Epic)
                new ForYouVoucherPoolItem
                {
                    Title = "Siêu Quà May Mắn 25% - Hộp Quà 12H",
                    Description = "Giảm 25% tối đa 120.000đ cho đơn từ 220.000đ. Quà tặng Cực Hiếm ngẫu nhiên từ Hộp Quà 12H!",
                    Type = DiscountType.Percentage,
                    Value = 25,
                    MinOrder = 220000,
                    MaxDiscount = 120000,
                    Rarity = "💎 CỰC HIẾM",
                    RarityColor = "bg-primary text-white",
                    Weight = tier.Contains("Kim Cương") ? 20 : tier.Contains("Vàng") ? 15 : 8
                },
                new ForYouVoucherPoolItem
                {
                    Title = "Bao Lì Xì VIP 70K - Hộp Quà 12H",
                    Description = "Giảm ngay 70.000đ cho đơn từ 250.000đ. Ưu đãi Cực Hiếm chỉ có tại Hộp Quà May Mắn 12H!",
                    Type = DiscountType.FixedAmount,
                    Value = 70000,
                    MinOrder = 250000,
                    MaxDiscount = null,
                    Rarity = "💎 CỰC HIẾM",
                    RarityColor = "bg-primary text-white",
                    Weight = tier.Contains("Kim Cương") ? 20 : tier.Contains("Vàng") ? 15 : 8
                },

                // Nhóm JACKPOT Thần Tài (Legendary)
                new ForYouVoucherPoolItem
                {
                    Title = "🌟 JACKPOT THẦN TÀI 35% - HỘP QUÀ 12H",
                    Description = "CHÚC MỪNG MẸ TRÚNG GIẢI ĐẶC BIỆT: Giảm 35% tối đa 150.000đ cho đơn từ 250.000đ!",
                    Type = DiscountType.Percentage,
                    Value = 35,
                    MinOrder = 250000,
                    MaxDiscount = 150000,
                    Rarity = "🌟 JACKPOT 35%",
                    RarityColor = "bg-danger text-white border border-warning",
                    Weight = tier.Contains("Kim Cương") ? 15 : tier.Contains("Vàng") ? 10 : 4
                },
                new ForYouVoucherPoolItem
                {
                    Title = "🌟 JACKPOT VÀNG 100K - HỘP QUÀ 12H",
                    Description = "CHÚC MỪNG MẸ TRÚNG GIẢI ĐẶC BIỆT: Giảm ngay 100.000đ cho đơn từ 300.000đ!",
                    Type = DiscountType.FixedAmount,
                    Value = 100000,
                    MinOrder = 300000,
                    MaxDiscount = null,
                    Rarity = "🌟 JACKPOT 100K",
                    RarityColor = "bg-danger text-white border border-warning",
                    Weight = tier.Contains("Kim Cương") ? 15 : tier.Contains("Vàng") ? 10 : 4
                }
            };

            int totalWeight = voucherPool.Sum(v => v.Weight);
            int randVal = new Random().Next(0, totalWeight);
            int accum = 0;
            var chosen = voucherPool[0];
            foreach (var item in voucherPool)
            {
                accum += item.Weight;
                if (randVal < accum)
                {
                    chosen = item;
                    break;
                }
            }

            var expiry = now.AddHours(12);

            var coupon = new Coupon
            {
                Code = code,
                Title = chosen.Title,
                Description = chosen.Description,
                DiscountType = chosen.Type,
                DiscountValue = chosen.Value,
                MinOrderAmount = chosen.MinOrder,
                MaxDiscountAmount = chosen.MaxDiscount,
                UsageLimit = 1,
                UsedCount = 0,
                StartDate = now,
                ExpiryDate = expiry,
                IsActive = true,
                IsForYou = true,
                AssignedUserId = user.Id
            };
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            var userCoupon = new UserCoupon
            {
                UserId = user.Id,
                CouponId = coupon.Id,
                CollectedAt = now,
                ExpiryDate = expiry,
                IsUsed = false
            };
            _context.UserCoupons.Add(userCoupon);

            user.LastForYouVoucherAt = now;
            await _context.SaveChangesAsync();

            string displayDiscount = chosen.Type == DiscountType.Percentage
                ? $"Giảm {chosen.Value:0.#}%"
                : $"Giảm {chosen.Value:N0}đ";

            return Json(new {
                success = true,
                message = $"Mở Hộp Quà 12H thành công! Bạn đã bốc trúng: {chosen.Title} ({chosen.Rarity}).",
                coupon = new {
                    code = coupon.Code,
                    title = coupon.Title,
                    description = coupon.Description,
                    discountValue = chosen.Value,
                    discountType = chosen.Type.ToString(),
                    displayDiscount = displayDiscount,
                    maxDiscount = chosen.MaxDiscount,
                    minOrder = chosen.MinOrder,
                    rarity = chosen.Rarity,
                    rarityColor = chosen.RarityColor,
                    expiryDate = expiry.ToString("HH:mm dd/MM/yyyy"),
                    remainingSeconds = 12 * 3600
                }
            });
        }

        // GET: /Account/GetFlashHuntCoupons (Lấy danh sách mã Săn Giờ Vàng và thời gian đếm ngược trực tiếp)
        [HttpGet]
        public async Task<IActionResult> GetFlashHuntCoupons()
        {
            var now = DateTime.Now;
            var coupons = await _context.Coupons
                .Where(c => c.IsFlashHunt && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate.Value > now))
                .OrderBy(c => c.StartDate)
                .ToListAsync();

            int userId = 0;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out userId))
                {
                    var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                    var u = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);
                    userId = u?.Id ?? 0;
                }
            }

            var userUsedCouponIds = new HashSet<int>();
            if (userId > 0)
            {
                userUsedCouponIds = (await _context.UserCoupons
                    .Where(uc => uc.UserId == userId && uc.IsUsed)
                    .Select(uc => uc.CouponId)
                    .ToListAsync()).ToHashSet();
            }

            var result = coupons.Select(c =>
            {
                bool isOpen = now >= c.StartDate;
                double remainingSeconds = isOpen ? 0 : Math.Max(0, (c.StartDate - now).TotalSeconds);
                bool isUsed = userUsedCouponIds.Contains(c.Id);

                return new
                {
                    id = c.Id,
                    code = c.Code,
                    title = c.Title,
                    description = c.Description,
                    discountType = (int)c.DiscountType,
                    discountValue = c.DiscountValue,
                    minOrderAmount = c.MinOrderAmount,
                    maxDiscountAmount = c.MaxDiscountAmount,
                    startDate = c.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    expiryDate = c.ExpiryDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    isOpen = isOpen,
                    remainingSeconds = (int)remainingSeconds,
                    isUsed = isUsed
                };
            });

            return Json(new { success = true, coupons = result, serverTime = now.ToString("yyyy-MM-ddTHH:mm:ss") });
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class CollectCouponAjaxModel
    {
        public int CouponId { get; set; }
        public string? CouponCode { get; set; }
    }

    public class ForYouVoucherPoolItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DiscountType Type { get; set; }
        public decimal Value { get; set; }
        public decimal MinOrder { get; set; }
        public decimal? MaxDiscount { get; set; }
        public string Rarity { get; set; } = "";
        public string RarityColor { get; set; } = "";
        public int Weight { get; set; }
    }
}
