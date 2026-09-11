using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class LuckyWheelHistory
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên phần thưởng")]
        public string PrizeName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Display(Name = "Loại phần thưởng")]
        public string PrizeType { get; set; } = "Points"; // "Points", "Coupon", "Spin", "Luck"

        [Display(Name = "Điểm nhận được")]
        public int PointsWon { get; set; } = 0;

        public int? CouponId { get; set; }

        [Display(Name = "Thời gian quay")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CouponId")]
        public virtual Coupon? Coupon { get; set; }
    }
}
