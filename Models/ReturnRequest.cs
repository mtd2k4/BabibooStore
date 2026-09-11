using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class ReturnRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Mã yêu cầu đổi/trả")]
        public string ReturnCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Đơn hàng")]
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        [Display(Name = "Người dùng")]
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên người liên hệ")]
        [StringLength(100)]
        [Display(Name = "Họ tên người liên hệ")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Email")]
        public string? CustomerEmail { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Hình thức yêu cầu")]
        public string RequestType { get; set; } = "Exchange"; // "Exchange" (Đổi size/màu), "Refund" (Trả hàng & Hoàn tiền)

        [Required]
        [StringLength(50)]
        [Display(Name = "Nguồn gốc lỗi")]
        public string FaultSource { get; set; } = "ShopFault"; // "ShopFault" (Lỗi từ Shop), "CarrierFault" (Lỗi do Vận chuyển), "CustomerRequest" (Nhu cầu đổi size)

        [Required(ErrorMessage = "Vui lòng chọn lý do đổi trả")]
        [StringLength(150)]
        [Display(Name = "Nhóm lý do")]
        public string ReasonCategory { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Mô tả chi tiết")]
        public string? ReasonDetail { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm cần đổi/trả")]
        [StringLength(200)]
        [Display(Name = "Sản phẩm đổi/trả")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Phân loại hiện tại (Size/Màu)")]
        public string? CurrentSizeColor { get; set; }

        [StringLength(100)]
        [Display(Name = "Size / Màu muốn đổi sang")]
        public string? DesiredSizeColor { get; set; }

        [StringLength(2000)]
        [Display(Name = "Danh sách ảnh bằng chứng")]
        public string? ProofImages { get; set; } // Phân tách bằng dấu chấm phẩy ;

        [StringLength(500)]
        [Display(Name = "Đường dẫn video bóc hàng (Unboxing Video tối đa 100MB)")]
        public string? UnboxingVideoPath { get; set; }

        [StringLength(500)]
        [Display(Name = "Link video mở gói hàng (Youtube, Drive...)")]
        public string? UnboxingVideoUrl { get; set; }

        [Display(Name = "Đã đồng kiểm cùng shipper lúc nhận")]
        public bool HadCarrierCoCheck { get; set; } = false;

        [StringLength(100)]
        [Display(Name = "Đơn vị vận chuyển giao hàng")]
        public string? CarrierName { get; set; }

        [StringLength(100)]
        [Display(Name = "Mã vận đơn bưu cục")]
        public string? CarrierTrackingCode { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên ngân hàng hoàn tiền")]
        public string? BankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Số tài khoản nhận tiền")]
        public string? BankAccountNo { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên chủ tài khoản nhận")]
        public string? BankAccountName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái xử lý")]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected", "Completed"

        [StringLength(1000)]
        [Display(Name = "Ghi chú từ Shop / Lời nhắn cho mẹ")]
        public string? AdminNote { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ lấy hàng / đổi hàng")]
        public string? PickupAddress { get; set; }

        [Display(Name = "Ngày gửi yêu cầu")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày xử lý")]
        public DateTime? ProcessedAt { get; set; }
    }
}
