using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Size")]
        [StringLength(50)]
        [Display(Name = "Kích thước (Size)")]
        public string Size { get; set; } = string.Empty; // e.g.: "0-3M", "3-6M", "1-2Y", "Size 90", "Size 100", "Size 110"

        [Required(ErrorMessage = "Vui lòng nhập màu sắc")]
        [StringLength(50)]
        [Display(Name = "Màu sắc")]
        public string Color { get; set; } = string.Empty; // e.g.: "Hồng Pastel", "Xanh Baby", "Trắng Tinh Khôi", "Vàng Chanh"

        [StringLength(20)]
        [Display(Name = "Mã màu Hex")]
        public string? ColorHex { get; set; } // e.g.: "#FFD1DC", "#AEEEEE", "#FFFFFF"

        [Display(Name = "Số lượng tồn kho")]
        [Range(0, 10000, ErrorMessage = "Số lượng tồn kho phải >= 0")]
        public int StockQuantity { get; set; } = 10;

        [StringLength(50)]
        [Display(Name = "Mã biến thể SKU")]
        public string? Sku { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh theo màu/biến thể")]
        public string? ImageUrl { get; set; }
    }
}
