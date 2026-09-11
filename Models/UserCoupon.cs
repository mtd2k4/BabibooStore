using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class UserCoupon
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CouponId { get; set; }

        [Display(Name = "Thời điểm nhận")]
        public DateTime CollectedAt { get; set; } = DateTime.Now;

        [Display(Name = "Thời hạn sử dụng")]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Đã sử dụng")]
        public bool IsUsed { get; set; } = false;

        [Display(Name = "Thời điểm sử dụng")]
        public DateTime? UsedAt { get; set; }

        public int? OrderId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}
