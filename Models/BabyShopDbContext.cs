using Microsoft.EntityFrameworkCore;

namespace WebsiteBabyShop.Models
{
    public class BabyShopDbContext : DbContext
    {
        public BabyShopDbContext(DbContextOptions<BabyShopDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<StockReceipt> StockReceipts { get; set; }
        public DbSet<StockReceiptDetail> StockReceiptDetails { get; set; }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }
        public DbSet<DailyCheckIn> DailyCheckIns { get; set; }
        public DbSet<LuckyWheelHistory> LuckyWheelHistories { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Category self-referencing
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - ProductImage
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product - ProductVariant
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order - User
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatConversation - User
            modelBuilder.Entity<ChatConversation>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatConversation - ChatMessage
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for ChatConversation SessionId
            modelBuilder.Entity<ChatConversation>()
                .HasIndex(c => c.SessionId)
                .IsUnique();

            // Product - ProductReview
            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductReview - User
            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique index for User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // StockReceipt - StockReceiptDetail
            modelBuilder.Entity<StockReceiptDetail>()
                .HasOne(d => d.StockReceipt)
                .WithMany(r => r.ReceiptDetails)
                .HasForeignKey(d => d.StockReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            // PointTransaction - User
            modelBuilder.Entity<PointTransaction>()
                .HasOne(pt => pt.User)
                .WithMany(u => u.PointTransactions)
                .HasForeignKey(pt => pt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // PointTransaction - Order
            modelBuilder.Entity<PointTransaction>()
                .HasOne(pt => pt.Order)
                .WithMany()
                .HasForeignKey(pt => pt.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // AuditLog Index
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Action);

            // UserCoupon - User, Coupon, Order
            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCoupons)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.Coupon)
                .WithMany(c => c.UserCoupons)
                .HasForeignKey(uc => uc.CouponId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCoupon>()
                .HasOne(uc => uc.Order)
                .WithMany()
                .HasForeignKey(uc => uc.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ràng buộc duy nhất: mỗi tài khoản chỉ được nhận 1 mã voucher đúng 1 lần
            modelBuilder.Entity<UserCoupon>()
                .HasIndex(uc => new { uc.UserId, uc.CouponId })
                .IsUnique();

            // Coupon - AssignedUser (Dành Cho Bạn)
            modelBuilder.Entity<Coupon>()
                .HasOne(c => c.AssignedUser)
                .WithMany()
                .HasForeignKey(c => c.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
