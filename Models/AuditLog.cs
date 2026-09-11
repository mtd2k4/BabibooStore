using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Hành động")]
        public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete", "Restock", "ChangeStatus", "LockUser", "Coupon"

        [Required]
        [StringLength(100)]
        [Display(Name = "Đối tượng")]
        public string EntityName { get; set; } = string.Empty; // "Product", "Order", "StockReceipt", "User", "Coupon"

        [Display(Name = "ID Đối tượng")]
        public int? EntityId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Mô tả chi tiết")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Dữ liệu trước thay đổi")]
        public string? OldValues { get; set; }

        [Display(Name = "Dữ liệu sau thay đổi")]
        public string? NewValues { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        [Display(Name = "Người thực hiện")]
        public string UserName { get; set; } = "Hệ thống";

        [StringLength(50)]
        [Display(Name = "Địa chỉ IP")]
        public string? IpAddress { get; set; }

        [Display(Name = "Thời gian thực hiện")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
