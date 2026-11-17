using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Slug { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Hình ảnh đại diện")]
        public string? ImageUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Danh mục cha")]
        public int? ParentId { get; set; }
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
