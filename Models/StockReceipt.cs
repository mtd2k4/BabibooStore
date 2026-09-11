using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class StockReceipt
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã phiếu nhập")]
        public string ReceiptCode { get; set; } = string.Empty; // e.g. PN-20260911-1001

        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp")]
        [StringLength(200)]
        [Display(Name = "Nhà cung cấp")]
        public string SupplierName { get; set; } = string.Empty; // e.g. Xưởng May Cotton Kids Hà Nội

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? SupplierPhone { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ NCC")]
        public string? SupplierAddress { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú nhập hàng")]
        public string? Note { get; set; }

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Tổng tiền nhập")]
        public decimal TotalAmount { get; set; } = 0;

        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Completed"; // "Completed", "Pending", "Cancelled"

        [StringLength(100)]
        [Display(Name = "Người lập phiếu")]
        public string? CreatedBy { get; set; } = "Admin";

        [Display(Name = "Ngày nhập kho")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<StockReceiptDetail> ReceiptDetails { get; set; } = new List<StockReceiptDetail>();
    }

    public class StockReceiptDetail
    {
        public int Id { get; set; }

        public int StockReceiptId { get; set; }
        public virtual StockReceipt? StockReceipt { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int? ProductVariantId { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Kích cỡ")]
        public string? Size { get; set; }

        [StringLength(50)]
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }

        [Display(Name = "Số lượng nhập")]
        [Range(1, 100000, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giá nhập (VNĐ)")]
        [Range(0, 100000000)]
        public decimal ImportPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Thành tiền")]
        public decimal TotalPrice { get; set; } = 0;
    }
}
