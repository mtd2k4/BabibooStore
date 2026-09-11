using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản")]
        [StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(255)]
        [Display(Name = "Mật khẩu")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Customer"; // "Admin" hoặc "Customer"

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Điểm tích lũy Baby Club")]
        public int RewardPoints { get; set; } = 0;

        [Display(Name = "Tổng tiền đã chi tiêu")]
        public decimal TotalSpent { get; set; } = 0;

        [StringLength(50)]
        [Display(Name = "Hạng thành viên")]
        public string MemberTier { get; set; } = "Thành viên hạng Đồng"; // "Thành viên hạng Đồng", "Thành viên hạng Bạc", "Thành viên hạng Vàng", "Thành viên hạng Kim Cương"

        [Display(Name = "Thời hạn duy trì hạng")]
        public DateTime? TierExpiryDate { get; set; }

        [Display(Name = "Thời gian nhận voucher Dành Cho Bạn gần nhất")]
        public DateTime? LastForYouVoucherAt { get; set; }

        [Display(Name = "Số ngày điểm danh liên tiếp")]
        public int DailyStreakCount { get; set; } = 0;

        [Display(Name = "Ngày điểm danh gần nhất")]
        public DateTime? LastCheckInDate { get; set; }

        [Display(Name = "Lượt quay may mắn khả dụng")]
        public int LuckyWheelSpinsAvailable { get; set; } = 1;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
        public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
        public virtual ICollection<DailyCheckIn> DailyCheckIns { get; set; } = new List<DailyCheckIn>();
        public virtual ICollection<LuckyWheelHistory> LuckyWheelHistories { get; set; } = new List<LuckyWheelHistory>();
    }
}
