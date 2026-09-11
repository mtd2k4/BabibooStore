using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Helpers;

namespace WebsiteBabyShop.Models
{
    public static class DbInitializer
    {
        public static void Initialize(BabyShopDbContext context)
        {
            context.Database.EnsureCreated();

            // Đảm bảo tạo bảng ChatConversations và ChatMessages nếu chưa tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.ChatConversations', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[ChatConversations] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [SessionId] NVARCHAR(100) NOT NULL,
                            [UserId] INT NULL,
                            [CustomerName] NVARCHAR(100) NOT NULL,
                            [CustomerPhone] NVARCHAR(20) NULL,
                            [LastMessageAt] DATETIME2 NOT NULL CONSTRAINT [DF_ChatConversations_LastMessageAt] DEFAULT (GETDATE()),
                            [HasUnreadAdmin] BIT NOT NULL CONSTRAINT [DF_ChatConversations_HasUnreadAdmin] DEFAULT (1),
                            CONSTRAINT [PK_ChatConversations] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_ChatConversations_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
                        );
                        CREATE UNIQUE NONCLUSTERED INDEX [IX_ChatConversations_SessionId] ON [dbo].[ChatConversations]([SessionId] ASC);
                    END

                    IF OBJECT_ID('dbo.ChatMessages', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[ChatMessages] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [ConversationId] INT NOT NULL,
                            [Sender] NVARCHAR(20) NOT NULL,
                            [SenderName] NVARCHAR(100) NOT NULL,
                            [Message] NVARCHAR(MAX) NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAt] DEFAULT (GETDATE()),
                            [IsRead] BIT NOT NULL CONSTRAINT [DF_ChatMessages_IsRead] DEFAULT (0),
                            CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_ChatMessages_ChatConversations] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[ChatConversations]([Id]) ON DELETE CASCADE
                        );
                    END
                ");
            }
            catch { }

            // Đảm bảo tạo bảng ProductReviews nếu chưa tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.ProductReviews', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[ProductReviews] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [ProductId] INT NOT NULL,
                            [UserId] INT NULL,
                            [CustomerName] NVARCHAR(100) NOT NULL,
                            [CustomerPhone] NVARCHAR(20) NULL,
                            [Rating] INT NOT NULL CONSTRAINT [DF_ProductReviews_Rating] DEFAULT (5),
                            [Comment] NVARCHAR(1000) NOT NULL,
                            [BabyInfo] NVARCHAR(100) NULL,
                            [IsVerifiedPurchase] BIT NOT NULL CONSTRAINT [DF_ProductReviews_IsVerifiedPurchase] DEFAULT (1),
                            [IsApproved] BIT NOT NULL CONSTRAINT [DF_ProductReviews_IsApproved] DEFAULT (1),
                            [HelpfulCount] INT NOT NULL CONSTRAINT [DF_ProductReviews_HelpfulCount] DEFAULT (0),
                            [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ProductReviews_CreatedAt] DEFAULT (GETDATE()),
                            CONSTRAINT [PK_ProductReviews] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_ProductReviews_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_ProductReviews_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
                        );
                        CREATE NONCLUSTERED INDEX [IX_ProductReviews_ProductId] ON [dbo].[ProductReviews]([ProductId] ASC);
                    END
                ");
            }
            catch { }

            // Đảm bảo tạo bảng Coupons và bổ sung cột CouponCode, DiscountAmount cho Orders nếu chưa tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.Coupons', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[Coupons] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [Code] NVARCHAR(50) NOT NULL,
                            [Title] NVARCHAR(200) NOT NULL,
                            [Description] NVARCHAR(500) NULL,
                            [DiscountType] INT NOT NULL DEFAULT (0),
                            [DiscountValue] DECIMAL(18, 0) NOT NULL,
                            [MinOrderAmount] DECIMAL(18, 0) NOT NULL DEFAULT (0),
                            [MaxDiscountAmount] DECIMAL(18, 0) NULL,
                            [UsageLimit] INT NULL DEFAULT (1000),
                            [UsedCount] INT NOT NULL DEFAULT (0),
                            [StartDate] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            [ExpiryDate] DATETIME2 NULL,
                            [IsActive] BIT NOT NULL DEFAULT (1),
                            CONSTRAINT [PK_Coupons] PRIMARY KEY CLUSTERED ([Id] ASC)
                        );
                        CREATE UNIQUE NONCLUSTERED INDEX [IX_Coupons_Code] ON [dbo].[Coupons]([Code] ASC);
                    END

                    IF COL_LENGTH('dbo.Orders', 'CouponCode') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Orders] ADD [CouponCode] NVARCHAR(50) NULL;
                    END

                    IF COL_LENGTH('dbo.Orders', 'DiscountAmount') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Orders] ADD [DiscountAmount] DECIMAL(18, 0) NOT NULL DEFAULT (0);
                    END
                ");
            }
            catch { }

            // Đảm bảo tạo bảng StockReceipts và StockReceiptDetails nếu chưa tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.StockReceipts', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[StockReceipts] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [ReceiptCode] NVARCHAR(50) NOT NULL,
                            [SupplierName] NVARCHAR(200) NOT NULL,
                            [SupplierPhone] NVARCHAR(20) NULL,
                            [SupplierAddress] NVARCHAR(255) NULL,
                            [Note] NVARCHAR(500) NULL,
                            [TotalAmount] DECIMAL(18, 0) NOT NULL DEFAULT (0),
                            [Status] NVARCHAR(50) NOT NULL DEFAULT (N'Completed'),
                            [CreatedBy] NVARCHAR(100) NULL DEFAULT (N'Admin'),
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            CONSTRAINT [PK_StockReceipts] PRIMARY KEY CLUSTERED ([Id] ASC)
                        );
                        CREATE UNIQUE NONCLUSTERED INDEX [IX_StockReceipts_Code] ON [dbo].[StockReceipts]([ReceiptCode] ASC);
                    END

                    IF OBJECT_ID('dbo.StockReceiptDetails', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[StockReceiptDetails] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [StockReceiptId] INT NOT NULL,
                            [ProductId] INT NOT NULL,
                            [ProductVariantId] INT NULL,
                            [ProductName] NVARCHAR(200) NOT NULL,
                            [Size] NVARCHAR(50) NULL,
                            [Color] NVARCHAR(50) NULL,
                            [Quantity] INT NOT NULL DEFAULT (1),
                            [ImportPrice] DECIMAL(18, 0) NOT NULL DEFAULT (0),
                            [TotalPrice] DECIMAL(18, 0) NOT NULL DEFAULT (0),
                            CONSTRAINT [PK_StockReceiptDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_StockReceiptDetails_StockReceipts] FOREIGN KEY ([StockReceiptId]) REFERENCES [dbo].[StockReceipts]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_StockReceiptDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION
                        );
                        CREATE NONCLUSTERED INDEX [IX_StockReceiptDetails_StockReceiptId] ON [dbo].[StockReceiptDetails]([StockReceiptId] ASC);
                    END

                    IF COL_LENGTH('dbo.ProductVariants', 'ImageUrl') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[ProductVariants] ADD [ImageUrl] NVARCHAR(500) NULL;
                    END
                ");
            }
            catch { }

            // Đảm bảo tạo bảng UserCoupons (Kho Voucher của từng tài khoản) nếu chưa tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.UserCoupons', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[UserCoupons] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [UserId] INT NOT NULL,
                            [CouponId] INT NOT NULL,
                            [CollectedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            [ExpiryDate] DATETIME2 NOT NULL,
                            [IsUsed] BIT NOT NULL DEFAULT (0),
                            [UsedAt] DATETIME2 NULL,
                            [OrderId] INT NULL,
                            CONSTRAINT [PK_UserCoupons] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_UserCoupons_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_UserCoupons_Coupons] FOREIGN KEY ([CouponId]) REFERENCES [dbo].[Coupons]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_UserCoupons_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE SET NULL
                        );
                        CREATE UNIQUE NONCLUSTERED INDEX [IX_UserCoupons_UserId_CouponId] ON [dbo].[UserCoupons]([UserId] ASC, [CouponId] ASC);
                    END
                ");
            }
            catch { }

            // Đảm bảo bổ sung các cột cho tính năng "Dành Cho Bạn" (12H Voucher)
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF COL_LENGTH('dbo.Coupons', 'IsForYou') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Coupons] ADD [IsForYou] BIT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Coupons', 'AssignedUserId') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Coupons] ADD [AssignedUserId] INT NULL;
                    END

                    IF COL_LENGTH('dbo.Users', 'LastForYouVoucherAt') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [LastForYouVoucherAt] DATETIME2 NULL;
                    END

                    IF COL_LENGTH('dbo.Coupons', 'IsFlashHunt') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Coupons] ADD [IsFlashHunt] BIT NOT NULL DEFAULT (0);
                    END
                ");
            }
            catch { }

            // Cập nhật ảnh tương ứng cho các biến thể màu sắc
            SeedVariantImages(context);

            // Đảm bảo có đơn hàng mẫu để kiểm thử tính năng Tra Cứu Đơn Hàng
            if (!context.Orders.Any() && context.Products.Any())
            {
                SeedSampleOrders(context);
            }

            // Đảm bảo có đánh giá sản phẩm mẫu
            if (!context.ProductReviews.Any() && context.Products.Any())
            {
                SeedSampleReviews(context);
            }

            // Đảm bảo có mã giảm giá mẫu
            if (!context.Coupons.Any())
            {
                SeedSampleCoupons(context);
            }

            // Đảm bảo có mã Săn Giờ Vàng mẫu (hiển thị sẵn trong kho, mở sau 2 tiếng)
            try
            {
                if (!context.Coupons.Any(c => c.IsFlashHunt))
                {
                    var flashHuntCoupon = new Coupon
                    {
                        Code = "SANMA-50K",
                        Title = "Săn Mã Giờ Vàng - Giảm 50.000đ",
                        Description = "Ưu đãi săn mã giảm ngay 50.000đ cho đơn từ 200.000đ. Hiển thị sẵn trong kho tất cả tài khoản, đến giờ mở mới được sử dụng!",
                        DiscountType = DiscountType.FixedAmount,
                        DiscountValue = 50000,
                        MinOrderAmount = 200000,
                        MaxDiscountAmount = null,
                        UsageLimit = 200,
                        UsedCount = 0,
                        StartDate = DateTime.Now.AddHours(2), // Mở sau 2 tiếng
                        ExpiryDate = DateTime.Now.AddDays(3),
                        IsActive = true,
                        IsFlashHunt = true
                    };
                    context.Coupons.Add(flashHuntCoupon);
                    context.SaveChanges();
                }
            }
            catch { }

            // Đảm bảo có phiếu nhập hàng mẫu
            if (!context.StockReceipts.Any() && context.Products.Any())
            {
                SeedSampleStockReceipts(context);
            }

            // Đảm bảo tạo bảng và các cột mới cho Nhóm 3 (Baby Club, Flash Sale, Deal Sốc) và Nhóm 4 (Audit Logs)
            EnsureGroup3And4TablesAndColumns(context);
            SeedGroup3And4Data(context);

            // Đảm bảo tạo bảng và dữ liệu cho Điểm danh hàng ngày & Vòng quay may mắn
            EnsureDailyCheckInAndLuckyWheel(context);

            // Đảm bảo tạo bảng và dữ liệu mẫu cho Đổi / Trả Hàng (ReturnRequests)
            EnsureReturnRequestsTableAndSeed(context);

            // Tự động làm sạch và chuẩn hóa toàn bộ dữ liệu tiếng Việt trong CSDL
            CleanCorruptedData(context);

            // Nếu đã có danh mục thì không cần seed lại
            if (context.Categories.Any())
            {
                return;
            }

            // 1. Tạo Tài khoản mặc định
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = PasswordHelper.HashPassword("Admin@123456"),
                FullName = "Quản Trị Viên BabyShop",
                Email = "admin@babyshop.vn",
                PhoneNumber = "0988889999",
                Address = "Hà Nội, Việt Nam",
                Role = "Admin",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            var customerUser = new User
            {
                Username = "khachhang",
                PasswordHash = PasswordHelper.HashPassword("123456"),
                FullName = "Nguyễn Thu Hà",
                Email = "thuha@gmail.com",
                PhoneNumber = "0912345678",
                Address = "123 Cầu Giấy, Hà Nội",
                Role = "Customer",
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            context.Users.AddRange(adminUser, customerUser);
            context.SaveChanges();

            // 2. Tạo Danh mục Thời trang Trẻ em
            var catGirl = new Category
            {
                Name = "Thời Trang Bé Gái",
                Slug = "thoi-trang-be-gai",
                Description = "Váy đầm công chúa, set đồ xinh xắn, áo kiểu và chân váy thời thượng cho bé gái",
                ImageUrl = "https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 1,
                IsActive = true
            };

            var catBoy = new Category
            {
                Name = "Thời Trang Bé Trai",
                Slug = "thoi-trang-be-trai",
                Description = "Set công tử, áo thun polo năng động, quần jean và yếm sành điệu cho bé trai",
                ImageUrl = "https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 2,
                IsActive = true
            };

            var catNewborn = new Category
            {
                Name = "Thời Trang Sơ Sinh (0-24M)",
                Slug = "thoi-trang-so-sinh",
                Description = "Bodysuit hữu cơ, romper mềm mại, bộ dài tay 100% cotton organic bảo vệ làn da non nớt",
                ImageUrl = "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 3,
                IsActive = true
            };

            var catAccessories = new Category
            {
                Name = "Phụ Kiện & Giày Dép",
                Slug = "phu-kien-giay-dep",
                Description = "Mũ nón, kính râm, nơ kẹp tóc, vớ tất và giày tập đi êm chân chống trượt",
                ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 4,
                IsActive = true
            };

            context.Categories.AddRange(catGirl, catBoy, catNewborn, catAccessories);
            context.SaveChanges();

            // 3. Tạo Sản phẩm mẫu cho từng danh mục
            var products = new List<Product>
            {
                // Bé gái
                new Product
                {
                    Name = "Váy Công Chúa Voan Xốp Nơ Pastel",
                    Slug = "vay-cong-chua-voan-xop-no-pastel",
                    Sku = "VBG-001",
                    ShortDescription = "Váy công chúa chất voan dệt xốp bồng bềnh, phối nơ ngọt ngào cho bé dự tiệc, sinh nhật.",
                    Description = "<p>Chiếc đầm công chúa làm từ <strong>chất liệu voan dệt xốp cao cấp</strong>, lớp lót trong 100% cotton mềm mịn, không gây châm chích cho làn da nhạy cảm của bé. Thiết kế tay phồng nhẹ nhàng cùng nơ xinh đính sau lưng giúp bé tỏa sáng như nàng công chúa nhỏ trong mọi buổi tiệc hay sinh nhật.</p><ul><li>Chất liệu: Voan tuyết cao cấp, lót lụa habutai 100% cotton.</li><li>Độ tuổi phù hợp: 1 - 6 tuổi.</li><li>Màu sắc: Hồng Pastel, Trắng Kem.</li></ul>",
                    Material = "Voan xốp cao cấp, lót cotton 100%",
                    CareInstructions = "Nên giặt tay nhẹ nhàng, không dùng chất tẩy mạnh, ủi ở nhiệt độ thấp.",
                    Price = 320000,
                    DiscountPrice = 259000,
                    MainImageUrl = "https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80",
                    Gender = "Bé gái",
                    AgeRange = "1 - 6 tuổi",
                    IsFeatured = true,
                    IsNewArrival = true,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catGirl.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "1 - 2 tuổi (9 - 11kg)", Color = "Hồng Pastel", ColorHex = "#FFC0CB", StockQuantity = 20, Sku = "VBG-001-P1" },
                        new ProductVariant { Size = "2 - 3 tuổi (12 - 14kg)", Color = "Hồng Pastel", ColorHex = "#FFC0CB", StockQuantity = 15, Sku = "VBG-001-P2" },
                        new ProductVariant { Size = "3 - 4 tuổi (15 - 17kg)", Color = "Hồng Pastel", ColorHex = "#FFC0CB", StockQuantity = 12, Sku = "VBG-001-P3" },
                        new ProductVariant { Size = "4 - 5 tuổi (18 - 20kg)", Color = "Hồng Pastel", ColorHex = "#FFC0CB", StockQuantity = 8, Sku = "VBG-001-P4" },
                        new ProductVariant { Size = "1 - 2 tuổi (9 - 11kg)", Color = "Trắng Kem", ColorHex = "#FFFDD0", StockQuantity = 15, Sku = "VBG-001-W1" },
                        new ProductVariant { Size = "2 - 3 tuổi (12 - 14kg)", Color = "Trắng Kem", ColorHex = "#FFFDD0", StockQuantity = 10, Sku = "VBG-001-W2" }
                    }
                },
                new Product
                {
                    Name = "Đầm Hoa Nhí Cổ Sen Phong Cách Vintage",
                    Slug = "dam-hoa-nhi-co-sen-vintage",
                    Sku = "VBG-002",
                    ShortDescription = "Đầm suông vải thô đũi organic thoáng mát in họa tiết hoa nhí phong cách Hàn Quốc dịu dàng.",
                    Description = "<p>Đầm hoa nhí cổ bèo sen mang phong cách vintage nhẹ nhàng, đằm thắm. Vải thô đũi dệt sợi organic siêu thoáng mát, thấm hút mồ hôi cực tốt, rất thích hợp cho bé đi dạo phố, đi học mầm non hay chụp ảnh dã ngoại cuối tuần.</p>",
                    Material = "Vải thô đũi Cotton Organic",
                    CareInstructions = "Giặt máy ở chế độ nhẹ nhàng, phơi nơi râm mát.",
                    Price = 280000,
                    DiscountPrice = 219000,
                    MainImageUrl = "https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80",
                    Gender = "Bé gái",
                    AgeRange = "2 - 7 tuổi",
                    IsFeatured = true,
                    IsNewArrival = false,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catGirl.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Size 90 (2 - 3T, 11-13kg)", Color = "Vàng Nhạt", ColorHex = "#FFF9A6", StockQuantity = 18, Sku = "VBG-002-Y90" },
                        new ProductVariant { Size = "Size 100 (3 - 4T, 14-16kg)", Color = "Vàng Nhạt", ColorHex = "#FFF9A6", StockQuantity = 22, Sku = "VBG-002-Y100" },
                        new ProductVariant { Size = "Size 110 (4 - 5T, 17-19kg)", Color = "Vàng Nhạt", ColorHex = "#FFF9A6", StockQuantity = 14, Sku = "VBG-002-Y110" },
                        new ProductVariant { Size = "Size 120 (5 - 6T, 20-23kg)", Color = "Vàng Nhạt", ColorHex = "#FFF9A6", StockQuantity = 10, Sku = "VBG-002-Y120" }
                    }
                },
                new Product
                {
                    Name = "Áo Len Cardigan Dệt Kim Họa Tiết Quả Trám",
                    Slug = "ao-len-cardigan-det-kim-hoa-tiet-qua-tram",
                    Sku = "VBG-003",
                    ShortDescription = "Áo cardigan len dệt kim sợi mềm cao cấp, ấm áp và dễ phối với mọi loại váy áo cho bé.",
                    Description = "<p>Áo khoác len cardigan dệt sợi bông tự nhiên giữ ấm hiệu quả, co giãn đàn hồi tốt, bé vận động không bị cộm bí. Hàng cúc gỗ mộc mạc phong cách Morigirl Hàn Quốc.</p>",
                    Material = "Len lông cừu pha sợi Cotton tự nhiên",
                    CareInstructions = "Nên giặt tay, vắt nhẹ, phơi nằm ngang để giữ dáng áo.",
                    Price = 350000,
                    DiscountPrice = 289000,
                    MainImageUrl = "https://images.unsplash.com/photo-1518831959646-742c3a14ebf7?auto=format&fit=crop&w=800&q=80",
                    Gender = "Bé gái",
                    AgeRange = "1 - 6 tuổi",
                    IsFeatured = false,
                    IsNewArrival = true,
                    IsBestSeller = false,
                    IsActive = true,
                    CategoryId = catGirl.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Size 90 (1 - 2 tuổi)", Color = "Hồng Đất", ColorHex = "#DDA0DD", StockQuantity = 12, Sku = "VBG-003-HD90" },
                        new ProductVariant { Size = "Size 100 (2 - 3 tuổi)", Color = "Hồng Đất", ColorHex = "#DDA0DD", StockQuantity = 14, Sku = "VBG-003-HD100" },
                        new ProductVariant { Size = "Size 110 (3 - 5 tuổi)", Color = "Be Kem", ColorHex = "#F5F5DC", StockQuantity = 16, Sku = "VBG-003-BE110" }
                    }
                },

                // Bé trai
                new Product
                {
                    Name = "Set Quần Yếm Denim Phong Cách Hàn Quốc Bé Trai",
                    Slug = "set-quan-yem-denim-han-quoc-be-trai",
                    Sku = "VBT-001",
                    ShortDescription = "Set yếm bò jean mềm co giãn kèm áo thun trắng cotton cho bé trai năng động, bảnh bao.",
                    Description = "<p>Bộ yếm bò thời trang được các mẹ yêu thích nhất cho bé trai. Chất denim wash mềm đã xử lý công nghệ nano không bai màu, không cứng ráp. Kèm theo áo phông trắng 100% cotton co giãn 4 chiều.</p>",
                    Material = "Denim wash mềm + Thun cotton 4 chiều",
                    CareInstructions = "Giặt riêng với đồ sáng màu ở lần giặt đầu tiên.",
                    Price = 360000,
                    DiscountPrice = 299000,
                    MainImageUrl = "https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80",
                    Gender = "Bé trai",
                    AgeRange = "1 - 5 tuổi",
                    IsFeatured = true,
                    IsNewArrival = true,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catBoy.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Size 90 (9 - 11kg)", Color = "Xanh Denim", ColorHex = "#4682B4", StockQuantity = 15, Sku = "VBT-001-D90" },
                        new ProductVariant { Size = "Size 100 (12 - 14kg)", Color = "Xanh Denim", ColorHex = "#4682B4", StockQuantity = 20, Sku = "VBT-001-D100" },
                        new ProductVariant { Size = "Size 110 (15 - 17kg)", Color = "Xanh Denim", ColorHex = "#4682B4", StockQuantity = 18, Sku = "VBT-001-D110" },
                        new ProductVariant { Size = "Size 120 (18 - 21kg)", Color = "Xanh Denim", ColorHex = "#4682B4", StockQuantity = 10, Sku = "VBT-001-D120" }
                    }
                },
                new Product
                {
                    Name = "Áo Thun Polo Kẻ Ngang Thể Thao Năng Động",
                    Slug = "ao-thun-polo-ke-ngang-nang-dong",
                    Sku = "VBT-002",
                    ShortDescription = "Áo polo cổ bẻ phối sọc ngang thời thượng, phong cách quý tử nhí lịch thiệp nhưng năng động.",
                    Description = "<p>Chất cá sấu cotton co giãn thấm hút mồ hôi cực tốt. Thiết kế cổ bẻ cứng cáp giữ form chuẩn, bé mặc đi tiệc, đi chơi hay đi học đều rất bảnh bao và phong độ.</p>",
                    Material = "Vải Pique cá sấu 100% Cotton",
                    CareInstructions = "Giặt máy bình thường, ủi nhiệt độ vừa phải.",
                    Price = 195000,
                    DiscountPrice = 159000,
                    MainImageUrl = "https://images.unsplash.com/photo-1503944583220-79d8926ad5e2?auto=format&fit=crop&w=800&q=80",
                    Gender = "Bé trai",
                    AgeRange = "2 - 8 tuổi",
                    IsFeatured = true,
                    IsNewArrival = false,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catBoy.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Size 100 (2 - 3T, 12-14kg)", Color = "Xanh Navy Sọc", ColorHex = "#000080", StockQuantity = 25, Sku = "VBT-002-NV1" },
                        new ProductVariant { Size = "Size 110 (3 - 4T, 15-17kg)", Color = "Xanh Navy Sọc", ColorHex = "#000080", StockQuantity = 20, Sku = "VBT-002-NV2" },
                        new ProductVariant { Size = "Size 120 (5 - 6T, 18-21kg)", Color = "Xanh Navy Sọc", ColorHex = "#000080", StockQuantity = 15, Sku = "VBT-002-NV3" },
                        new ProductVariant { Size = "Size 130 (7 - 8T, 22-26kg)", Color = "Xanh Navy Sọc", ColorHex = "#000080", StockQuantity = 12, Sku = "VBT-002-NV4" }
                    }
                },
                new Product
                {
                    Name = "Áo Khoác Jean Denim Phối Nón Hoodie Bé Trai",
                    Slug = "ao-khoac-jean-denim-phoi-non-hoodie",
                    Sku = "VBT-003",
                    ShortDescription = "Áo khoác bò cực ngầu lót nỉ mềm, tay và nón phối nỉ xám ấm áp cho bé đi chơi ngày gió.",
                    Description = "<p>Mẫu áo khoác street style cực chất cho các bé trai cá tính. Vừa cản gió tốt vừa giữ ấm cơ thể, dễ mix cùng quần jean hoặc jogger thể thao.</p>",
                    Material = "Jean denim lót cotton nỉ mềm",
                    CareInstructions = "Giặt máy ở chu kỳ nhẹ, phơi trong bóng râm.",
                    Price = 380000,
                    DiscountPrice = 320000,
                    MainImageUrl = "https://images.unsplash.com/photo-1471286174890-9c112ffca564?auto=format&fit=crop&w=800&q=80",
                    Gender = "Bé trai",
                    AgeRange = "3 - 8 tuổi",
                    IsFeatured = false,
                    IsNewArrival = true,
                    IsBestSeller = false,
                    IsActive = true,
                    CategoryId = catBoy.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Size 110 (3 - 4 tuổi)", Color = "Xanh Bò", ColorHex = "#4682B4", StockQuantity = 15, Sku = "VBT-003-XB110" },
                        new ProductVariant { Size = "Size 120 (5 - 6 tuổi)", Color = "Xanh Bò", ColorHex = "#4682B4", StockQuantity = 18, Sku = "VBT-003-XB120" },
                        new ProductVariant { Size = "Size 130 (7 - 8 tuổi)", Color = "Xanh Bò", ColorHex = "#4682B4", StockQuantity = 12, Sku = "VBT-003-XB130" }
                    }
                },

                // Sơ sinh
                new Product
                {
                    Name = "Bộ Bodysuit Romper Cotton Organic Xuất Hàn Cho Bé Sơ Sinh",
                    Slug = "bo-bodysuit-romper-cotton-organic-xuat-han",
                    Sku = "BSS-001",
                    ShortDescription = "Chất liệu cotton hữu cơ kháng khuẩn, đường may phẳng lộn ngoài chống xước da bé sơ sinh.",
                    Description = "<p>Sản phẩm dành riêng cho làn da non nớt của các thiên thần nhỏ. 100% sợi cotton hữu cơ tự nhiên không chất tẩy nhuộm độc hại, đạt chứng chỉ an toàn Oeko-Tex. Thiết kế hàng khuy bấm đáy đũng tã tiện lợi giúp mẹ thay bỉm cho bé dễ dàng mà không làm bé tỉnh giấc.</p>",
                    Material = "100% Cotton Organic nguyên chất",
                    CareInstructions = "Nên dùng nước giặt chuyên dụng cho em bé, giặt ở nhiệt độ thường.",
                    Price = 210000,
                    DiscountPrice = 165000,
                    MainImageUrl = "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80",
                    Gender = "Sơ sinh",
                    AgeRange = "0 - 12 tháng",
                    IsFeatured = true,
                    IsNewArrival = true,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catNewborn.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "0 - 3 tháng (3 - 5kg)", Color = "Vàng Kem Nhạt", ColorHex = "#FFFDD0", StockQuantity = 30, Sku = "BSS-001-03" },
                        new ProductVariant { Size = "3 - 6 tháng (5 - 7kg)", Color = "Vàng Kem Nhạt", ColorHex = "#FFFDD0", StockQuantity = 25, Sku = "BSS-001-36" },
                        new ProductVariant { Size = "6 - 9 tháng (7 - 9kg)", Color = "Vàng Kem Nhạt", ColorHex = "#FFFDD0", StockQuantity = 20, Sku = "BSS-001-69" },
                        new ProductVariant { Size = "9 - 12 tháng (9 - 11kg)", Color = "Vàng Kem Nhạt", ColorHex = "#FFFDD0", StockQuantity = 15, Sku = "BSS-001-912" }
                    }
                },
                new Product
                {
                    Name = "Set Bộ Dài Tay Cài Lệch Bamboo Kháng Khuẩn Sơ Sinh",
                    Slug = "set-bo-dai-tay-cai-lech-bamboo",
                    Sku = "BSS-002",
                    ShortDescription = "Vải sợi tre Bamboo cao cấp mềm mướt, điều hòa thân nhiệt cực tốt cho bé nằm điều hòa.",
                    Description = "<p>Sợi tre thiên nhiên có khả năng kháng khuẩn tự nhiên, kiểm soát mùi và điều hòa thân nhiệt thông minh: mát mẻ mùa hè, ấm áp mùa đông. Thiết kế vạt áo cài chéo đôi 2 lớp bảo vệ vùng ngực và rốn nhạy cảm của bé sơ sinh.</p>",
                    Material = "Sợi tre Bamboo 95% + Spandex 5%",
                    CareInstructions = "Giặt nhẹ nhàng với nước giặt em bé, không vắt xoắn mạnh.",
                    Price = 260000,
                    DiscountPrice = 205000,
                    MainImageUrl = "https://images.unsplash.com/photo-1555252333-9f8e92e65df9?auto=format&fit=crop&w=800&q=80",
                    Gender = "Sơ sinh",
                    AgeRange = "0 - 18 tháng",
                    IsFeatured = true,
                    IsNewArrival = false,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catNewborn.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "0 - 3 tháng (3 - 5kg)", Color = "Xanh Pastel", ColorHex = "#B0E0E6", StockQuantity = 25, Sku = "BSS-002-BL03" },
                        new ProductVariant { Size = "3 - 6 tháng (5 - 7kg)", Color = "Xanh Pastel", ColorHex = "#B0E0E6", StockQuantity = 20, Sku = "BSS-002-BL36" },
                        new ProductVariant { Size = "6 - 12 tháng (7 - 10kg)", Color = "Hồng Phấn", ColorHex = "#FFB6C1", StockQuantity = 22, Sku = "BSS-002-PK612" }
                    }
                },

                // Phụ kiện & Giày
                new Product
                {
                    Name = "Giày Tập Đi Da Mềm Quai Dán Chống Trượt Cho Bé",
                    Slug = "giay-tap-di-da-mem-quai-dan-chong-truot",
                    Sku = "PK-001",
                    ShortDescription = "Đế cao su nhiệt dẻo mềm uốn cong 1/3, hỗ trợ bé tập những bước đi đầu đời an toàn.",
                    Description = "<p>Giày tập đi chuẩn khoa học y khoa cho bé từ 6 tháng đến 2 tuổi. Mũi giày rộng rãi không chèn ép ngón chân non, đế cao su tổ ong bám sàn chống trơn trượt tối đa. Quai dán velcro chắc chắn giúp mẹ dễ dàng xỏ giày cho bé.</p>",
                    Material = "Da PU cao cấp vi sinh + Đế cao su dẻo",
                    CareInstructions = "Lau sạch bằng khăn ẩm, không ngâm lâu trong nước.",
                    Price = 220000,
                    DiscountPrice = 175000,
                    MainImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=800&q=80",
                    Gender = "Unisex",
                    AgeRange = "6 - 24 tháng",
                    IsFeatured = true,
                    IsNewArrival = true,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catAccessories.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Size 11 (Chân 10.5cm, 6-9M)", Color = "Nâu Bò Vintage", ColorHex = "#8B4513", StockQuantity = 15, Sku = "PK-001-11" },
                        new ProductVariant { Size = "Size 12 (Chân 11.5cm, 9-12M)", Color = "Nâu Bò Vintage", ColorHex = "#8B4513", StockQuantity = 18, Sku = "PK-001-12" },
                        new ProductVariant { Size = "Size 13 (Chân 12.5cm, 12-18M)", Color = "Trắng Sữa", ColorHex = "#FFFFF0", StockQuantity = 20, Sku = "PK-001-13" }
                    }
                },
                new Product
                {
                    Name = "Mũ Cói Tai Gấu Dễ Thương Cho Bé Đi Biển Dã Ngoại",
                    Slug = "mu-coi-tai-gau-de-thuong",
                    Sku = "PK-002",
                    ShortDescription = "Chất liệu cói dệt thủ công thoáng mát có dây rút giữ quai nón cho bé tung tăng ngoài trời.",
                    Description = "<p>Chiếc mũ cói xinh xắn với hai tai gấu đáng yêu, vành rộng giúp che nắng bảo vệ khuôn mặt và mắt của bé. Có quai thun mềm mại cài dưới cằm giữ nón không bị bay khi có gió.</p>",
                    Material = "Sợi cói tự nhiên mềm dẻo",
                    CareInstructions = "Bảo quản nơi khô ráo, dùng cọ mềm vệ sinh vết bẩn.",
                    Price = 135000,
                    DiscountPrice = 99000,
                    MainImageUrl = "https://images.unsplash.com/photo-1508214751196-bcfd4ca60f91?auto=format&fit=crop&w=800&q=80",
                    Gender = "Unisex",
                    AgeRange = "1 - 5 tuổi",
                    IsFeatured = false,
                    IsNewArrival = false,
                    IsBestSeller = true,
                    IsActive = true,
                    CategoryId = catAccessories.Id,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { Size = "Vòng đầu 46 - 50cm", Color = "Nâu Cói Tự Nhiên", ColorHex = "#D2B48C", StockQuantity = 30, Sku = "PK-002-NC" }
                    }
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();
        }

        private static void SeedSampleOrders(BabyShopDbContext context)
        {
            var products = context.Products.Take(4).ToList();
            if (!products.Any()) return;

            var p1 = products[0];
            var p2 = products.Count > 1 ? products[1] : products[0];
            var p3 = products.Count > 2 ? products[2] : products[0];

            var order1 = new Order
            {
                OrderCode = "BS-20260910-1024",
                CustomerName = "Nguyễn Thu Hà",
                CustomerPhone = "0987654321",
                CustomerEmail = "thuha@gmail.com",
                ShippingAddress = "Số 45 Đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội",
                Note = "Giao giờ hành chính, gọi trước khi giao giúp mình nha shop.",
                SubTotal = p1.CurrentPrice + p2.CurrentPrice,
                ShippingFee = 0, // Freeship từ 300K
                TotalAmount = p1.CurrentPrice + p2.CurrentPrice,
                PaymentMethod = "COD",
                IsPaid = false,
                OrderStatus = "Shipping", // Đang giao hàng
                CreatedAt = DateTime.Now.AddDays(-1),
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        ProductId = p1.Id,
                        ProductName = p1.Name,
                        ProductImage = p1.MainImageUrl,
                        Size = "Size 100 (2-3T)",
                        Color = "Hồng Pastel",
                        UnitPrice = p1.CurrentPrice,
                        Quantity = 1
                    },
                    new OrderDetail
                    {
                        ProductId = p2.Id,
                        ProductName = p2.Name,
                        ProductImage = p2.MainImageUrl,
                        Size = "Size 110 (3-4T)",
                        Color = "Trắng Kem",
                        UnitPrice = p2.CurrentPrice,
                        Quantity = 1
                    }
                }
            };

            var order2 = new Order
            {
                OrderCode = "BS-20260909-8868",
                CustomerName = "Trần Minh Anh",
                CustomerPhone = "0912345678",
                CustomerEmail = "minhanh.tran@gmail.com",
                ShippingAddress = "288 Nguyễn Tri Phương, Phường 4, Quận 10, TP. Hồ Chí Minh",
                Note = "Giao buổi sáng giúp mình, đóng hộp cẩn thận nhé!",
                SubTotal = p3.CurrentPrice * 2,
                ShippingFee = 0,
                TotalAmount = p3.CurrentPrice * 2,
                PaymentMethod = "BankTransfer",
                IsPaid = true,
                OrderStatus = "Completed", // Giao thành công
                CreatedAt = DateTime.Now.AddDays(-3),
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        ProductId = p3.Id,
                        ProductName = p3.Name,
                        ProductImage = p3.MainImageUrl,
                        Size = "Size 90 (1-2T)",
                        Color = "Xanh Sage",
                        UnitPrice = p3.CurrentPrice,
                        Quantity = 2
                    }
                }
            };

            context.Orders.AddRange(order1, order2);
            context.SaveChanges();
        }

        private static void SeedSampleReviews(BabyShopDbContext context)
        {
            var products = context.Products.Take(6).ToList();
            if (!products.Any()) return;

            var reviews = new List<ProductReview>();

            // Sản phẩm 1
            if (products.Count > 0)
            {
                var p1 = products[0];
                reviews.AddRange(new[]
                {
                    new ProductReview
                    {
                        ProductId = p1.Id,
                        CustomerName = "Mẹ Bảo An",
                        Rating = 5,
                        BabyInfo = "Bé gái 18 tháng • 11.5 kg",
                        Comment = "Váy xinh xỉu các mẹ ơi! Chất vải cotton organic mềm mịn sờ mướt tay lắm, đường chỉ may phẳng phiu không có sợi thừa cọ ngứa da con. Bé mặc đi tiệc ai cũng khen như công chúa nhỏ.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 18,
                        CreatedAt = DateTime.Now.AddDays(-2)
                    },
                    new ProductReview
                    {
                        ProductId = p1.Id,
                        CustomerName = "Chị Minh Thư",
                        Rating = 5,
                        BabyInfo = "Bé 2 tuổi • 12 kg",
                        Comment = "Giao hàng siêu nhanh, đóng gói hộp xinh xắn thơm nức mùi em bé. Form váy xòe bồng bềnh tự nhiên, giặt máy chế độ nhẹ không hề bị co rút hay phai màu.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 9,
                        CreatedAt = DateTime.Now.AddDays(-5)
                    },
                    new ProductReview
                    {
                        ProductId = p1.Id,
                        CustomerName = "Mẹ Cún Bông",
                        Rating = 5,
                        BabyInfo = "Bé 14 tháng • 10 kg",
                        Comment = "Mình mua làm quà sinh nhật cho cháu, mẹ bé thích mê. Vải cao cấp chuẩn xịn, sờ mát rượi. Shop tư vấn size cực kỳ có tâm!",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 6,
                        CreatedAt = DateTime.Now.AddDays(-8)
                    },
                    new ProductReview
                    {
                        ProductId = p1.Id,
                        CustomerName = "Nguyễn Thanh Trúc",
                        Rating = 4,
                        BabyInfo = "Bé gái 3 tuổi • 14 kg",
                        Comment = "Váy rất đẹp, màu sắc ngọt ngào đúng như hình mẫu. Bé mình hơi tròn bụng nên mặc size 100 vừa khít luôn, mẹ nào có bé bụ bẫm nên nhích lên 1 size nhé.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 4,
                        CreatedAt = DateTime.Now.AddDays(-12)
                    }
                });
            }

            // Sản phẩm 2
            if (products.Count > 1)
            {
                var p2 = products[1];
                reviews.AddRange(new[]
                {
                    new ProductReview
                    {
                        ProductId = p2.Id,
                        CustomerName = "Mẹ Sóc Nâu",
                        Rating = 5,
                        BabyInfo = "Bé trai 2.5 tuổi • 13.5 kg",
                        Comment = "Set đồ năng động siêu ngầu cho hoàng tử nhỏ! Thun co giãn 4 chiều cực thích, cạp chun mềm không hằn bụng con. Bé chạy nhảy cả buổi mà vẫn khô thoáng mồ hôi.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 12,
                        CreatedAt = DateTime.Now.AddDays(-3)
                    },
                    new ProductReview
                    {
                        ProductId = p2.Id,
                        CustomerName = "Hoàng Mai",
                        Rating = 5,
                        BabyInfo = "Bé 3 tuổi • 15 kg",
                        Comment = "Chất vải dệt kim dày dặn nhưng thoáng khí, không xù lông. Đã mua đồ ở BabyShop nhiều lần và lần nào cũng ưng ý 100%.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 7,
                        CreatedAt = DateTime.Now.AddDays(-7)
                    }
                });
            }

            // Sản phẩm 3
            if (products.Count > 2)
            {
                var p3 = products[2];
                reviews.AddRange(new[]
                {
                    new ProductReview
                    {
                        ProductId = p3.Id,
                        CustomerName = "Mẹ Bắp Nhỏ",
                        Rating = 5,
                        BabyInfo = "Bé 5 tháng • 7.2 kg",
                        Comment = "Bộ body chip siêu mềm mịn, cúc bấm đáy chắc chắn thay bỉm cho bé nửa đêm siêu nhanh. Vải organic không mùi hóa chất, mẹ rất yên tâm cho da con nhạy cảm.",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        HelpfulCount = 14,
                        CreatedAt = DateTime.Now.AddDays(-4)
                    }
                });
            }

            context.ProductReviews.AddRange(reviews);
            context.SaveChanges();
        }

        private static void SeedSampleCoupons(BabyShopDbContext context)
        {
            var coupons = new List<Coupon>
            {
                new Coupon
                {
                    Code = "BABYNEW",
                    Title = "Ưu đãi chào bé mới",
                    Description = "Giảm 10% tối đa 40.000đ cho đơn hàng đầu tiên từ 150.000đ",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 10,
                    MinOrderAmount = 150000,
                    MaxDiscountAmount = 40000,
                    UsageLimit = 500,
                    UsedCount = 28,
                    StartDate = DateTime.Now.AddDays(-30),
                    ExpiryDate = DateTime.Now.AddMonths(3),
                    IsActive = true
                },
                new Coupon
                {
                    Code = "MEVACO50",
                    Title = "Mẹ và bé yêu",
                    Description = "Giảm ngay 50.000đ tiền mặt cho đơn hàng từ 300.000đ",
                    DiscountType = DiscountType.FixedAmount,
                    DiscountValue = 50000,
                    MinOrderAmount = 300000,
                    MaxDiscountAmount = null,
                    UsageLimit = 200,
                    UsedCount = 42,
                    StartDate = DateTime.Now.AddDays(-15),
                    ExpiryDate = DateTime.Now.AddMonths(2),
                    IsActive = true
                },
                new Coupon
                {
                    Code = "FREESHIP",
                    Title = "Miễn phí vận chuyển",
                    Description = "Giảm ngay 30.000đ phí giao hàng toàn quốc cho đơn từ 100.000đ",
                    DiscountType = DiscountType.FixedAmount,
                    DiscountValue = 30000,
                    MinOrderAmount = 100000,
                    MaxDiscountAmount = null,
                    UsageLimit = 1000,
                    UsedCount = 135,
                    StartDate = DateTime.Now.AddDays(-60),
                    ExpiryDate = DateTime.Now.AddMonths(6),
                    IsActive = true
                },
                new Coupon
                {
                    Code = "VIPBABY",
                    Title = "Khách hàng thân thiết",
                    Description = "Giảm 15% tối đa 100.000đ cho đơn hàng từ 500.000đ",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 15,
                    MinOrderAmount = 500000,
                    MaxDiscountAmount = 100000,
                    UsageLimit = 300,
                    UsedCount = 15,
                    StartDate = DateTime.Now.AddDays(-10),
                    ExpiryDate = DateTime.Now.AddMonths(3),
                    IsActive = true
                },
                new Coupon
                {
                    Code = "DAIDIECTUYET",
                    Title = "Đại tiệc sắm đồ cho con",
                    Description = "Giảm 20% tối đa 180.000đ cho đơn hàng lớn từ 800.000đ",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 20,
                    MinOrderAmount = 800000,
                    MaxDiscountAmount = 180000,
                    UsageLimit = 100,
                    UsedCount = 9,
                    StartDate = DateTime.Now.AddDays(-5),
                    ExpiryDate = DateTime.Now.AddMonths(1),
                    IsActive = true
                }
            };

            context.Coupons.AddRange(coupons);
            context.SaveChanges();
        }

        private static void SeedSampleStockReceipts(BabyShopDbContext context)
        {
            var products = context.Products.Include(p => p.Variants).Take(4).ToList();
            if (!products.Any()) return;

            var r1 = new StockReceipt
            {
                ReceiptCode = "PN-20260908-1001",
                SupplierName = "Xưởng Dệt May Trẻ Em Minh Châu",
                SupplierPhone = "0918889922",
                SupplierAddress = "Lô 12, KCN Tân Bình, TP. Hồ Chí Minh",
                Note = "Nhập lô hàng đầu vụ thu đông bé gái và body chip sơ sinh",
                Status = "Completed",
                CreatedBy = "Quản Trị Viên BabyShop",
                CreatedAt = DateTime.Now.AddDays(-3),
                TotalAmount = 0
            };

            foreach (var prod in products.Take(2))
            {
                var variant = prod.Variants.FirstOrDefault();
                decimal importPrice = Math.Round(prod.Price * 0.45m / 1000m) * 1000m;
                int qty = 30;
                decimal itemTotal = importPrice * qty;
                r1.ReceiptDetails.Add(new StockReceiptDetail
                {
                    ProductId = prod.Id,
                    ProductVariantId = variant?.Id,
                    ProductName = prod.Name,
                    Size = variant?.Size ?? "Size 100",
                    Color = variant?.Color ?? "Hồng Pastel",
                    Quantity = qty,
                    ImportPrice = importPrice,
                    TotalPrice = itemTotal
                });
                r1.TotalAmount += itemTotal;
            }

            var r2 = new StockReceipt
            {
                ReceiptCode = "PN-20260910-1002",
                SupplierName = "Công Ty TNHH Sợi Cotton Hưng Phát",
                SupplierPhone = "0982334455",
                SupplierAddress = "Số 45 Đường Giải Phóng, Hai Bà Trưng, Hà Nội",
                Note = "Nhập bổ sung set yếm và polo cho bé trai dịp lễ",
                Status = "Completed",
                CreatedBy = "Quản Trị Viên BabyShop",
                CreatedAt = DateTime.Now.AddDays(-1),
                TotalAmount = 0
            };

            foreach (var prod in products.Skip(2).Take(2))
            {
                var variant = prod.Variants.FirstOrDefault();
                decimal importPrice = Math.Round(prod.Price * 0.45m / 1000m) * 1000m;
                int qty = 40;
                decimal itemTotal = importPrice * qty;
                r2.ReceiptDetails.Add(new StockReceiptDetail
                {
                    ProductId = prod.Id,
                    ProductVariantId = variant?.Id,
                    ProductName = prod.Name,
                    Size = variant?.Size ?? "Size 110",
                    Color = variant?.Color ?? "Xanh Baby",
                    Quantity = qty,
                    ImportPrice = importPrice,
                    TotalPrice = itemTotal
                });
                r2.TotalAmount += itemTotal;
            }

            context.StockReceipts.AddRange(r1, r2);
            context.SaveChanges();
        }

        /// <summary>
        /// Chuẩn hóa chuỗi tiếng Việt bị lỗi hiển thị / mojibake
        /// </summary>
        public static string FixVietnameseText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input ?? "";
            string s = input;

            if (s.Contains("Lan Anh") || s.Contains("Ä áº·ng") || s.Contains("Ä\u0090áº·ng"))
                return "Đặng Lan Anh";
            if (s.Contains("Thu H") || s.Contains("Nguyá»…n") || s.Contains("Nguyen Thu Ha"))
                return "Nguyễn Thu Hà";
            if (s.Contains("Minh Anh") || s.Contains("Tráº§n"))
                return "Trần Minh Anh";
            if (s.Contains("Quản Trị") || s.Contains("Quáº£n"))
                return "Quản Trị Viên BabyShop";
            if (s.Contains("Khách Hàng") || s.Contains("KhÃ¡ch"))
                return "Khách Hàng";

            if (s.Contains("88") && (s.Contains("Huáº¿") || s.Contains("Phá»‘") || s.Contains("PhÆ°á»")))
                return "Số 88 Phố Huế, Phường Hàng Bài, Quận Hoàn Kiếm, Hà Nội";
            if (s.Contains("123") && (s.Contains("Cáº§u") || s.Contains("Cau Giay") || s.Contains("Cầu Giấy")))
                return "Số 123 Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội";
            if (s.Contains("209") && (s.Contains("Kim MÃ") || s.Contains("Kim Mã")))
                return "Số 209 Kim Mã, Phường Giảng Võ, Ba Đình, Hà Nội";

            if (s.Contains("giá»") || s.Contains("hÃ nh chÃ­nh") || s.Contains("hành chính"))
                return "Giao trong giờ hành chính";
            if (s.Contains("Ä‘á»‹a") || s.Contains("Ä Æ¡n") || s.Contains("test"))
                return "Đơn hàng kiểm tra hệ thống";

            return s;
        }

        /// <summary>
        /// Tự động làm sạch và chuẩn hóa toàn bộ dữ liệu tiếng Việt có dấu trong CSDL,
        /// sửa triệt để các bản ghi bị lỗi font/mojibake (VD: Ä áº·ng Lan Anh, Sá»‘ 88, Cáº§u Giáº¥y...)
        /// </summary>
        public static void CleanCorruptedData(BabyShopDbContext context)
        {
            try
            {
                // 1. Chuẩn hóa dữ liệu Orders
                var orders = context.Orders.ToList();
                bool orderModified = false;
                foreach (var o in orders)
                {
                    string cleanName = FixVietnameseText(o.CustomerName);
                    if (cleanName != o.CustomerName)
                    {
                        o.CustomerName = cleanName;
                        orderModified = true;
                    }

                    string cleanAddr = FixVietnameseText(o.ShippingAddress);
                    if (cleanAddr != o.ShippingAddress)
                    {
                        o.ShippingAddress = cleanAddr;
                        orderModified = true;
                    }

                    if (!string.IsNullOrEmpty(o.Note))
                    {
                        string cleanNote = FixVietnameseText(o.Note);
                        if (cleanNote != o.Note)
                        {
                            o.Note = cleanNote;
                            orderModified = true;
                        }
                    }
                }

                // 2. Chuẩn hóa dữ liệu Users
                var users = context.Users.ToList();
                bool userModified = false;
                foreach (var u in users)
                {
                    string cleanName = FixVietnameseText(u.FullName);
                    if (cleanName != u.FullName)
                    {
                        u.FullName = cleanName;
                        userModified = true;
                    }

                    if (!string.IsNullOrEmpty(u.Address))
                    {
                        string cleanAddr = FixVietnameseText(u.Address);
                        if (cleanAddr != u.Address)
                        {
                            u.Address = cleanAddr;
                            userModified = true;
                        }
                    }
                }

                // 3. Chuẩn hóa dữ liệu ProductReviews
                try
                {
                    var reviews = context.ProductReviews.ToList();
                    bool reviewModified = false;
                    foreach (var r in reviews)
                    {
                        if (r.CustomerName.Contains("KhÃ¡ch") || r.CustomerName.Contains("Ä"))
                        {
                            r.CustomerName = "Khách Hàng Đã Mua";
                            reviewModified = true;
                        }
                        if (r.Comment.Contains("Sáº£n") || r.Comment.Contains("cháº¥t") || r.Comment.Contains("má»"))
                        {
                            r.Comment = "Sản phẩm chất vải organic rất mềm mát, bé mặc rất thích!";
                            reviewModified = true;
                        }
                    }
                    if (reviewModified) context.SaveChanges();
                }
                catch { }

                // 4. Chuẩn hóa dữ liệu ChatConversations & ChatMessages
                try
                {
                    var chats = context.ChatConversations.ToList();
                    bool chatModified = false;
                    foreach (var c in chats)
                    {
                        string cleanName = FixVietnameseText(c.CustomerName);
                        if (cleanName != c.CustomerName)
                        {
                            c.CustomerName = cleanName;
                            chatModified = true;
                        }
                    }
                    if (chatModified) context.SaveChanges();

                    var msgs = context.ChatMessages.ToList();
                    bool msgModified = false;
                    foreach (var m in msgs)
                    {
                        string cleanName = FixVietnameseText(m.SenderName);
                        if (cleanName != m.SenderName)
                        {
                            m.SenderName = cleanName;
                            msgModified = true;
                        }
                        if (m.Message.Contains("Em chÃ o") || m.Message.Contains("tiáº¿n") || m.Message.Contains("Ä‘Æ¡n"))
                        {
                            m.Message = "Em chào shop ạ! Em cần hỗ trợ kiểm tra tiến độ đơn hàng #BS-20260910-1024 (Người nhận: Nguyễn Thu Hà - SĐT: 0987654321 - Trạng thái: Đang Giao Hàng) ạ.";
                            msgModified = true;
                        }
                    }
                    if (msgModified) context.SaveChanges();
                }
                catch { }

                if (orderModified || userModified)
                {
                    context.SaveChanges();
                }
            }
            catch { }
        }

        private static void SeedVariantImages(BabyShopDbContext context)
        {
            try
            {
                var variants = context.ProductVariants.Include(v => v.Product).ToList();
                bool hasChanges = false;
                foreach (var v in variants)
                {
                    if (string.IsNullOrEmpty(v.ImageUrl))
                    {
                        string colorLower = (v.Color ?? "").ToLower();
                        if (colorLower.Contains("hồng"))
                        {
                            v.ImageUrl = "https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#FFC0CB";
                        }
                        else if (colorLower.Contains("trắng") || colorLower.Contains("kem") || colorLower.Contains("be"))
                        {
                            v.ImageUrl = "https://images.unsplash.com/photo-1518831959646-742c3a14ebf7?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#FFFDD0";
                        }
                        else if (colorLower.Contains("vàng"))
                        {
                            v.ImageUrl = "https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#FFF9A6";
                        }
                        else if (colorLower.Contains("navy") || colorLower.Contains("denim") || colorLower.Contains("bò"))
                        {
                            v.ImageUrl = "https://images.unsplash.com/photo-1503944583220-79d8926ad5e2?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#4682B4";
                        }
                        else if (colorLower.Contains("xanh"))
                        {
                            v.ImageUrl = "https://images.unsplash.com/photo-1522771930-78848d9293e8?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#B0E0E6";
                        }
                        else if (colorLower.Contains("nâu"))
                        {
                            v.ImageUrl = "https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#8B4513";
                        }
                        else
                        {
                            v.ImageUrl = v.Product?.MainImageUrl ?? "https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80";
                            if (string.IsNullOrEmpty(v.ColorHex)) v.ColorHex = "#C57B57";
                        }
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    context.SaveChanges();
                }
            }
            catch { }
        }

        private static void EnsureGroup3And4TablesAndColumns(BabyShopDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.PointTransactions', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[PointTransactions] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [UserId] INT NOT NULL,
                            [OrderId] INT NULL,
                            [PointsChanged] INT NOT NULL,
                            [Type] NVARCHAR(20) NOT NULL,
                            [Description] NVARCHAR(255) NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            CONSTRAINT [PK_PointTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_PointTransactions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_PointTransactions_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE SET NULL
                        );
                        CREATE NONCLUSTERED INDEX [IX_PointTransactions_UserId] ON [dbo].[PointTransactions]([UserId] ASC);
                    END

                    IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[AuditLogs] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [Action] NVARCHAR(50) NOT NULL,
                            [EntityName] NVARCHAR(100) NOT NULL,
                            [EntityId] INT NULL,
                            [Description] NVARCHAR(500) NOT NULL,
                            [OldValues] NVARCHAR(MAX) NULL,
                            [NewValues] NVARCHAR(MAX) NULL,
                            [UserId] INT NULL,
                            [UserName] NVARCHAR(100) NOT NULL DEFAULT (N'Hệ thống'),
                            [IpAddress] NVARCHAR(50) NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
                        );
                        CREATE NONCLUSTERED INDEX [IX_AuditLogs_CreatedAt] ON [dbo].[AuditLogs]([CreatedAt] DESC);
                        CREATE NONCLUSTERED INDEX [IX_AuditLogs_Action] ON [dbo].[AuditLogs]([Action] ASC);
                    END

                    IF COL_LENGTH('dbo.Users', 'RewardPoints') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [RewardPoints] INT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Users', 'TotalSpent') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [TotalSpent] DECIMAL(18, 0) NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Users', 'MemberTier') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [MemberTier] NVARCHAR(50) NOT NULL DEFAULT (N'Thành viên hạng Đồng');
                    END

                    IF COL_LENGTH('dbo.Users', 'TierExpiryDate') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [TierExpiryDate] DATETIME2 NULL;
                    END

                    IF COL_LENGTH('dbo.Orders', 'PointsUsed') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Orders] ADD [PointsUsed] INT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Orders', 'PointDiscount') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Orders] ADD [PointDiscount] DECIMAL(18, 0) NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Orders', 'MemberDiscount') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Orders] ADD [MemberDiscount] DECIMAL(18, 0) NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Products', 'IsFlashSale') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [IsFlashSale] BIT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Products', 'FlashSalePrice') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [FlashSalePrice] DECIMAL(18, 0) NULL;
                    END

                    IF COL_LENGTH('dbo.Products', 'FlashSaleQuantity') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [FlashSaleQuantity] INT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Products', 'FlashSaleSold') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [FlashSaleSold] INT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Products', 'FlashSaleEndTime') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [FlashSaleEndTime] DATETIME2 NULL;
                    END

                    IF COL_LENGTH('dbo.Products', 'IsAddonDeal') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [IsAddonDeal] BIT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Products', 'AddonPrice') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Products] ADD [AddonPrice] DECIMAL(18, 0) NULL;
                    END
                ");
            }
            catch { }
        }

        private static void SeedGroup3And4Data(BabyShopDbContext context)
        {
            try
            {
                // 1. Cập nhật tài khoản khachhang có điểm thưởng mẫu
                var customer = context.Users.FirstOrDefault(u => u.Username == "khachhang");
                if (customer != null && customer.RewardPoints == 0)
                {
                    customer.RewardPoints = 250;
                    customer.TotalSpent = 1250000;
                    customer.MemberTier = "Thành viên hạng Bạc";
                    customer.TierExpiryDate = DateTime.Now.AddYears(1);

                    if (!context.PointTransactions.Any(pt => pt.UserId == customer.Id))
                    {
                        context.PointTransactions.Add(new PointTransaction
                        {
                            UserId = customer.Id,
                            PointsChanged = 125,
                            Type = "Earn",
                            Description = "Tích điểm tự động từ đơn hàng thành công #DH-8801",
                            CreatedAt = DateTime.Now.AddDays(-5)
                        });
                        context.PointTransactions.Add(new PointTransaction
                        {
                            UserId = customer.Id,
                            PointsChanged = 125,
                            Type = "Bonus",
                            Description = "Thưởng chào mừng nâng hạng Thành viên hạng Bạc (Baby Club)",
                            CreatedAt = DateTime.Now.AddDays(-2)
                        });
                    }
                    context.SaveChanges();
                }

                // 2. Cập nhật sản phẩm Flash Sale mẫu
                var flashSaleProds = context.Products.Where(p => p.IsActive).Take(2).ToList();
                if (flashSaleProds.Any() && !context.Products.Any(p => p.IsFlashSale))
                {
                    foreach (var p in flashSaleProds)
                    {
                        p.IsFlashSale = true;
                        p.FlashSalePrice = Math.Round(p.Price * 0.65m / 1000) * 1000; // Giảm 35%
                        p.FlashSaleQuantity = 30;
                        p.FlashSaleSold = 19;
                        p.FlashSaleEndTime = DateTime.Now.AddHours(28); // Còn 28 tiếng
                    }
                    context.SaveChanges();
                }

                // 3. Cập nhật sản phẩm Mua Kèm Deal Sốc
                var addonProds = context.Products.Where(p => p.CategoryId == 4 || p.Name.Contains("Phụ Kiện") || p.Name.Contains("Tất") || p.Name.Contains("Mũ") || p.Price < 150000).Take(3).ToList();
                if (!addonProds.Any())
                {
                    addonProds = context.Products.OrderBy(p => p.Price).Take(3).ToList();
                }
                foreach (var p in addonProds)
                {
                    if (!p.IsAddonDeal)
                    {
                        p.IsAddonDeal = true;
                        p.AddonPrice = Math.Max(9000, Math.Round(p.Price * 0.4m / 1000) * 1000); // Giảm 60%
                    }
                }
                context.SaveChanges();

                // 4. Seed Audit Logs mẫu nếu chưa có
                if (!context.AuditLogs.Any())
                {
                    context.AuditLogs.AddRange(
                        new AuditLog
                        {
                            Action = "Create",
                            EntityName = "Product",
                            EntityId = 1,
                            Description = "Khởi tạo sản phẩm mới \"Váy Công Chúa Voan Xốp Nơ Pastel\"",
                            UserName = "admin",
                            IpAddress = "127.0.0.1",
                            CreatedAt = DateTime.Now.AddDays(-3)
                        },
                        new AuditLog
                        {
                            Action = "Restock",
                            EntityName = "StockReceipt",
                            EntityId = 1,
                            Description = "Tạo phiếu nhập kho NK-20260908-001 (50 sản phẩm)",
                            UserName = "admin",
                            IpAddress = "127.0.0.1",
                            CreatedAt = DateTime.Now.AddDays(-2)
                        },
                        new AuditLog
                        {
                            Action = "Update",
                            EntityName = "Coupon",
                            EntityId = 1,
                            Description = "Cập nhật mã giảm giá HELLOBABY (Giảm 15%)",
                            UserName = "admin",
                            IpAddress = "127.0.0.1",
                            CreatedAt = DateTime.Now.AddDays(-1)
                        },
                        new AuditLog
                        {
                            Action = "ChangeStatus",
                            EntityName = "Order",
                            EntityId = 1,
                            Description = "Chuyển trạng thái đơn hàng sang Hoàn thành (Completed)",
                            UserName = "admin",
                            IpAddress = "127.0.0.1",
                            CreatedAt = DateTime.Now.AddHours(-6)
                        }
                    );
                    context.SaveChanges();
                }
            }
            catch { }
        }

        private static void EnsureDailyCheckInAndLuckyWheel(BabyShopDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF COL_LENGTH('dbo.Users', 'DailyStreakCount') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [DailyStreakCount] INT NOT NULL DEFAULT (0);
                    END

                    IF COL_LENGTH('dbo.Users', 'LastCheckInDate') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [LastCheckInDate] DATE NULL;
                    END

                    IF COL_LENGTH('dbo.Users', 'LuckyWheelSpinsAvailable') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[Users] ADD [LuckyWheelSpinsAvailable] INT NOT NULL DEFAULT (1);
                    END

                    IF OBJECT_ID('dbo.DailyCheckIns', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[DailyCheckIns] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [UserId] INT NOT NULL,
                            [CheckInDate] DATE NOT NULL,
                            [StreakDay] INT NOT NULL DEFAULT (1),
                            [PointsAwarded] INT NOT NULL DEFAULT (10),
                            [SpinsAwarded] INT NOT NULL DEFAULT (0),
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            CONSTRAINT [PK_DailyCheckIns] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_DailyCheckIns_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
                        );
                        CREATE NONCLUSTERED INDEX [IX_DailyCheckIns_UserId_Date] ON [dbo].[DailyCheckIns]([UserId] ASC, [CheckInDate] ASC);
                    END

                    IF OBJECT_ID('dbo.LuckyWheelHistories', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[LuckyWheelHistories] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [UserId] INT NOT NULL,
                            [PrizeName] NVARCHAR(100) NOT NULL,
                            [PrizeType] NVARCHAR(30) NOT NULL,
                            [PointsWon] INT NOT NULL DEFAULT (0),
                            [CouponId] INT NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            CONSTRAINT [PK_LuckyWheelHistories] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_LuckyWheelHistories_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_LuckyWheelHistories_Coupons] FOREIGN KEY ([CouponId]) REFERENCES [dbo].[Coupons]([Id]) ON DELETE SET NULL
                        );
                        CREATE NONCLUSTERED INDEX [IX_LuckyWheelHistories_UserId] ON [dbo].[LuckyWheelHistories]([UserId] ASC);
                    END
                ");

                // Seed các mã voucher đặc biệt dành cho Vòng Quay May Mắn nếu chưa có
                var lucky20k = context.Coupons.FirstOrDefault(c => c.Code == "LUCKY20K");
                if (lucky20k == null)
                {
                    context.Coupons.Add(new Coupon
                    {
                        Code = "LUCKY20K",
                        Title = "Voucher May Mắn 20.000đ",
                        Description = "Phần quà từ Vòng Quay May Mắn Baby Club. Giảm 20.000đ cho đơn từ 150.000đ",
                        DiscountType = DiscountType.FixedAmount,
                        DiscountValue = 20000,
                        MinOrderAmount = 150000,
                        UsageLimit = 10000,
                        IsActive = true,
                        IsForYou = false,
                        StartDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddYears(1)
                    });
                }

                var luckyShip = context.Coupons.FirstOrDefault(c => c.Code == "LUCKYSHIP");
                if (luckyShip == null)
                {
                    context.Coupons.Add(new Coupon
                    {
                        Code = "LUCKYSHIP",
                        Title = "Freeship Vòng Quay 30.000đ",
                        Description = "Phần quà từ Vòng Quay May Mắn Baby Club. Giảm phí ship 30.000đ cho đơn từ 200.000đ",
                        DiscountType = DiscountType.FixedAmount,
                        DiscountValue = 30000,
                        MinOrderAmount = 200000,
                        UsageLimit = 10000,
                        IsActive = true,
                        IsForYou = false,
                        StartDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddYears(1)
                    });
                }

                var lucky50k = context.Coupons.FirstOrDefault(c => c.Code == "LUCKY50K");
                if (lucky50k == null)
                {
                    context.Coupons.Add(new Coupon
                    {
                        Code = "LUCKY50K",
                        Title = "Voucher VIP Vòng Quay 50.000đ",
                        Description = "Phần quà ĐẶC BIỆT từ Vòng Quay May Mắn Baby Club. Giảm 50.000đ cho đơn từ 350.000đ",
                        DiscountType = DiscountType.FixedAmount,
                        DiscountValue = 50000,
                        MinOrderAmount = 350000,
                        UsageLimit = 10000,
                        IsActive = true,
                        IsForYou = false,
                        StartDate = DateTime.Now.AddDays(-1),
                        ExpiryDate = DateTime.Now.AddYears(1)
                    });
                }

                context.SaveChanges();
            }
            catch { }
        }

        private static void EnsureReturnRequestsTableAndSeed(BabyShopDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('dbo.ReturnRequests', 'U') IS NULL
                    BEGIN
                        CREATE TABLE [dbo].[ReturnRequests] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [ReturnCode] NVARCHAR(50) NOT NULL,
                            [OrderId] INT NOT NULL,
                            [UserId] INT NULL,
                            [CustomerName] NVARCHAR(100) NOT NULL,
                            [CustomerPhone] NVARCHAR(20) NOT NULL,
                            [CustomerEmail] NVARCHAR(100) NULL,
                            [PickupAddress] NVARCHAR(255) NULL,
                            [RequestType] NVARCHAR(50) NOT NULL DEFAULT (N'Exchange'),
                            [FaultSource] NVARCHAR(50) NOT NULL DEFAULT (N'ShopFault'),
                            [ReasonCategory] NVARCHAR(150) NOT NULL,
                            [ReasonDetail] NVARCHAR(1000) NULL,
                            [ProductName] NVARCHAR(200) NOT NULL,
                            [CurrentSizeColor] NVARCHAR(100) NULL,
                            [DesiredSizeColor] NVARCHAR(100) NULL,
                            [ProofImages] NVARCHAR(2000) NULL,
                            [UnboxingVideoPath] NVARCHAR(500) NULL,
                            [UnboxingVideoUrl] NVARCHAR(500) NULL,
                            [HadCarrierCoCheck] BIT NOT NULL DEFAULT (0),
                            [CarrierName] NVARCHAR(100) NULL,
                            [CarrierTrackingCode] NVARCHAR(100) NULL,
                            [BankName] NVARCHAR(100) NULL,
                            [BankAccountNo] NVARCHAR(50) NULL,
                            [BankAccountName] NVARCHAR(100) NULL,
                            [Status] NVARCHAR(50) NOT NULL DEFAULT (N'Pending'),
                            [AdminNote] NVARCHAR(1000) NULL,
                            [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            [UpdatedAt] DATETIME2 NOT NULL DEFAULT (GETDATE()),
                            [ProcessedAt] DATETIME2 NULL,
                            CONSTRAINT [PK_ReturnRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_ReturnRequests_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_ReturnRequests_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
                        );
                        CREATE UNIQUE NONCLUSTERED INDEX [IX_ReturnRequests_ReturnCode] ON [dbo].[ReturnRequests]([ReturnCode] ASC);
                    END
                ");

                if (!context.ReturnRequests.Any() && context.Orders.Any())
                {
                    var orders = context.Orders.Include(o => o.OrderDetails).ToList();
                    var order1 = orders.FirstOrDefault();
                    var order2 = orders.Count > 1 ? orders[1] : order1;

                    if (order1 != null)
                    {
                        var sample1 = new ReturnRequest
                        {
                            ReturnCode = "RT-" + DateTime.Now.ToString("yyyyMMdd") + "-8812",
                            OrderId = order1.Id,
                            UserId = order1.UserId,
                            CustomerName = order1.CustomerName,
                            CustomerPhone = order1.CustomerPhone,
                            CustomerEmail = order1.CustomerEmail,
                            PickupAddress = order1.ShippingAddress,
                            RequestType = "Exchange",
                            FaultSource = "ShopFault",
                            ReasonCategory = "Giao nhầm mẫu / sai kích thước so với đơn đặt",
                            ReasonDetail = "Mẹ đặt set đồ size 12-18M nhưng khi mở bưu kiện ra lại là size 6-9M. Bé không mặc vừa, nhờ shop gửi lại đúng size giúp mẹ ạ.",
                            ProductName = order1.OrderDetails.FirstOrDefault()?.ProductName ?? "Set Quần Áo Trẻ Em Cao Cấp",
                            CurrentSizeColor = "Size 6-9M (Giao nhầm)",
                            DesiredSizeColor = "Size 12-18M (Đúng đơn)",
                            ProofImages = "/images/banner-kid.jpg",
                            UnboxingVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                            HadCarrierCoCheck = false,
                            CarrierName = "Viettel Post",
                            CarrierTrackingCode = "VP" + order1.OrderCode,
                            Status = "Pending",
                            CreatedAt = DateTime.Now.AddHours(-3),
                            UpdatedAt = DateTime.Now.AddHours(-3)
                        };

                        var sample2 = new ReturnRequest
                        {
                            ReturnCode = "RT-" + DateTime.Now.AddDays(-1).ToString("yyyyMMdd") + "-6321",
                            OrderId = order2?.Id ?? order1.Id,
                            UserId = order2?.UserId ?? order1.UserId,
                            CustomerName = order2?.CustomerName ?? "Lê Thị Ngọc Lan",
                            CustomerPhone = order2?.CustomerPhone ?? "0934567890",
                            CustomerEmail = order2?.CustomerEmail ?? "ngoclan@gmail.com",
                            PickupAddress = order2?.ShippingAddress ?? "45 Lê Duẩn, Quận 1, TP. Hồ Chí Minh",
                            RequestType = "Exchange",
                            FaultSource = "CarrierFault",
                            ReasonCategory = "Hàng bị vỡ móp / rách bao bì trong lúc vận chuyển",
                            ReasonDetail = "Hộp bưu kiện bên ngoài bị dập nát, ướt sũng do bưu tá đi mưa. Vải áo bên trong bị lem màu nước bẩn. Có video bóc hàng quay rõ tem vận đơn của bưu cục.",
                            ProductName = order2?.OrderDetails.FirstOrDefault()?.ProductName ?? "Váy Công Chúa Voan Xốp Nơ Pastel",
                            CurrentSizeColor = "Size 2-3Y (Hồng)",
                            DesiredSizeColor = "Size 2-3Y (Hồng nguyên vẹn)",
                            ProofImages = "/images/banner-kid.jpg",
                            HadCarrierCoCheck = false,
                            CarrierName = "Giao Hàng Nhanh (GHN)",
                            CarrierTrackingCode = "GHN88291038",
                            Status = "Approved",
                            AdminNote = "Đã duyệt đổi sản phẩm mới ngay cho khách (miễn phí ship). Bộ phận vận hành đã gửi công văn khiếu nại bồi thường 100% đến GHN kèm video bóc hàng của khách.",
                            CreatedAt = DateTime.Now.AddDays(-1),
                            UpdatedAt = DateTime.Now.AddHours(-4)
                        };

                        var sample3 = new ReturnRequest
                        {
                            ReturnCode = "RT-" + DateTime.Now.AddDays(-2).ToString("yyyyMMdd") + "-5419",
                            OrderId = order1.Id,
                            UserId = order1.UserId,
                            CustomerName = "Trần Minh Anh",
                            CustomerPhone = "0977112233",
                            CustomerEmail = "minhanh@gmail.com",
                            PickupAddress = "88 Hoàng Diệu, Đà Nẵng",
                            RequestType = "Exchange",
                            FaultSource = "CustomerRequest",
                            ReasonCategory = "Bé mặc chật/rộng (Đổi size)",
                            ReasonDetail = "Bé nhà mình trộm vía tăng cân nhanh nên mặc size 1Y hơi kích nách, mình muốn nhờ shop hỗ trợ đổi lên size 2Y với ạ.",
                            ProductName = order1.OrderDetails.LastOrDefault()?.ProductName ?? "Bộ Pijama Lụa Mềm Cho Bé",
                            CurrentSizeColor = "Size 1Y (Xanh)",
                            DesiredSizeColor = "Size 2Y (Xanh)",
                            Status = "Completed",
                            AdminNote = "Đã hoàn tất đổi size tận nhà cho mẹ qua bưu tá ViettelPost. Đơn hàng mới đã được giao thành công.",
                            CreatedAt = DateTime.Now.AddDays(-2),
                            UpdatedAt = DateTime.Now.AddDays(-1)
                        };

                        context.ReturnRequests.AddRange(sample1, sample2, sample3);
                        context.SaveChanges();
                    }
                }
            }
            catch { }
        }
    }
}

