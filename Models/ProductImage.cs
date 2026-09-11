using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        [Display(Name = "Đường dẫn ảnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [Display(Name = "Ảnh chính")]
        public bool IsMain { get; set; } = false;

        [Display(Name = "Thứ tự")]
        public int DisplayOrder { get; set; } = 0;
    }
}
