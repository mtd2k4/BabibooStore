-- ============================================================================
-- DỰ ÁN: WEBSITE BÁN HÀNG THỜI TRANG TRẺ EM (BABY & KIDS FASHION)
-- HỆ QUẢN TRỊ CƠ SỞ DỮ LIỆU: MICROSOFT SQL SERVER 2019 / 2022 / AZURE SQL
-- TÁC GIẢ: Antigravity AI Pair Programmer
-- ============================================================================

USE master;
GO

-- 1. Tạo Database nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'WebsiteBabyShopDb')
BEGIN
    CREATE DATABASE [WebsiteBabyShopDb];
    PRINT N'==> Đã tạo Cơ sở dữ liệu [WebsiteBabyShopDb] thành công.';
END
GO

USE [WebsiteBabyShopDb];
GO

-- ============================================================================
-- 2. XÓA BẢNG CŨ THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI (NẾU CẦN TẠO LẠI TỪ ĐẦU)
-- ============================================================================
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.ProductImages', 'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL DROP TABLE dbo.ProductVariants;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- ============================================================================
-- 3. BẢNG [Users] - QUẢN LÝ TÀI KHOẢN (ADMIN & KHÁCH HÀNG)
-- ============================================================================
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(50) NOT NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [FullName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    [Address] NVARCHAR(255) NULL,
    [Role] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Users_Role] DEFAULT ('Customer'), -- 'Admin' hoặc 'Customer'
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (GETDATE()),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
    [RewardPoints] INT NOT NULL CONSTRAINT [DF_Users_RewardPoints] DEFAULT (0),
    [TotalSpent] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_Users_TotalSpent] DEFAULT (0),
    [MemberTier] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Users_MemberTier] DEFAULT (N'Thành viên hạng Đồng'),
    [TierExpiryDate] DATETIME2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]([Username] ASC);
CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]([Email] ASC);
GO

-- ============================================================================
-- 4. BẢNG [Categories] - DANH MỤC SẢN PHẨM THỜI TRANG TRẺ EM
-- ============================================================================
CREATE TABLE [dbo].[Categories] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Slug] NVARCHAR(150) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [ImageUrl] NVARCHAR(MAX) NULL,
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_Categories_DisplayOrder] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Categories_IsActive] DEFAULT (1),
    [ParentId] INT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Categories_Parent] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE NO ACTION
);
GO
CREATE NONCLUSTERED INDEX [IX_Categories_ParentId] ON [dbo].[Categories]([ParentId] ASC);
GO

-- ============================================================================
-- 5. BẢNG [Products] - THÔNG TIN MẪU QUẦN ÁO BÉ
-- ============================================================================
CREATE TABLE [dbo].[Products] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Slug] NVARCHAR(250) NULL,
    [Sku] NVARCHAR(50) NULL,
    [ShortDescription] NVARCHAR(500) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Material] NVARCHAR(100) NULL,           -- Chất liệu (Cotton Organic, Voan, Lụa...)
    [CareInstructions] NVARCHAR(MAX) NULL,    -- Hướng dẫn giặt ủi
    [Price] DECIMAL(18, 0) NOT NULL,          -- Giá niêm yết
    [DiscountPrice] DECIMAL(18, 0) NULL,      -- Giá giảm khuyến mãi
    [MainImageUrl] NVARCHAR(MAX) NULL,
    [Gender] NVARCHAR(30) NOT NULL CONSTRAINT [DF_Products_Gender] DEFAULT (N'Bé gái'), -- 'Bé gái', 'Bé trai', 'Sơ sinh', 'Unisex'
    [AgeRange] NVARCHAR(50) NULL,             -- Nhóm tuổi: '0 - 12 tháng', '1 - 5 tuổi', '6 - 10 tuổi'...
    [IsFeatured] BIT NOT NULL CONSTRAINT [DF_Products_IsFeatured] DEFAULT (0),
    [IsNewArrival] BIT NOT NULL CONSTRAINT [DF_Products_IsNewArrival] DEFAULT (1),
    [IsBestSeller] BIT NOT NULL CONSTRAINT [DF_Products_IsBestSeller] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Products_IsActive] DEFAULT (1),
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Products_CreatedAt] DEFAULT (GETDATE()),
    [CategoryId] INT NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE NO ACTION
);
GO
CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId] ASC);
CREATE NONCLUSTERED INDEX [IX_Products_Gender] ON [dbo].[Products]([Gender] ASC);
GO

-- ============================================================================
-- 6. BẢNG [ProductVariants] - BIẾN THỂ KÍCH THƯỚC (SIZE) & MÀU SẮC
-- ============================================================================
CREATE TABLE [dbo].[ProductVariants] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProductId] INT NOT NULL,
    [Size] NVARCHAR(50) NOT NULL,         -- Size: '0-3M', '3-6M', 'Size 90', 'Size 100', 'Size 110'...
    [Color] NVARCHAR(50) NOT NULL,        -- Màu: 'Hồng Pastel', 'Xanh Baby', 'Trắng Kem'...
    [ColorHex] NVARCHAR(20) NULL,         -- Mã màu: '#FFC0CB', '#B0E0E6'...
    [StockQuantity] INT NOT NULL CONSTRAINT [DF_ProductVariants_Stock] DEFAULT (10),
    [Sku] NVARCHAR(50) NULL,
    CONSTRAINT [PK_ProductVariants] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProductVariants_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
);
GO
CREATE NONCLUSTERED INDEX [IX_ProductVariants_ProductId] ON [dbo].[ProductVariants]([ProductId] ASC);
GO

-- ============================================================================
-- 7. BẢNG [ProductImages] - THƯ VIỆN ẢNH SẢN PHẨM
-- ============================================================================
CREATE TABLE [dbo].[ProductImages] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProductId] INT NOT NULL,
    [ImageUrl] NVARCHAR(MAX) NOT NULL,
    [IsMain] BIT NOT NULL CONSTRAINT [DF_ProductImages_IsMain] DEFAULT (0),
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_ProductImages_DisplayOrder] DEFAULT (0),
    CONSTRAINT [PK_ProductImages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProductImages_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
);
GO
CREATE NONCLUSTERED INDEX [IX_ProductImages_ProductId] ON [dbo].[ProductImages]([ProductId] ASC);
GO

-- ============================================================================
-- 8. BẢNG [Orders] - QUẢN LÝ ĐƠN HÀNG
-- ============================================================================
CREATE TABLE [dbo].[Orders] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OrderCode] NVARCHAR(50) NOT NULL,
    [CustomerName] NVARCHAR(100) NOT NULL,
    [CustomerPhone] NVARCHAR(20) NOT NULL,
    [CustomerEmail] NVARCHAR(100) NULL,
    [ShippingAddress] NVARCHAR(255) NOT NULL,
    [Note] NVARCHAR(500) NULL,
    [SubTotal] DECIMAL(18, 0) NOT NULL,
    [CouponCode] NVARCHAR(50) NULL,
    [DiscountAmount] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_Orders_DiscountAmount] DEFAULT (0),
    [MemberDiscount] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_Orders_MemberDiscount] DEFAULT (0),
    [PointsUsed] INT NOT NULL CONSTRAINT [DF_Orders_PointsUsed] DEFAULT (0),
    [PointDiscount] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_Orders_PointDiscount] DEFAULT (0),
    [ShippingFee] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_Orders_ShippingFee] DEFAULT (30000),
    [TotalAmount] DECIMAL(18, 0) NOT NULL,
    [PaymentMethod] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Orders_PaymentMethod] DEFAULT ('COD'), -- 'COD', 'BankTransfer'
    [IsPaid] BIT NOT NULL CONSTRAINT [DF_Orders_IsPaid] DEFAULT (0),
    [OrderStatus] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Orders_OrderStatus] DEFAULT ('Pending'), -- 'Pending', 'Processing', 'Shipping', 'Completed', 'Cancelled'
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Orders_CreatedAt] DEFAULT (GETDATE()),
    [UserId] INT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
GO
CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [dbo].[Orders]([UserId] ASC);
CREATE NONCLUSTERED INDEX [IX_Orders_OrderStatus] ON [dbo].[Orders]([OrderStatus] ASC);
GO

-- ============================================================================
-- 9. BẢNG [OrderDetails] - CHI TIẾT SẢN PHẨM TRONG TỪNG ĐƠN HÀNG
-- ============================================================================
CREATE TABLE [dbo].[OrderDetails] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [ProductVariantId] INT NULL,
    [ProductName] NVARCHAR(200) NOT NULL,
    [ProductImage] NVARCHAR(MAX) NULL,
    [Size] NVARCHAR(50) NULL,
    [Color] NVARCHAR(50) NULL,
    [UnitPrice] DECIMAL(18, 0) NOT NULL,
    [Quantity] INT NOT NULL CONSTRAINT [DF_OrderDetails_Quantity] DEFAULT (1),
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_OrderDetails_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION
);
GO
CREATE NONCLUSTERED INDEX [IX_OrderDetails_OrderId] ON [dbo].[OrderDetails]([OrderId] ASC);
CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductId] ON [dbo].[OrderDetails]([ProductId] ASC);
GO

-- ============================================================================
-- 10. ĐĂNG KÝ BẢNG LỊCH SỬ MIGRATION CHO ENTITY FRAMEWORK CORE
-- ============================================================================
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId] ASC)
    );
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910134050_InitialCreate', N'8.0.11');
END
GO

-- ============================================================================
-- 11. DỮ LIỆU MẪU BAN ĐẦU (SEED DATA CHUẨN XÁC & ĐẦY ĐỦ)
-- ============================================================================

-- A. Tài khoản mẫu
-- Mật khẩu admin: Admin@123456 -> Hash SHA256 kèm salt bí mật
-- Mật khẩu khachhang: 123456 -> Hash SHA256 kèm salt bí mật
SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] ([Id], [Username], [PasswordHash], [FullName], [Email], [PhoneNumber], [Address], [Role], [CreatedAt], [IsActive])
VALUES 
(1, N'admin', N'YZovgFDpVqitoW0TXoaT5tfW5RSZIGYBPjQVuDlQ88M=', N'Quản Trị Viên BabyShop', N'admin@babyshop.vn', N'0988889999', N'Hà Nội, Việt Nam', N'Admin', GETDATE(), 1),
(2, N'khachhang', N'ewNAXAsuiPDYupvyLZclcX+ur3jkA+kdlr1dBZ9R60g=', N'Nguyễn Thu Hà', N'thuha@gmail.com', N'0912345678', N'123 Cầu Giấy, Hà Nội', N'Customer', GETDATE(), 1);
SET IDENTITY_INSERT [dbo].[Users] OFF;
GO

-- B. Danh mục thời trang bé
SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [ImageUrl], [DisplayOrder], [IsActive], [ParentId])
VALUES 
(1, N'Thời Trang Bé Gái', N'thoi-trang-be-gai', N'Váy đầm công chúa, set đồ xinh xắn, áo kiểu và chân váy thời thượng cho bé gái', N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=600&q=80', 1, 1, NULL),
(2, N'Thời Trang Bé Trai', N'thoi-trang-be-trai', N'Set công tử, áo thun polo năng động, quần jean và yếm sành điệu cho bé trai', N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=600&q=80', 2, 1, NULL),
(3, N'Thời Trang Sơ Sinh (0-24M)', N'thoi-trang-so-sinh', N'Bodysuit hữu cơ, romper mềm mại, bộ dài tay 100% cotton organic bảo vệ làn da non nớt', N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=600&q=80', 3, 1, NULL),
(4, N'Phụ Kiện & Giày Dép', N'phu-kien-giay-dep', N'Mũ nón, kính râm, nơ kẹp tóc, vớ tất và giày tập đi êm chân chống trượt', N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=600&q=80', 4, 1, NULL);
SET IDENTITY_INSERT [dbo].[Categories] OFF;
GO

-- C. Sản phẩm mẫu
SET IDENTITY_INSERT [dbo].[Products] ON;
INSERT INTO [dbo].[Products] 
([Id], [Name], [Slug], [Sku], [ShortDescription], [Description], [Material], [CareInstructions], [Price], [DiscountPrice], [MainImageUrl], [Gender], [AgeRange], [IsFeatured], [IsNewArrival], [IsBestSeller], [IsActive], [CreatedAt], [CategoryId])
VALUES 
(1, N'Váy Công Chúa Voan Xốp Nơ Pastel', N'vay-cong-chua-voan-xop-no-pastel', N'VBG-001', 
 N'Váy công chúa chất voan dệt xốp bồng bềnh, phối nơ ngọt ngào cho bé dự tiệc, sinh nhật.', 
 N'<p>Chiếc đầm công chúa làm từ chất liệu voan dệt xốp cao cấp, lớp lót trong 100% cotton mềm mịn.</p>', 
 N'Voan xốp cao cấp, lót cotton 100%', N'Nên giặt tay nhẹ nhàng, ủi nhiệt độ thấp.', 320000, 259000, 
 N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80', N'Bé gái', N'1 - 6 tuổi', 1, 1, 1, 1, GETDATE(), 1),

(2, N'Đầm Hoa Nhí Cổ Sen Phong Cách Vintage', N'dam-hoa-nhi-co-sen-vintage', N'VBG-002', 
 N'Đầm suông vải thô đũi organic thoáng mát in họa tiết hoa nhí phong cách Hàn Quốc dịu dàng.', 
 N'<p>Đầm hoa nhí cổ bèo sen mang phong cách vintage nhẹ nhàng, đằm thắm.</p>', 
 N'Vải thô đũi Cotton Organic', N'Giặt máy ở chế độ nhẹ nhàng.', 280000, 219000, 
 N'https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80', N'Bé gái', N'2 - 7 tuổi', 1, 0, 1, 1, GETDATE(), 1),

(3, N'Áo Len Cardigan Dệt Kim Họa Tiết Quả Trám', N'ao-len-cardigan-det-kim-hoa-tiet-qua-tram', N'VBG-003', 
 N'Áo cardigan len dệt kim sợi mềm cao cấp, ấm áp và dễ phối với mọi loại váy áo cho bé.', 
 N'<p>Áo khoác len cardigan dệt sợi bông tự nhiên giữ ấm hiệu quả, co giãn đàn hồi tốt.</p>', 
 N'Len lông cừu pha sợi Cotton tự nhiên', N'Nên giặt tay, vắt nhẹ.', 350000, 289000, 
 N'https://images.unsplash.com/photo-1518831959646-742c3a14ebf7?auto=format&fit=crop&w=800&q=80', N'Bé gái', N'1 - 6 tuổi', 0, 1, 0, 1, GETDATE(), 1),

(4, N'Set Quần Yếm Denim Phong Cách Hàn Quốc Bé Trai', N'set-quan-yem-denim-han-quoc-be-trai', N'VBT-001', 
 N'Set yếm bò jean mềm co giãn kèm áo thun trắng cotton cho bé trai năng động, bảnh bao.', 
 N'<p>Bộ yếm bò thời trang được các mẹ yêu thích nhất cho bé trai. Kèm áo phông trắng 100% cotton.</p>', 
 N'Denim wash mềm + Thun cotton 4 chiều', N'Giặt riêng với đồ sáng màu.', 360000, 299000, 
 N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80', N'Bé trai', N'1 - 5 tuổi', 1, 1, 1, 1, GETDATE(), 2),

(5, N'Áo Thun Polo Kẻ Ngang Thể Thao Năng Động', N'ao-thun-polo-ke-ngang-nang-dong', N'VBT-002', 
 N'Áo polo cổ bẻ phối sọc ngang thời thượng, phong cách quý tử nhí lịch thiệp nhưng năng động.', 
 N'<p>Chất cá sấu cotton co giãn thấm hút mồ hôi cực tốt. Thiết kế cổ bẻ cứng cáp giữ form chuẩn.</p>', 
 N'Vải Pique cá sấu 100% Cotton', N'Giặt máy bình thường.', 195000, 159000, 
 N'https://images.unsplash.com/photo-1503944583220-79d8926ad5e2?auto=format&fit=crop&w=800&q=80', N'Bé trai', N'2 - 8 tuổi', 1, 0, 1, 1, GETDATE(), 2),

(6, N'Bộ Bodysuit Romper Cotton Organic Xuất Hàn Sơ Sinh', N'bo-bodysuit-romper-cotton-organic-xuat-han', N'BSS-001', 
 N'Chất liệu cotton hữu cơ kháng khuẩn, đường may phẳng lộn ngoài chống xước da bé sơ sinh.', 
 N'<p>Sản phẩm dành riêng cho làn da non nớt của các thiên thần nhỏ. 100% sợi cotton hữu cơ an toàn Oeko-Tex.</p>', 
 N'100% Cotton Organic nguyên chất', N'Giặt nhẹ với nước giặt em bé.', 210000, 165000, 
 N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80', N'Sơ sinh', N'0 - 12 tháng', 1, 1, 1, 1, GETDATE(), 3),

(7, N'Set Bộ Dài Tay Cài Lệch Bamboo Kháng Khuẩn', N'set-bo-dai-tay-cai-lech-bamboo', N'BSS-002', 
 N'Vải sợi tre Bamboo cao cấp mềm mướt, điều hòa thân nhiệt cực tốt cho bé nằm điều hòa.', 
 N'<p>Sợi tre thiên nhiên có khả năng kháng khuẩn tự nhiên, kiểm soát mùi và điều hòa thân nhiệt thông minh.</p>', 
 N'Sợi tre Bamboo 95% + Spandex 5%', N'Giặt nước thường, phơi bóng râm.', 260000, 205000, 
 N'https://images.unsplash.com/photo-1555252333-9f8e92e65df9?auto=format&fit=crop&w=800&q=80', N'Sơ sinh', N'0 - 18 tháng', 1, 0, 1, 1, GETDATE(), 3),

(8, N'Giày Tập Đi Da Mềm Quai Dán Chống Trượt', N'giay-tap-di-da-mem-quai-dan-chong-truot', N'PK-001', 
 N'Đế cao su nhiệt dẻo mềm uốn cong 1/3, hỗ trợ bé tập những bước đi đầu đời an toàn.', 
 N'<p>Giày tập đi chuẩn y khoa cho bé từ 6 tháng đến 2 tuổi. Mũi giày rộng rãi không chèn ép ngón chân.</p>', 
 N'Da PU cao cấp vi sinh + Đế cao su dẻo', N'Lau sạch bằng khăn ẩm.', 220000, 175000, 
 N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=800&q=80', N'Unisex', N'6 - 24 tháng', 1, 1, 1, 1, GETDATE(), 4);
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

-- D. Biến thể Size & Màu sắc cho trang phục bé
INSERT INTO [dbo].[ProductVariants] ([ProductId], [Size], [Color], [ColorHex], [StockQuantity], [Sku])
VALUES 
-- Váy công chúa
(1, N'1 - 2 tuổi (9 - 11kg)', N'Hồng Pastel', N'#FFC0CB', 20, N'VBG-001-P1'),
(1, N'2 - 3 tuổi (12 - 14kg)', N'Hồng Pastel', N'#FFC0CB', 15, N'VBG-001-P2'),
(1, N'3 - 4 tuổi (15 - 17kg)', N'Hồng Pastel', N'#FFC0CB', 12, N'VBG-001-P3'),
(1, N'1 - 2 tuổi (9 - 11kg)', N'Trắng Kem', N'#FFFDD0', 15, N'VBG-001-W1'),
-- Đầm hoa nhí
(2, N'Size 90 (2 - 3T, 11-13kg)', N'Vàng Nhạt', N'#FFF9A6', 18, N'VBG-002-Y90'),
(2, N'Size 100 (3 - 4T, 14-16kg)', N'Vàng Nhạt', N'#FFF9A6', 22, N'VBG-002-Y100'),
(2, N'Size 110 (4 - 5T, 17-19kg)', N'Vàng Nhạt', N'#FFF9A6', 14, N'VBG-002-Y110'),
-- Set yếm bé trai
(4, N'Size 90 (9 - 11kg)', N'Xanh Denim', N'#4682B4', 15, N'VBT-001-D90'),
(4, N'Size 100 (12 - 14kg)', N'Xanh Denim', N'#4682B4', 20, N'VBT-001-D100'),
(4, N'Size 110 (15 - 17kg)', N'Xanh Denim', N'#4682B4', 18, N'VBT-001-D110'),
-- Polo bé trai
(5, N'Size 100 (2 - 3T, 12-14kg)', N'Xanh Navy', N'#000080', 25, N'VBT-002-NV1'),
(5, N'Size 110 (3 - 4T, 15-17kg)', N'Xanh Navy', N'#000080', 20, N'VBT-002-NV2'),
-- Bodysuit sơ sinh
(6, N'0 - 3 tháng (3 - 5kg)', N'Vàng Kem Nhạt', N'#FFFDD0', 30, N'BSS-001-03'),
(6, N'3 - 6 tháng (5 - 7kg)', N'Vàng Kem Nhạt', N'#FFFDD0', 25, N'BSS-001-36'),
(6, N'6 - 9 tháng (7 - 9kg)', N'Vàng Kem Nhạt', N'#FFFDD0', 20, N'BSS-001-69'),
-- Giày tập đi
(8, N'Size 11 (Chân 10.5cm, 6-9M)', N'Nâu Bò Vintage', N'#8B4513', 15, N'PK-001-11'),
(8, N'Size 12 (Chân 11.5cm, 9-12M)', N'Nâu Bò Vintage', N'#8B4513', 18, N'PK-001-12');
GO

-- ============================================================================
-- 10. BẢNG [ChatConversations] & [ChatMessages] - QUẢN LÝ LIVE CHAT CSKH
-- ============================================================================
IF OBJECT_ID('dbo.ChatMessages', 'U') IS NOT NULL DROP TABLE dbo.ChatMessages;
IF OBJECT_ID('dbo.ChatConversations', 'U') IS NOT NULL DROP TABLE dbo.ChatConversations;
GO

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
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ChatConversations_SessionId] ON [dbo].[ChatConversations]([SessionId] ASC);
GO

CREATE TABLE [dbo].[ChatMessages] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ConversationId] INT NOT NULL,
    [Sender] NVARCHAR(20) NOT NULL, -- 'Customer' or 'Staff'
    [SenderName] NVARCHAR(100) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAt] DEFAULT (GETDATE()),
    [IsRead] BIT NOT NULL CONSTRAINT [DF_ChatMessages_IsRead] DEFAULT (0),
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ChatMessages_ChatConversations] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[ChatConversations]([Id]) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 11. BẢNG [UserCoupons] - KHO VOUCHER CỦA TỪNG TÀI KHOẢN (GIỚI HẠN 1 LẦN / USER)
-- ============================================================================
IF OBJECT_ID('dbo.UserCoupons', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserCoupons] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [CouponId] INT NOT NULL,
        [CollectedAt] DATETIME2 NOT NULL CONSTRAINT [DF_UserCoupons_CollectedAt] DEFAULT (GETDATE()),
        [ExpiryDate] DATETIME2 NOT NULL,
        [IsUsed] BIT NOT NULL CONSTRAINT [DF_UserCoupons_IsUsed] DEFAULT (0),
        [UsedAt] DATETIME2 NULL,
        [OrderId] INT NULL,
        CONSTRAINT [PK_UserCoupons] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserCoupons_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserCoupons_Coupons] FOREIGN KEY ([CouponId]) REFERENCES [dbo].[Coupons]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserCoupons_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserCoupons_UserId_CouponId] ON [dbo].[UserCoupons]([UserId] ASC, [CouponId] ASC);
END
GO

-- ============================================================================
-- 12. CỘT MỚI: TÍNH NĂNG "DÀNH CHO BẠN" (VOUCHER ĐỘC QUYỀN 12H THEO TÀI KHOẢN)
-- ============================================================================
IF COL_LENGTH('dbo.Coupons', 'IsForYou') IS NULL
BEGIN
    ALTER TABLE [dbo].[Coupons] ADD [IsForYou] BIT NOT NULL CONSTRAINT [DF_Coupons_IsForYou] DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Coupons', 'AssignedUserId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Coupons] ADD [AssignedUserId] INT NULL CONSTRAINT [FK_Coupons_AssignedUser] FOREIGN KEY REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'LastForYouVoucherAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [LastForYouVoucherAt] DATETIME2 NULL;
END
GO

-- ============================================================================
-- 13. CỘT MỚI: TÍNH NĂNG "SĂN MÃ GIẢM GIÁ" (FLASH COUPON HUNT)
-- ============================================================================
IF COL_LENGTH('dbo.Coupons', 'IsFlashHunt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Coupons] ADD [IsFlashHunt] BIT NOT NULL CONSTRAINT [DF_Coupons_IsFlashHunt] DEFAULT (0);
END
GO

PRINT N'============================================================================';
PRINT N'==> KHỞI TẠO CƠ SỞ DỮ LIỆU [WebsiteBabyShopDb] VÀ NẠP DỮ LIỆU THÀNH CÔNG!   <==';
PRINT N'============================================================================';
GO

