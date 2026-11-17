using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Slug { get; set; }

        [StringLength(50)]
        [Display(Name = "Mã SKU")]
        public string? Sku { get; set; }

        [Display(Name = "Mô tả ngắn")]
        [StringLength(500)]
        public string? ShortDescription { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [Display(Name = "Chất liệu")]
        [StringLength(100)]
        public string? Material { get; set; } // Ví dụ: 100% Cotton Organic, Thun lạnh dệt kim, Voan xốp cao cấp

        [Display(Name = "Hướng dẫn giặt ủi")]
        public string? CareInstructions { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá gốc")]
        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giá niêm yết (VNĐ)")]
        [Range(0, 100000000, ErrorMessage = "Giá phải từ 0 đến 100.000.000 VNĐ")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giá khuyến mãi (VNĐ)")]
        [Range(0, 100000000, ErrorMessage = "Giá khuyến mãi phải từ 0 đến 100.000.000 VNĐ")]
        public decimal? DiscountPrice { get; set; }

        [Display(Name = "Ảnh chính")]
        public string? MainImageUrl { get; set; }

        [Display(Name = "Giới tính")]
        [StringLength(30)]
        public string Gender { get; set; } = "Bé gái"; // "Bé gái", "Bé trai", "Unisex", "Sơ sinh"

        [Display(Name = "Nhóm tuổi / Giai đoạn")]
        [StringLength(50)]
        public string? AgeRange { get; set; } // "Sơ sinh (0 - 24 tháng)", "Bé tập đi (1 - 3 tuổi)", "Mẫu giáo (3 - 6 tuổi)", "Tiểu học (6 - 12 tuổi)"

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Hàng mới về")]
        public bool IsNewArrival { get; set; } = true;

        [Display(Name = "Bán chạy")]
        public bool IsBestSeller { get; set; } = false;

        [Display(Name = "Sản phẩm Flash Sale")]
        public bool IsFlashSale { get; set; } = false;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giá Flash Sale (VNĐ)")]
        public decimal? FlashSalePrice { get; set; }

        [Display(Name = "Số lượng mở bán Flash Sale")]
        public int FlashSaleQuantity { get; set; } = 0;

        [Display(Name = "Số lượng đã bán trong Flash Sale")]
        public int FlashSaleSold { get; set; } = 0;

        [Display(Name = "Thời gian kết thúc Flash Sale")]
        public DateTime? FlashSaleEndTime { get; set; }

        [Display(Name = "Sản phẩm Mua Kèm Deal Sốc")]
        public bool IsAddonDeal { get; set; } = false;

        [Column(TypeName = "decimal(18, 0)")]
        [Display(Name = "Giá Mua Kèm Deal Sốc (VNĐ)")]
        public decimal? AddonPrice { get; set; }

        [Display(Name = "Đang bán")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        [NotMapped]
        public int TotalStock => Variants?.Sum(v => v.StockQuantity) ?? 0;

        [NotMapped]
        public bool IsFlashSaleActive => IsFlashSale && FlashSalePrice.HasValue && FlashSalePrice.Value > 0 &&
            (!FlashSaleEndTime.HasValue || FlashSaleEndTime.Value > DateTime.Now) &&
            (FlashSaleQuantity == 0 || FlashSaleSold < FlashSaleQuantity);

        // Tính % giảm giá nếu có
        [NotMapped]
        public int DiscountPercentage
        {
            get
            {
                var cur = CurrentPrice;
                if (cur < Price && Price > 0)
                {
                    return (int)Math.Round((Price - cur) / Price * 100);
                }
                return 0;
            }
        }

        // Giá bán thực tế
        [NotMapped]
        public decimal CurrentPrice
        {
            get
            {
                if (IsFlashSaleActive) return FlashSalePrice!.Value;
                if (DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price) return DiscountPrice.Value;
                return Price;
            }
        }

        // Điểm đánh giá trung bình (1 đến 5 sao)
        [NotMapped]
        public double AverageRating
        {
            get
            {
                var approvedReviews = Reviews.Where(r => r.IsApproved).ToList();
                if (!approvedReviews.Any()) return 5.0; // Mặc định 5 sao cho sản phẩm mới
                return Math.Round(approvedReviews.Average(r => r.Rating), 1);
            }
        }

        // Tổng số lượt đánh giá
        [NotMapped]
        public int ReviewCount => Reviews.Count(r => r.IsApproved);
    }
}

