using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int? ProductVariantId { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string? ProductImage { get; set; }

        [StringLength(50)]
        [Display(Name = "Size")]
        public string? Size { get; set; }

        [StringLength(50)]
        [Display(Name = "Màu sắc")]
        public string? Color { get; set; }

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Đơn giá")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Số lượng")]
        public int Quantity { get; set; } = 1;

        [NotMapped]
        [Display(Name = "Thành tiền")]
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
