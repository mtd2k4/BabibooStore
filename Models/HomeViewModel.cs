namespace WebsiteBabyShop.Models
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public List<Product> NewArrivals { get; set; } = new List<Product>();
        public List<Product> BestSellers { get; set; } = new List<Product>();
        public List<Product> GirlProducts { get; set; } = new List<Product>();
        public List<Product> BoyProducts { get; set; } = new List<Product>();
        public List<Product> NewbornProducts { get; set; } = new List<Product>();
        public List<Product> FlashSaleProducts { get; set; } = new List<Product>();
    }
}
