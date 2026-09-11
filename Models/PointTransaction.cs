using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class PointTransaction
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        [Display(Name = "Số điểm biến động")]
        public int PointsChanged { get; set; } // Dương nếu tích điểm (+), Âm nếu tiêu điểm (-)

        [Required]
        [StringLength(20)]
        [Display(Name = "Loại giao dịch")]
        public string Type { get; set; } = "Earn"; // "Earn" (Tích điểm từ đơn), "Spend" (Tiêu điểm), "Bonus" (Thưởng sinh nhật/thành viên)

        [Required]
        [StringLength(255)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Thời gian")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
