namespace WebsiteBabyShop.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public bool IsAddonDeal { get; set; } = false;
        public int Quantity { get; set; } = 1;
        public int AvailableStock { get; set; } = 99;

        public decimal TotalPrice => Price * Quantity;
    }
}
