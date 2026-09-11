using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class Order
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã đơn hàng")]
        public string OrderCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [StringLength(100)]
        [Display(Name = "Họ tên người nhận")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email nhận xác nhận đơn hàng")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? CustomerEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(255)]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Ghi chú đơn hàng")]
        public string? Note { get; set; }

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Tổng tiền hàng")]
        public decimal SubTotal { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã giảm giá")]
        public string? CouponCode { get; set; }

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Số tiền giảm giá")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Chiết khấu hạng thành viên")]
        public decimal MemberDiscount { get; set; } = 0;

        [Display(Name = "Điểm thưởng sử dụng")]
        public int PointsUsed { get; set; } = 0;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giảm giá từ điểm")]
        public decimal PointDiscount { get; set; } = 0;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Phí giao hàng")]
        public decimal ShippingFee { get; set; } = 30000; // Mặc định 30.000 VNĐ

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Tổng thanh toán")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD"; // "COD", "BankTransfer"

        [Display(Name = "Trạng thái thanh toán")]
        public bool IsPaid { get; set; } = false;

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái đơn hàng")]
        public string OrderStatus { get; set; } = "Pending"; // "Pending" (Chờ duyệt), "Processing" (Đang chuẩn bị hàng), "Shipping" (Đang giao), "Completed" (Hoàn thành), "Cancelled" (Đã hủy)

        [Display(Name = "Ngày đặt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
