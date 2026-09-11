using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public enum DiscountType
    {
        Percentage,   // Giảm theo phần trăm (VD: 10%, 15%, 20%)
        FixedAmount   // Giảm số tiền cố định (VD: 30.000đ, 50.000đ, 100.000đ)
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã giảm giá")]
        [StringLength(50)]
        [Display(Name = "Mã voucher")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề khuyến mãi")]
        [StringLength(200)]
        [Display(Name = "Tên chương trình")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [Display(Name = "Hình thức giảm")]
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; } // Nếu Percentage: 10 (10%); Nếu FixedAmount: 50000 (50.000đ)

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Đơn hàng tối thiểu")]
        public decimal MinOrderAmount { get; set; } = 0; // Giá trị đơn hàng tối thiểu để áp dụng

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Mức giảm tối đa")]
        public decimal? MaxDiscountAmount { get; set; } // Mức trần giảm giá (đặc biệt hữu ích cho mã %)

        [Display(Name = "Số lượt dùng tối đa")]
        public int? UsageLimit { get; set; } = 1000;

        [Display(Name = "Số lượt đã dùng")]
        public int UsedCount { get; set; } = 0;

        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày hết hạn")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Dành riêng cho bạn")]
        public bool IsForYou { get; set; } = false;

        [Display(Name = "Săn Mã Giờ Vàng (Mở theo giờ)")]
        public bool IsFlashHunt { get; set; } = false;

        [Display(Name = "Tài khoản sở hữu độc quyền")]
        public int? AssignedUserId { get; set; }

        [ForeignKey("AssignedUserId")]
        public virtual User? AssignedUser { get; set; }

        public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
    }
}
