using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class DailyCheckIn
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Display(Name = "Ngày điểm danh")]
        public DateTime CheckInDate { get; set; }

        [Display(Name = "Ngày thứ trong chuỗi")]
        public int StreakDay { get; set; } = 1;

        [Display(Name = "Điểm thưởng nhận được")]
        public int PointsAwarded { get; set; } = 10;

        [Display(Name = "Lượt quay được tặng")]
        public int SpinsAwarded { get; set; } = 0;

        [Display(Name = "Thời gian tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
