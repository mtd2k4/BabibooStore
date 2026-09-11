using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class RewardsController : Controller
    {
        private readonly BabyShopDbContext _context;

        public RewardsController(BabyShopDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            if (!(User.Identity?.IsAuthenticated ?? false)) return null;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int uid))
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Id == uid && u.IsActive);
            }

            var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            }

            return null;
        }

        // GET: /Rewards
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();

            bool hasCheckedInToday = false;
            int currentStreak = 0;
            int availableSpins = 0;
            int rewardPoints = 0;
            List<DailyCheckIn> recentCheckIns = new();
            List<LuckyWheelHistory> userHistory = new();

            if (user != null)
            {
                rewardPoints = user.RewardPoints;
                availableSpins = user.LuckyWheelSpinsAvailable;
                currentStreak = user.DailyStreakCount;

                // Kiểm tra chính xác theo tài khoản: đã điểm danh hôm nay chưa
                if (user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == DateTime.Today)
                {
                    hasCheckedInToday = true;
                }

                if (!hasCheckedInToday)
                {
                    hasCheckedInToday = await _context.DailyCheckIns
                        .AnyAsync(c => c.UserId == user.Id && c.CheckInDate.Date == DateTime.Today);
                }

                if (hasCheckedInToday)
                {
                    currentStreak = user.DailyStreakCount;
                }
                else
                {
                    if (user.LastCheckInDate.HasValue)
                    {
                        if (user.LastCheckInDate.Value.Date == DateTime.Today.AddDays(-1))
                        {
                            // Điểm danh hôm qua, hôm nay chưa điểm danh
                            currentStreak = user.DailyStreakCount;
                        }
                        else if (user.LastCheckInDate.Value.Date < DateTime.Today.AddDays(-1))
                        {
                            // Chuỗi bị ngắt quãng do bỏ lỡ điểm danh từ hôm qua -> Lập tức reset về Ngày 1
                            if (user.DailyStreakCount > 0)
                            {
                                user.DailyStreakCount = 0;
                                await _context.SaveChangesAsync();
                                ViewBag.StreakWasReset = true;
                            }
                            currentStreak = 0;
                        }
                    }
                    else
                    {
                        currentStreak = 0;
                        if (user.DailyStreakCount > 0)
                        {
                            user.DailyStreakCount = 0;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                recentCheckIns = await _context.DailyCheckIns
                    .Where(c => c.UserId == user.Id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(7)
                    .ToListAsync();

                userHistory = await _context.LuckyWheelHistories
                    .Where(h => h.UserId == user.Id)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }

            // Danh sách các lượt trúng thưởng mẫu/gần đây trên toàn hệ thống
            var recentWinners = await _context.LuckyWheelHistories
                .Include(h => h.User)
                .OrderByDescending(h => h.CreatedAt)
                .Take(12)
                .Select(h => new LuckyWinnerDto {
                    UserName = h.User != null ? MaskName(h.User.FullName) : "Mẹ bỉm BabyShop",
                    PrizeName = h.PrizeName,
                    TimeAgo = FormatTimeAgo(h.CreatedAt)
                })
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.HasCheckedInToday = hasCheckedInToday;
            ViewBag.CurrentStreak = currentStreak;
            ViewBag.AvailableSpins = availableSpins;
            ViewBag.RewardPoints = rewardPoints;
            ViewBag.RecentCheckIns = recentCheckIns;
            ViewBag.UserHistory = userHistory;
            ViewBag.RecentWinners = recentWinners;

            return View();
        }

        // POST: /Rewards/CheckIn (AJAX)
        [HttpPost]
        public async Task<IActionResult> CheckIn()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Json(new { success = false, requireLogin = true, message = "Mẹ vui lòng đăng nhập để tham gia điểm danh nhé!" });
            }

            // Kiểm tra xem tài khoản này hôm nay đã điểm danh hay chưa
            bool alreadyCheckedIn = (user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == DateTime.Today)
                || await _context.DailyCheckIns.AnyAsync(c => c.UserId == user.Id && c.CheckInDate.Date == DateTime.Today);

            if (alreadyCheckedIn)
            {
                return Json(new { success = false, message = "Hôm nay mẹ đã điểm danh rồi. Hẹn gặp lại mẹ vào ngày mai nha ✨" });
            }

            int newStreak = 1;
            bool wasReset = false;
            if (user.LastCheckInDate.HasValue)
            {
                if (user.LastCheckInDate.Value.Date == DateTime.Today.AddDays(-1))
                {
                    // Điểm danh liên tiếp đúng ngày hôm qua -> tăng streak
                    newStreak = (user.DailyStreakCount % 7) + 1;
                }
                else if (user.LastCheckInDate.Value.Date < DateTime.Today.AddDays(-1))
                {
                    // Bỏ lỡ điểm danh từ hôm qua -> Lập tức reset về Ngày 1
                    newStreak = 1;
                    wasReset = true;
                }
            }

            // Phần thưởng điểm danh theo ngày
            int pointsAwarded = newStreak switch
            {
                1 => 10,
                2 => 15,
                3 => 20,
                4 => 25,
                5 => 30,
                6 => 40,
                7 => 50,
                _ => 10
            };

            int spinsAwarded = (newStreak == 7) ? 1 : 0;

            user.RewardPoints += pointsAwarded;
            user.DailyStreakCount = newStreak;
            user.LastCheckInDate = DateTime.Today;
            user.LuckyWheelSpinsAvailable += spinsAwarded;

            // Ghi nhật ký điểm danh
            _context.DailyCheckIns.Add(new DailyCheckIn
            {
                UserId = user.Id,
                CheckInDate = DateTime.Today,
                StreakDay = newStreak,
                PointsAwarded = pointsAwarded,
                SpinsAwarded = spinsAwarded,
                CreatedAt = DateTime.Now
            });

            // Ghi biến động điểm
            _context.PointTransactions.Add(new PointTransaction
            {
                UserId = user.Id,
                PointsChanged = pointsAwarded,
                Type = "CheckIn",
                Description = $"Điểm danh hàng ngày (Ngày {newStreak}/7)",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            string spinMsg = spinsAwarded > 0 ? " và TẶNG 1 LƯỢT QUAY MAY MẮN 🎡" : "";
            string msg;
            if (wasReset)
            {
                msg = $"⚠️ Mẹ đã bỏ lỡ điểm danh ngày hôm qua nên chuỗi đã lập tức reset về Ngày 1! Mẹ được cộng +{pointsAwarded} Xu hôm nay. Mẹ nhớ vào điểm danh mỗi ngày để duy trì chuỗi nhé!";
            }
            else
            {
                msg = $"Điểm danh Ngày {newStreak}/7 thành công! Mẹ được cộng +{pointsAwarded} Xu Baby Club{spinMsg}!";
            }

            return Json(new
            {
                success = true,
                wasReset = wasReset,
                streak = newStreak,
                pointsAwarded = pointsAwarded,
                spinsAwarded = spinsAwarded,
                newPoints = user.RewardPoints,
                newSpins = user.LuckyWheelSpinsAvailable,
                message = msg
            });
        }

        // POST: /Rewards/SpinWheel (AJAX)
        [HttpPost]
        public async Task<IActionResult> SpinWheel()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Json(new { success = false, requireLogin = true, message = "Mẹ vui lòng đăng nhập để quay thưởng nhé!" });
            }

            if (user.LuckyWheelSpinsAvailable <= 0)
            {
                return Json(new { success = false, noSpins = true, message = "Mẹ đã hết lượt quay rồi! Mẹ có thể điểm danh đều đặn hoặc dùng 30 Xu đổi lượt quay nhé." });
            }

            // Trừ 1 lượt quay
            user.LuckyWheelSpinsAvailable--;

            // Định nghĩa 8 ô phần thưởng và trọng số xác suất (Tổng = 100)
            var prizes = new[]
            {
                new { Index = 0, Name = "+15 Xu Baby Club", Type = "Points", Points = 15, CouponCode = "", Weight = 25 },
                new { Index = 1, Name = "Voucher 20.000đ", Type = "Coupon", Points = 0, CouponCode = "LUCKY20K", Weight = 15 },
                new { Index = 2, Name = "+30 Xu Baby Club", Type = "Points", Points = 30, CouponCode = "", Weight = 20 },
                new { Index = 3, Name = "Freeship Vòng Quay 30K", Type = "Coupon", Points = 0, CouponCode = "LUCKYSHIP", Weight = 15 },
                new { Index = 4, Name = "+50 Xu Baby Club", Type = "Points", Points = 50, CouponCode = "", Weight = 10 },
                new { Index = 5, Name = "Chúc Mẹ May Mắn (+5 Xu)", Type = "Luck", Points = 5, CouponCode = "", Weight = 5 },
                new { Index = 6, Name = "Voucher VIP 50.000đ", Type = "Coupon", Points = 0, CouponCode = "LUCKY50K", Weight = 5 },
                new { Index = 7, Name = "+1 Lượt Quay Thưởng", Type = "Spin", Points = 0, CouponCode = "", Weight = 5 }
            };

            // Quay ngẫu nhiên an toàn phía Server
            int totalWeight = prizes.Sum(p => p.Weight);
            int roll = Random.Shared.Next(0, totalWeight);
            int currentSum = 0;
            var selectedPrize = prizes[0];

            foreach (var p in prizes)
            {
                currentSum += p.Weight;
                if (roll < currentSum)
                {
                    selectedPrize = p;
                    break;
                }
            }

            int? wonCouponId = null;
            string wonCouponCode = "";

            if (selectedPrize.Type == "Points" || selectedPrize.Type == "Luck")
            {
                user.RewardPoints += selectedPrize.Points;
                _context.PointTransactions.Add(new PointTransaction
                {
                    UserId = user.Id,
                    PointsChanged = selectedPrize.Points,
                    Type = "LuckyWheel",
                    Description = $"Trúng thưởng Vòng Quay May Mắn ({selectedPrize.Name})",
                    CreatedAt = DateTime.Now
                });
            }
            else if (selectedPrize.Type == "Spin")
            {
                user.LuckyWheelSpinsAvailable += 1;
            }
            else if (selectedPrize.Type == "Coupon")
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == selectedPrize.CouponCode && c.IsActive);
                if (coupon != null)
                {
                    wonCouponId = coupon.Id;
                    wonCouponCode = coupon.Code;

                    // Nạp thẳng vào kho voucher của mẹ
                    _context.UserCoupons.Add(new UserCoupon
                    {
                        UserId = user.Id,
                        CouponId = coupon.Id,
                        CollectedAt = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(14),
                        IsUsed = false
                    });
                }
            }

            // Ghi nhật ký trúng thưởng
            _context.LuckyWheelHistories.Add(new LuckyWheelHistory
            {
                UserId = user.Id,
                PrizeName = selectedPrize.Name,
                PrizeType = selectedPrize.Type,
                PointsWon = selectedPrize.Points,
                CouponId = wonCouponId,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                prizeIndex = selectedPrize.Index,
                prizeName = selectedPrize.Name,
                prizeType = selectedPrize.Type,
                pointsWon = selectedPrize.Points,
                couponCode = wonCouponCode,
                newSpins = user.LuckyWheelSpinsAvailable,
                newPoints = user.RewardPoints,
                message = $"Chúc mừng mẹ đã quay trúng: {selectedPrize.Name}! 🎉"
            });
        }

        // POST: /Rewards/BuySpinWithPoints (AJAX)
        [HttpPost]
        public async Task<IActionResult> BuySpinWithPoints()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Json(new { success = false, requireLogin = true, message = "Mẹ vui lòng đăng nhập để đổi lượt quay nhé!" });
            }

            const int cost = 30;
            if (user.RewardPoints < cost)
            {
                return Json(new { success = false, message = $"Mẹ cần có ít nhất {cost} Xu Baby Club để đổi 1 lượt quay. Hiện mẹ đang có {user.RewardPoints} Xu." });
            }

            user.RewardPoints -= cost;
            user.LuckyWheelSpinsAvailable += 1;

            _context.PointTransactions.Add(new PointTransaction
            {
                UserId = user.Id,
                PointsChanged = -cost,
                Type = "BuySpin",
                Description = "Đổi 30 Xu lấy 1 Lượt Quay May Mắn",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newSpins = user.LuckyWheelSpinsAvailable,
                newPoints = user.RewardPoints,
                message = "Đổi 30 Xu lấy 1 lượt quay may mắn thành công! Chúc mẹ quay trúng quà to nha ✨"
            });
        }

        private static string MaskName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "Mẹ bỉm BabyShop";
            var parts = fullName.Trim().Split(' ');
            if (parts.Length == 1) return parts[0];
            return parts[0] + " " + parts[^1].Substring(0, 1) + "***";
        }

        private static string FormatTimeAgo(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalMinutes < 1) return "vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            return $"{(int)span.TotalDays} ngày trước";
        }
    }

    public class LuckyWinnerDto
    {
        public string UserName { get; set; } = string.Empty;
        public string PrizeName { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
    }
}
