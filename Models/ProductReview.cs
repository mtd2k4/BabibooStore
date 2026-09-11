using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        [StringLength(100, ErrorMessage = "Tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? CustomerPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Vui lòng viết nhận xét về sản phẩm")]
        [StringLength(1000, ErrorMessage = "Nội dung nhận xét tối đa 1000 ký tự")]
        [Display(Name = "Nội dung nhận xét")]
        public string Comment { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Thông tin bé (Cân nặng / Tuổi)")]
        public string? BabyInfo { get; set; } // Ví dụ: "Bé gái 18 tháng, 11kg"

        [Display(Name = "Đã mua hàng")]
        public bool IsVerifiedPurchase { get; set; } = true;

        [Display(Name = "Duyệt hiển thị")]
        public bool IsApproved { get; set; } = true;

        [Display(Name = "Lượt thấy hữu ích")]
        public int HelpfulCount { get; set; } = 0;

        [Display(Name = "Thời gian gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
