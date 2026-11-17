-- ============================================================================
-- DỰ ÁN: WEBSITE BÁN HÀNG THỜI TRANG TRẺ EM (BABY & KIDS FASHION - BABIBOO STORE)
-- HỆ QUẢN TRỊ CƠ SỞ DỮ LIỆU: MICROSOFT SQL SERVER 2019 / 2022 / AZURE SQL
-- ĐỒNG BỘ HOÀN HẢO VỚI: Entity Framework Core 8.0 & DbInitializer.cs
-- TÁC GIẢ: Antigravity AI Pair Programmer
-- NGÀY CẬP NHẬT: 2026-09-11
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
-- 2. XÓA BẢNG CŨ THEO THỨ TỰ RÀNG BUỘC KHÓA NGOẠI (CLEAN DATABASE RESET)
-- ============================================================================
IF OBJECT_ID('dbo.BankAccounts', 'U') IS NOT NULL DROP TABLE dbo.BankAccounts;
IF OBJECT_ID('dbo.ReturnRequests', 'U') IS NOT NULL DROP TABLE dbo.ReturnRequests;
IF OBJECT_ID('dbo.LuckyWheelHistories', 'U') IS NOT NULL DROP TABLE dbo.LuckyWheelHistories;
IF OBJECT_ID('dbo.DailyCheckIns', 'U') IS NOT NULL DROP TABLE dbo.DailyCheckIns;
IF OBJECT_ID('dbo.UserCoupons', 'U') IS NOT NULL DROP TABLE dbo.UserCoupons;
IF OBJECT_ID('dbo.PointTransactions', 'U') IS NOT NULL DROP TABLE dbo.PointTransactions;
IF OBJECT_ID('dbo.ProductReviews', 'U') IS NOT NULL DROP TABLE dbo.ProductReviews;
IF OBJECT_ID('dbo.StockReceiptDetails', 'U') IS NOT NULL DROP TABLE dbo.StockReceiptDetails;
IF OBJECT_ID('dbo.StockReceipts', 'U') IS NOT NULL DROP TABLE dbo.StockReceipts;
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.ChatMessages', 'U') IS NOT NULL DROP TABLE dbo.ChatMessages;
IF OBJECT_ID('dbo.ChatConversations', 'U') IS NOT NULL DROP TABLE dbo.ChatConversations;
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL DROP TABLE dbo.ProductVariants;
IF OBJECT_ID('dbo.ProductImages', 'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Coupons', 'U') IS NOT NULL DROP TABLE dbo.Coupons;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- ============================================================================
-- 3. BẢNG [Users] - QUẢN LÝ TÀI KHOẢN (ADMIN, KHÁCH HÀNG & BABY CLUB)
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
    [LastForYouVoucherAt] DATETIME2 NULL,
    [DailyStreakCount] INT NOT NULL CONSTRAINT [DF_Users_DailyStreakCount] DEFAULT (0),
    [LastCheckInDate] DATE NULL,
    [LuckyWheelSpinsAvailable] INT NOT NULL CONSTRAINT [DF_Users_LuckyWheelSpinsAvailable] DEFAULT (1),
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]([Username] ASC);
CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]([Email] ASC);
GO

-- ============================================================================
-- 4. BẢNG [Categories] - DANH MỤC THỜI TRANG TRẺ EM
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
-- 5. BẢNG [Products] - MẪU QUẦN ÁO, FLASH SALE & MUA KÈM DEAL SỐC
-- ============================================================================
CREATE TABLE [dbo].[Products] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Slug] NVARCHAR(250) NULL,
    [Sku] NVARCHAR(50) NULL,
    [ShortDescription] NVARCHAR(500) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Material] NVARCHAR(100) NULL,           -- Chất liệu (Cotton Organic, Voan, Sợi tre Bamboo...)
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
    [IsFlashSale] BIT NOT NULL CONSTRAINT [DF_Products_IsFlashSale] DEFAULT (0),
    [FlashSalePrice] DECIMAL(18, 0) NULL,
    [FlashSaleQuantity] INT NOT NULL CONSTRAINT [DF_Products_FlashSaleQuantity] DEFAULT (0),
    [FlashSaleSold] INT NOT NULL CONSTRAINT [DF_Products_FlashSaleSold] DEFAULT (0),
    [FlashSaleEndTime] DATETIME2 NULL,
    [IsAddonDeal] BIT NOT NULL CONSTRAINT [DF_Products_IsAddonDeal] DEFAULT (0),
    [AddonPrice] DECIMAL(18, 0) NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE NO ACTION
);
GO
CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId] ASC);
CREATE NONCLUSTERED INDEX [IX_Products_Gender] ON [dbo].[Products]([Gender] ASC);
CREATE NONCLUSTERED INDEX [IX_Products_IsFlashSale] ON [dbo].[Products]([IsFlashSale] ASC);
GO

-- ============================================================================
-- 6. BẢNG [ProductVariants] - BIẾN THỂ SIZE, MÀU SẮC, TỒN KHO & ẢNH RIÊNG
-- ============================================================================
CREATE TABLE [dbo].[ProductVariants] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProductId] INT NOT NULL,
    [Size] NVARCHAR(50) NOT NULL,         -- Size: '0-3M', '3-6M', 'Size 90', 'Size 100', 'Size 110'...
    [Color] NVARCHAR(50) NOT NULL,        -- Màu: 'Hồng Pastel', 'Xanh Baby', 'Trắng Kem'...
    [ColorHex] NVARCHAR(20) NULL,         -- Mã màu: '#FFC0CB', '#4682B4'...
    [StockQuantity] INT NOT NULL CONSTRAINT [DF_ProductVariants_Stock] DEFAULT (10),
    [Sku] NVARCHAR(50) NULL,
    [ImageUrl] NVARCHAR(500) NULL,
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
-- 8. BẢNG [Coupons] - MÃ GIẢM GIÁ, DÀNH CHO BẠN & SĂN GIỜ VÀNG
-- ============================================================================
CREATE TABLE [dbo].[Coupons] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DiscountType] INT NOT NULL CONSTRAINT [DF_Coupons_DiscountType] DEFAULT (0), -- 0: Percentage, 1: FixedAmount
    [DiscountValue] DECIMAL(18, 0) NOT NULL,
    [MinOrderAmount] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_Coupons_MinOrderAmount] DEFAULT (0),
    [MaxDiscountAmount] DECIMAL(18, 0) NULL,
    [UsageLimit] INT NULL CONSTRAINT [DF_Coupons_UsageLimit] DEFAULT (1000),
    [UsedCount] INT NOT NULL CONSTRAINT [DF_Coupons_UsedCount] DEFAULT (0),
    [StartDate] DATETIME2 NOT NULL CONSTRAINT [DF_Coupons_StartDate] DEFAULT (GETDATE()),
    [ExpiryDate] DATETIME2 NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Coupons_IsActive] DEFAULT (1),
    [IsForYou] BIT NOT NULL CONSTRAINT [DF_Coupons_IsForYou] DEFAULT (0),
    [IsFlashHunt] BIT NOT NULL CONSTRAINT [DF_Coupons_IsFlashHunt] DEFAULT (0),
    [AssignedUserId] INT NULL,
    CONSTRAINT [PK_Coupons] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Coupons_AssignedUser] FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Coupons_Code] ON [dbo].[Coupons]([Code] ASC);
GO

-- ============================================================================
-- 9. BẢNG [Orders] - QUẢN LÝ ĐƠN HÀNG, KHUYẾN MÃI & CHIẾT KHẤU ĐIỂM
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
-- 10. BẢNG [OrderDetails] - CHI TIẾT SẢN PHẨM TRONG ĐƠN HÀNG
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
-- 11. BẢNG [UserCoupons] - KHO VOUCHER CỦA KHÁCH HÀNG (GIỚI HẠN 1 LẦN / TÀI KHOẢN)
-- ============================================================================
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
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_UserCoupons_UserId_CouponId] ON [dbo].[UserCoupons]([UserId] ASC, [CouponId] ASC);
GO

-- ============================================================================
-- 12. BẢNG [ProductReviews] - ĐÁNH GIÁ SẢN PHẨM TỪ CÁC MẸ (CÓ DUYỆT & XÁC THỰC)
-- ============================================================================
CREATE TABLE [dbo].[ProductReviews] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ProductId] INT NOT NULL,
    [UserId] INT NULL,
    [CustomerName] NVARCHAR(100) NOT NULL,
    [CustomerPhone] NVARCHAR(20) NULL,
    [Rating] INT NOT NULL CONSTRAINT [DF_ProductReviews_Rating] DEFAULT (5),
    [Comment] NVARCHAR(1000) NOT NULL,
    [BabyInfo] NVARCHAR(100) NULL,             -- Ví dụ: "Bé gái 18 tháng • 11.5 kg"
    [IsVerifiedPurchase] BIT NOT NULL CONSTRAINT [DF_ProductReviews_IsVerifiedPurchase] DEFAULT (1),
    [IsApproved] BIT NOT NULL CONSTRAINT [DF_ProductReviews_IsApproved] DEFAULT (1),
    [HelpfulCount] INT NOT NULL CONSTRAINT [DF_ProductReviews_HelpfulCount] DEFAULT (0),
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ProductReviews_CreatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_ProductReviews] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProductReviews_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductReviews_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
GO
CREATE NONCLUSTERED INDEX [IX_ProductReviews_ProductId] ON [dbo].[ProductReviews]([ProductId] ASC);
GO

-- ============================================================================
-- 13. BẢNG [StockReceipts] - PHIẾU NHẬP KHO NGUYÊN KIỆN / NHÀ CUNG CẤP
-- ============================================================================
CREATE TABLE [dbo].[StockReceipts] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ReceiptCode] NVARCHAR(50) NOT NULL,
    [SupplierName] NVARCHAR(200) NOT NULL,
    [SupplierPhone] NVARCHAR(20) NULL,
    [SupplierAddress] NVARCHAR(255) NULL,
    [Note] NVARCHAR(500) NULL,
    [TotalAmount] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_StockReceipts_TotalAmount] DEFAULT (0),
    [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_StockReceipts_Status] DEFAULT (N'Completed'), -- 'Completed', 'Pending', 'Cancelled'
    [CreatedBy] NVARCHAR(100) NULL CONSTRAINT [DF_StockReceipts_CreatedBy] DEFAULT (N'Admin'),
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_StockReceipts_CreatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_StockReceipts] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_StockReceipts_ReceiptCode] ON [dbo].[StockReceipts]([ReceiptCode] ASC);
GO

-- ============================================================================
-- 14. BẢNG [StockReceiptDetails] - CHI TIẾT SỐ LƯỢNG & GIÁ NHẬP TỪNG BIẾN THỂ
-- ============================================================================
CREATE TABLE [dbo].[StockReceiptDetails] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [StockReceiptId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [ProductVariantId] INT NULL,
    [ProductName] NVARCHAR(200) NOT NULL,
    [Size] NVARCHAR(50) NULL,
    [Color] NVARCHAR(50) NULL,
    [Quantity] INT NOT NULL CONSTRAINT [DF_StockReceiptDetails_Quantity] DEFAULT (1),
    [ImportPrice] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_StockReceiptDetails_ImportPrice] DEFAULT (0),
    [TotalPrice] DECIMAL(18, 0) NOT NULL CONSTRAINT [DF_StockReceiptDetails_TotalPrice] DEFAULT (0),
    CONSTRAINT [PK_StockReceiptDetails] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_StockReceiptDetails_StockReceipts] FOREIGN KEY ([StockReceiptId]) REFERENCES [dbo].[StockReceipts]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockReceiptDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION
);
GO
CREATE NONCLUSTERED INDEX [IX_StockReceiptDetails_StockReceiptId] ON [dbo].[StockReceiptDetails]([StockReceiptId] ASC);
GO

-- ============================================================================
-- 15. BẢNG [PointTransactions] - LỊCH SỬ TÍCH / TIÊU ĐIỂM THƯỞNG BABY CLUB
-- ============================================================================
CREATE TABLE [dbo].[PointTransactions] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [OrderId] INT NULL,
    [PointsChanged] INT NOT NULL,
    [Type] NVARCHAR(20) NOT NULL,              -- 'Earn', 'Spend', 'Bonus'
    [Description] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_PointTransactions_CreatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_PointTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PointTransactions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PointTransactions_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE SET NULL
);
GO
CREATE NONCLUSTERED INDEX [IX_PointTransactions_UserId] ON [dbo].[PointTransactions]([UserId] ASC);
GO

-- ============================================================================
-- 16. BẢNG [AuditLogs] - NHẬT KÝ KIỂM TOÁN HỆ THỐNG QUẢN TRỊ ADMIN
-- ============================================================================
CREATE TABLE [dbo].[AuditLogs] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,           -- 'Create', 'Update', 'Delete', 'Restock', 'ChangeStatus'
    [EntityName] NVARCHAR(100) NOT NULL,      -- 'Product', 'Order', 'StockReceipt', 'Coupon'
    [EntityId] INT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [UserId] INT NULL,
    [UserName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_AuditLogs_UserName] DEFAULT (N'Hệ thống'),
    [IpAddress] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AuditLogs_CreatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO
CREATE NONCLUSTERED INDEX [IX_AuditLogs_CreatedAt] ON [dbo].[AuditLogs]([CreatedAt] DESC);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Action] ON [dbo].[AuditLogs]([Action] ASC);
GO

-- ============================================================================
-- 17. BẢNG [ChatConversations] & [ChatMessages] - LIVE CHAT HỖ TRỢ KHÁCH HÀNG
-- ============================================================================
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
    [Sender] NVARCHAR(20) NOT NULL,            -- 'Customer' or 'Staff'
    [SenderName] NVARCHAR(100) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAt] DEFAULT (GETDATE()),
    [IsRead] BIT NOT NULL CONSTRAINT [DF_ChatMessages_IsRead] DEFAULT (0),
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ChatMessages_ChatConversations] FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[ChatConversations]([Id]) ON DELETE CASCADE
);
GO
CREATE NONCLUSTERED INDEX [IX_ChatMessages_ConversationId] ON [dbo].[ChatMessages]([ConversationId] ASC);
GO

-- ============================================================================
-- 18. BẢNG [DailyCheckIns] & [LuckyWheelHistories] - ĐIỂM DANH & VÒNG QUAY
-- ============================================================================
CREATE TABLE [dbo].[DailyCheckIns] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [CheckInDate] DATE NOT NULL,
    [StreakDay] INT NOT NULL CONSTRAINT [DF_DailyCheckIns_StreakDay] DEFAULT (1),
    [PointsAwarded] INT NOT NULL CONSTRAINT [DF_DailyCheckIns_PointsAwarded] DEFAULT (10),
    [SpinsAwarded] INT NOT NULL CONSTRAINT [DF_DailyCheckIns_SpinsAwarded] DEFAULT (0),
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_DailyCheckIns_CreatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_DailyCheckIns] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DailyCheckIns_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
GO
CREATE NONCLUSTERED INDEX [IX_DailyCheckIns_UserId_Date] ON [dbo].[DailyCheckIns]([UserId] ASC, [CheckInDate] ASC);
GO

CREATE TABLE [dbo].[LuckyWheelHistories] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [PrizeName] NVARCHAR(100) NOT NULL,
    [PrizeType] NVARCHAR(30) NOT NULL,         -- 'Points', 'Coupon', 'Spin', 'Luck'
    [PointsWon] INT NOT NULL CONSTRAINT [DF_LuckyWheelHistories_PointsWon] DEFAULT (0),
    [CouponId] INT NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_LuckyWheelHistories_CreatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_LuckyWheelHistories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_LuckyWheelHistories_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LuckyWheelHistories_Coupons] FOREIGN KEY ([CouponId]) REFERENCES [dbo].[Coupons]([Id]) ON DELETE SET NULL
);
GO
CREATE NONCLUSTERED INDEX [IX_LuckyWheelHistories_UserId] ON [dbo].[LuckyWheelHistories]([UserId] ASC);
GO

-- ============================================================================
-- 19. BẢNG [ReturnRequests] - HỆ THỐNG ĐỔI/TRẢ HÀNG 7 NGÀY & VIDEO BÓC HÀNG 100MB
-- ============================================================================
CREATE TABLE [dbo].[ReturnRequests] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ReturnCode] NVARCHAR(50) NOT NULL,
    [OrderId] INT NOT NULL,
    [UserId] INT NULL,
    [CustomerName] NVARCHAR(100) NOT NULL,
    [CustomerPhone] NVARCHAR(20) NOT NULL,
    [CustomerEmail] NVARCHAR(100) NULL,
    [PickupAddress] NVARCHAR(255) NULL,
    [RequestType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ReturnRequests_RequestType] DEFAULT (N'Exchange'),         -- 'Exchange' (Đổi size), 'Refund' (Hoàn tiền)
    [FaultSource] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ReturnRequests_FaultSource] DEFAULT (N'ShopFault'),        -- 'ShopFault', 'CarrierFault', 'CustomerRequest'
    [ReasonCategory] NVARCHAR(150) NOT NULL,
    [ReasonDetail] NVARCHAR(1000) NULL,
    [ProductName] NVARCHAR(200) NOT NULL,
    [CurrentSizeColor] NVARCHAR(100) NULL,
    [DesiredSizeColor] NVARCHAR(100) NULL,
    [ProofImages] NVARCHAR(2000) NULL,
    [UnboxingVideoPath] NVARCHAR(500) NULL,
    [UnboxingVideoUrl] NVARCHAR(500) NULL,
    [HadCarrierCoCheck] BIT NOT NULL CONSTRAINT [DF_ReturnRequests_HadCarrierCoCheck] DEFAULT (0),
    [CarrierName] NVARCHAR(100) NULL,
    [CarrierTrackingCode] NVARCHAR(100) NULL,
    [BankName] NVARCHAR(100) NULL,
    [BankAccountNo] NVARCHAR(50) NULL,
    [BankAccountName] NVARCHAR(100) NULL,
    [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ReturnRequests_Status] DEFAULT (N'Pending'),                     -- 'Pending', 'Approved', 'Rejected', 'Completed'
    [AdminNote] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ReturnRequests_CreatedAt] DEFAULT (GETDATE()),
    [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_ReturnRequests_UpdatedAt] DEFAULT (GETDATE()),
    [ProcessedAt] DATETIME2 NULL,
    CONSTRAINT [PK_ReturnRequests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ReturnRequests_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReturnRequests_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_ReturnRequests_ReturnCode] ON [dbo].[ReturnRequests]([ReturnCode] ASC);
CREATE NONCLUSTERED INDEX [IX_ReturnRequests_OrderId] ON [dbo].[ReturnRequests]([OrderId] ASC);
CREATE NONCLUSTERED INDEX [IX_ReturnRequests_Status] ON [dbo].[ReturnRequests]([Status] ASC);
GO

-- ============================================================================
-- 20. BẢNG [BankAccounts] - CẤU HÌNH TÀI KHOẢN NGÂN HÀNG & THANH TOÁN VIETQR
-- ============================================================================
CREATE TABLE [dbo].[BankAccounts] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [BankCode] NVARCHAR(20) NOT NULL CONSTRAINT [DF_BankAccounts_BankCode] DEFAULT (N'MB'),
    [BankName] NVARCHAR(150) NOT NULL,
    [AccountNumber] NVARCHAR(50) NOT NULL,
    [AccountHolder] NVARCHAR(100) NOT NULL,
    [Branch] NVARCHAR(150) NULL,
    [QrTemplate] NVARCHAR(30) NOT NULL CONSTRAINT [DF_BankAccounts_QrTemplate] DEFAULT (N'compact2'),
    [Note] NVARCHAR(500) NULL,
    [IsDefault] BIT NOT NULL CONSTRAINT [DF_BankAccounts_IsDefault] DEFAULT (0),
    [IsActive] BIT NOT NULL CONSTRAINT [DF_BankAccounts_IsActive] DEFAULT (1),
    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BankAccounts_CreatedAt] DEFAULT (GETDATE()),
    [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_BankAccounts_UpdatedAt] DEFAULT (GETDATE()),
    CONSTRAINT [PK_BankAccounts] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- ============================================================================
-- 21. ĐĂNG KÝ BẢNG LỊCH SỬ MIGRATION CHO ENTITY FRAMEWORK CORE
-- ============================================================================
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId] ASC)
    );
END
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260910134050_InitialCreate')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910134050_InitialCreate', N'8.0.11');
END
GO

-- ============================================================================
-- 21. DỮ LIỆU MẪU BAN ĐẦU (SEED DATA ĐỒNG BỘ 100% VỚI DbInitializer.cs)
-- ============================================================================

-- A. TÀI KHOẢN MẪU
-- Mật khẩu admin: Admin@123456 -> Hash SHA256 kèm salt bí mật
-- Mật khẩu khachhang: 123456 -> Hash SHA256 kèm salt bí mật
SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] 
([Id], [Username], [PasswordHash], [FullName], [Email], [PhoneNumber], [Address], [Role], [CreatedAt], [IsActive], [RewardPoints], [TotalSpent], [MemberTier], [TierExpiryDate], [DailyStreakCount], [LuckyWheelSpinsAvailable])
VALUES 
(1, N'admin', N'YZovgFDpVqitoW0TXoaT5tfW5RSZIGYBPjQVuDlQ88M=', N'Quản Trị Viên BabyShop', N'admin@babyshop.vn', N'0988889999', N'Hà Nội, Việt Nam', N'Admin', GETDATE(), 1, 0, 0, N'Thành viên hạng Kim Cương', NULL, 0, 99),
(2, N'khachhang', N'ewNAXAsuiPDYupvyLZclcX+ur3jkA+kdlr1dBZ9R60g=', N'Nguyễn Thu Hà', N'thuha@gmail.com', N'0912345678', N'123 Cầu Giấy, Hà Nội', N'Customer', GETDATE(), 1, 250, 1250000, N'Thành viên hạng Bạc', DATEADD(year, 1, GETDATE()), 3, 2);
SET IDENTITY_INSERT [dbo].[Users] OFF;
GO

-- B. DANH MỤC THỜI TRANG TRẺ EM
SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([Id], [Name], [Slug], [Description], [ImageUrl], [DisplayOrder], [IsActive], [ParentId])
VALUES 
(1, N'Thời Trang Bé Gái', N'thoi-trang-be-gai', N'Váy đầm công chúa, set đồ xinh xắn, áo kiểu và chân váy thời thượng cho bé gái', N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=600&q=80', 1, 1, NULL),
(2, N'Thời Trang Bé Trai', N'thoi-trang-be-trai', N'Set công tử, áo thun polo năng động, quần jean và yếm sành điệu cho bé trai', N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=600&q=80', 2, 1, NULL),
(3, N'Thời Trang Sơ Sinh (0-24M)', N'thoi-trang-so-sinh', N'Bodysuit hữu cơ, romper mềm mại, bộ dài tay 100% cotton organic bảo vệ làn da non nớt', N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=600&q=80', 3, 1, NULL),
(4, N'Phụ Kiện & Giày Dép', N'phu-kien-giay-dep', N'Mũ nón, kính râm, nơ kẹp tóc, vớ tất và giày tập đi êm chân chống trượt', N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=600&q=80', 4, 1, NULL);
SET IDENTITY_INSERT [dbo].[Categories] OFF;
GO

-- C. SẢN PHẨM MẪU (KÈM FLASH SALE & DEAL SỐC)
SET IDENTITY_INSERT [dbo].[Products] ON;
INSERT INTO [dbo].[Products] 
([Id], [Name], [Slug], [Sku], [ShortDescription], [Description], [Material], [CareInstructions], [Price], [DiscountPrice], [MainImageUrl], [Gender], [AgeRange], [IsFeatured], [IsNewArrival], [IsBestSeller], [IsActive], [CreatedAt], [CategoryId], [IsFlashSale], [FlashSalePrice], [FlashSaleQuantity], [FlashSaleSold], [FlashSaleEndTime], [IsAddonDeal], [AddonPrice])
VALUES 
(1, N'Váy Công Chúa Voan Xốp Nơ Pastel', N'vay-cong-chua-voan-xop-no-pastel', N'VBG-001', 
 N'Váy công chúa chất voan dệt xốp bồng bềnh, phối nơ ngọt ngào cho bé dự tiệc, sinh nhật.', 
 N'<p>Chiếc đầm công chúa làm từ chất liệu voan dệt xốp cao cấp, lớp lót trong 100% cotton mềm mịn an toàn tuyệt đối cho làn da bé gái.</p>', 
 N'Voan xốp cao cấp, lót cotton 100%', N'Nên giặt tay nhẹ nhàng, ủi nhiệt độ thấp.', 320000, 259000, 
 N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80', N'Bé gái', N'Bé tập đi (1 - 3 tuổi)', 1, 1, 1, 1, GETDATE(), 1, 1, 199000, 30, 18, DATEADD(hour, 28, GETDATE()), 0, NULL),

(2, N'Đầm Hoa Nhí Cổ Sen Phong Cách Vintage', N'dam-hoa-nhi-co-sen-vintage', N'VBG-002', 
 N'Đầm suông vải thô đũi organic thoáng mát in họa tiết hoa nhí phong cách Hàn Quốc dịu dàng.', 
 N'<p>Đầm hoa nhí cổ bèo sen mang phong cách vintage nhẹ nhàng, đằm thắm. Thiết kế tay phồng nhẹ nhàng, form suông dễ vận động.</p>', 
 N'Vải thô đũi Cotton Organic', N'Giặt máy ở chế độ nhẹ nhàng.', 280000, 219000, 
 N'https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80', N'Bé gái', N'Mẫu giáo (3 - 6 tuổi)', 1, 0, 1, 1, GETDATE(), 1, 1, 179000, 25, 12, DATEADD(hour, 28, GETDATE()), 0, NULL),

(3, N'Áo Len Cardigan Dệt Kim Họa Tiết Quả Trám', N'ao-len-cardigan-det-kim-hoa-tiet-qua-tram', N'VBG-003', 
 N'Áo cardigan len dệt kim sợi mềm cao cấp, ấm áp và dễ phối với mọi loại váy áo cho bé.', 
 N'<p>Áo khoác len cardigan dệt sợi bông tự nhiên giữ ấm hiệu quả, co giãn đàn hồi tốt, không gây dặm ngứa cổ hay nách của bé.</p>', 
 N'Len lông cừu pha sợi Cotton tự nhiên', N'Nên giặt tay, vắt nhẹ.', 350000, 289000, 
 N'https://images.unsplash.com/photo-1518831959646-742c3a14ebf7?auto=format&fit=crop&w=800&q=80', N'Bé gái', N'Tiểu học (6 - 12 tuổi)', 0, 1, 0, 1, GETDATE(), 1, 0, NULL, 0, 0, NULL, 0, NULL),

(4, N'Set Quần Yếm Denim Phong Cách Hàn Quốc Bé Trai', N'set-quan-yem-denim-han-quoc-be-trai', N'VBT-001', 
 N'Set yếm bò jean mềm co giãn kèm áo thun trắng cotton cho bé trai năng động, bảnh bao.', 
 N'<p>Bộ yếm bò thời trang được các mẹ yêu thích nhất cho bé trai. Kèm áo phông trắng 100% cotton mỏng nhẹ thoáng mồ hôi.</p>', 
 N'Denim wash mềm + Thun cotton 4 chiều', N'Giặt riêng với đồ sáng màu.', 360000, 299000, 
 N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80', N'Bé trai', N'Bé tập đi (1 - 3 tuổi)', 1, 1, 1, 1, GETDATE(), 2, 0, NULL, 0, 0, NULL, 0, NULL),

(5, N'Áo Thun Polo Kẻ Ngang Thể Thao Năng Động', N'ao-thun-polo-ke-ngang-nang-dong', N'VBT-002', 
 N'Áo polo cổ bẻ phối sọc ngang thời thượng, phong cách quý tử nhí lịch thiệp nhưng năng động.', 
 N'<p>Chất cá sấu cotton co giãn thấm hút mồ hôi cực tốt. Thiết kế cổ bẻ cứng cáp giữ form chuẩn khi giặt máy.</p>', 
 N'Vải Pique cá sấu 100% Cotton', N'Giặt máy bình thường.', 195000, 159000, 
 N'https://images.unsplash.com/photo-1503944583220-79d8926ad5e2?auto=format&fit=crop&w=800&q=80', N'Bé trai', N'Mẫu giáo (3 - 6 tuổi)', 1, 0, 1, 1, GETDATE(), 2, 0, NULL, 0, 0, NULL, 0, NULL),

(6, N'Bộ Bodysuit Romper Cotton Organic Xuất Hàn Sơ Sinh', N'bo-bodysuit-romper-cotton-organic-xuat-han', N'BSS-001', 
 N'Chất liệu cotton hữu cơ kháng khuẩn, đường may phẳng lộn ngoài chống xước da bé sơ sinh.', 
 N'<p>Sản phẩm dành riêng cho làn da non nớt của các thiên thần nhỏ. 100% sợi cotton hữu cơ an toàn Oeko-Tex Standard 100.</p>', 
 N'100% Cotton Organic nguyên chất', N'Giặt nhẹ với nước giặt em bé.', 210000, 165000, 
 N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80', N'Sơ sinh', N'Sơ sinh (0 - 24 tháng)', 1, 1, 1, 1, GETDATE(), 3, 0, NULL, 0, 0, NULL, 0, NULL),

(7, N'Set Bộ Dài Tay Cài Lệch Bamboo Kháng Khuẩn', N'set-bo-dai-tay-cai-lech-bamboo', N'BSS-002', 
 N'Vải sợi tre Bamboo cao cấp mềm mướt, điều hòa thân nhiệt cực tốt cho bé nằm điều hòa.', 
 N'<p>Sợi tre thiên nhiên có khả năng kháng khuẩn tự nhiên, kiểm soát mùi và điều hòa thân nhiệt thông minh cho bé giấc ngủ ngon.</p>', 
 N'Sợi tre Bamboo 95% + Spandex 5%', N'Giặt nước thường, phơi bóng râm.', 260000, 205000, 
 N'https://images.unsplash.com/photo-1555252333-9f8e92e65df9?auto=format&fit=crop&w=800&q=80', N'Sơ sinh', N'Sơ sinh (0 - 24 tháng)', 1, 0, 1, 1, GETDATE(), 3, 0, NULL, 0, 0, NULL, 0, NULL),

(8, N'Giày Tập Đi Da Mềm Quai Dán Chống Trượt', N'giay-tap-di-da-mem-quai-dan-chong-truot', N'PK-001', 
 N'Đế cao su nhiệt dẻo mềm uốn cong 1/3, hỗ trợ bé tập những bước đi đầu đời an toàn.', 
 N'<p>Giày tập đi chuẩn y khoa cho bé từ 6 tháng đến 2 tuổi. Mũi giày rộng rãi không chèn ép ngón chân, quai dán tiện lợi điều chỉnh.</p>', 
 N'Da PU cao cấp vi sinh + Đế cao su dẻo', N'Lau sạch bằng khăn ẩm.', 220000, 175000, 
 N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=800&q=80', N'Unisex', N'Sơ sinh (0 - 24 tháng)', 1, 1, 1, 1, GETDATE(), 4, 1, 89000, 50, 24, NULL, 1, 49000);
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

-- D. BIẾN THỂ KÍCH CỠ & MÀU SẮC (CÓ ẢNH & MÃ MÀU HEX)
INSERT INTO [dbo].[ProductVariants] ([ProductId], [Size], [Color], [ColorHex], [StockQuantity], [Sku], [ImageUrl])
VALUES 
-- Váy công chúa
(1, N'1 - 2 tuổi (9 - 11kg)', N'Hồng Pastel', N'#FFC0CB', 20, N'VBG-001-P1', N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80'),
(1, N'2 - 3 tuổi (12 - 14kg)', N'Hồng Pastel', N'#FFC0CB', 15, N'VBG-001-P2', N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80'),
(1, N'3 - 4 tuổi (15 - 17kg)', N'Hồng Pastel', N'#FFC0CB', 12, N'VBG-001-P3', N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80'),
(1, N'1 - 2 tuổi (9 - 11kg)', N'Trắng Kem', N'#FFFDD0', 15, N'VBG-001-W1', N'https://images.unsplash.com/photo-1518831959646-742c3a14ebf7?auto=format&fit=crop&w=800&q=80'),
-- Đầm hoa nhí
(2, N'Size 90 (2 - 3T, 11-13kg)', N'Vàng Nhạt', N'#FFF9A6', 18, N'VBG-002-Y90', N'https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80'),
(2, N'Size 100 (3 - 4T, 14-16kg)', N'Vàng Nhạt', N'#FFF9A6', 22, N'VBG-002-Y100', N'https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80'),
(2, N'Size 110 (4 - 5T, 17-19kg)', N'Vàng Nhạt', N'#FFF9A6', 14, N'VBG-002-Y110', N'https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80'),
-- Set yếm bé trai
(4, N'Size 90 (9 - 11kg)', N'Xanh Denim', N'#4682B4', 15, N'VBT-001-D90', N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80'),
(4, N'Size 100 (12 - 14kg)', N'Xanh Denim', N'#4682B4', 20, N'VBT-001-D100', N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80'),
(4, N'Size 110 (15 - 17kg)', N'Xanh Denim', N'#4682B4', 18, N'VBT-001-D110', N'https://images.unsplash.com/photo-1519689680058-324335c77eba?auto=format&fit=crop&w=800&q=80'),
-- Polo bé trai
(5, N'Size 100 (2 - 3T, 12-14kg)', N'Xanh Navy', N'#000080', 25, N'VBT-002-NV1', N'https://images.unsplash.com/photo-1503944583220-79d8926ad5e2?auto=format&fit=crop&w=800&q=80'),
(5, N'Size 110 (3 - 4T, 15-17kg)', N'Xanh Navy', N'#000080', 20, N'VBT-002-NV2', N'https://images.unsplash.com/photo-1503944583220-79d8926ad5e2?auto=format&fit=crop&w=800&q=80'),
-- Bodysuit sơ sinh
(6, N'0 - 3 tháng (3 - 5kg)', N'Vàng Kem Nhạt', N'#FFFDD0', 30, N'BSS-001-03', N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80'),
(6, N'3 - 6 tháng (5 - 7kg)', N'Vàng Kem Nhạt', N'#FFFDD0', 25, N'BSS-001-36', N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80'),
(6, N'6 - 9 tháng (7 - 9kg)', N'Vàng Kem Nhạt', N'#FFFDD0', 20, N'BSS-001-69', N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80'),
-- Giày tập đi
(8, N'Size 11 (Chân 10.5cm, 6-9M)', N'Nâu Bò Vintage', N'#8B4513', 15, N'PK-001-11', N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=800&q=80'),
(8, N'Size 12 (Chân 11.5cm, 9-12M)', N'Nâu Bò Vintage', N'#8B4513', 18, N'PK-001-12', N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=800&q=80');
GO

-- E. MÃ GIẢM GIÁ MẪU (COUPONS)
SET IDENTITY_INSERT [dbo].[Coupons] ON;
INSERT INTO [dbo].[Coupons] 
([Id], [Code], [Title], [Description], [DiscountType], [DiscountValue], [MinOrderAmount], [MaxDiscountAmount], [UsageLimit], [UsedCount], [StartDate], [ExpiryDate], [IsActive], [IsForYou], [IsFlashHunt], [AssignedUserId])
VALUES 
(1, N'BABYNEW', N'Ưu đãi chào bé mới', N'Giảm 10% tối đa 40.000đ cho đơn hàng đầu tiên từ 150.000đ', 0, 10, 150000, 40000, 500, 28, DATEADD(day, -30, GETDATE()), DATEADD(month, 3, GETDATE()), 1, 0, 0, NULL),
(2, N'MEVACO50', N'Mẹ và bé yêu', N'Giảm ngay 50.000đ tiền mặt cho đơn hàng từ 300.000đ', 1, 50000, 300000, NULL, 200, 42, DATEADD(day, -15, GETDATE()), DATEADD(month, 2, GETDATE()), 1, 0, 0, NULL),
(3, N'FREESHIP', N'Miễn phí vận chuyển', N'Giảm ngay 30.000đ phí giao hàng toàn quốc cho đơn từ 100.000đ', 1, 30000, 100000, NULL, 1000, 135, DATEADD(day, -60, GETDATE()), DATEADD(month, 6, GETDATE()), 1, 0, 0, NULL),
(4, N'VIPBABY', N'Khách hàng thân thiết', N'Giảm 15% tối đa 100.000đ cho đơn hàng từ 500.000đ', 0, 15, 500000, 100000, 300, 15, DATEADD(day, -10, GETDATE()), DATEADD(month, 3, GETDATE()), 1, 0, 0, NULL),
(5, N'DAIDIECTUYET', N'Đại tiệc sắm đồ cho con', N'Giảm 20% tối đa 180.000đ cho đơn hàng lớn từ 800.000đ', 0, 20, 800000, 180000, 100, 9, DATEADD(day, -5, GETDATE()), DATEADD(month, 1, GETDATE()), 1, 0, 0, NULL),
(6, N'SANMA-50K', N'Săn Mã Giờ Vàng - Giảm 50.000đ', N'Ưu đãi săn mã giảm ngay 50.000đ cho đơn từ 200.000đ. Hiển thị sẵn trong kho, đến giờ mở mới được sử dụng!', 1, 50000, 200000, NULL, 200, 0, DATEADD(hour, 2, GETDATE()), DATEADD(day, 3, GETDATE()), 1, 0, 1, NULL),
(7, N'LUCKY20K', N'Voucher May Mắn 20.000đ', N'Phần quà từ Vòng Quay May Mắn Baby Club. Giảm 20.000đ cho đơn từ 150.000đ', 1, 20000, 150000, NULL, 10000, 0, DATEADD(day, -1, GETDATE()), DATEADD(year, 1, GETDATE()), 1, 0, 0, NULL),
(8, N'LUCKYSHIP', N'Freeship Vòng Quay 30.000đ', N'Phần quà từ Vòng Quay May Mắn Baby Club. Giảm phí ship 30.000đ cho đơn từ 200.000đ', 1, 30000, 200000, NULL, 10000, 0, DATEADD(day, -1, GETDATE()), DATEADD(year, 1, GETDATE()), 1, 0, 0, NULL),
(9, N'LUCKY50K', N'Voucher VIP Vòng Quay 50.000đ', N'Phần quà ĐẶC BIỆT từ Vòng Quay May Mắn Baby Club. Giảm 50.000đ cho đơn từ 350.000đ', 1, 50000, 350000, NULL, 10000, 0, DATEADD(day, -1, GETDATE()), DATEADD(year, 1, GETDATE()), 1, 0, 0, NULL);
SET IDENTITY_INSERT [dbo].[Coupons] OFF;
GO

-- F. ĐƠN HÀNG MẪU (ORDERS & ORDER DETAILS)
SET IDENTITY_INSERT [dbo].[Orders] ON;
INSERT INTO [dbo].[Orders]
([Id], [OrderCode], [CustomerName], [CustomerPhone], [CustomerEmail], [ShippingAddress], [Note], [SubTotal], [CouponCode], [DiscountAmount], [MemberDiscount], [PointsUsed], [PointDiscount], [ShippingFee], [TotalAmount], [PaymentMethod], [IsPaid], [OrderStatus], [CreatedAt], [UserId])
VALUES
(1, N'BS-20260910-1024', N'Nguyễn Thu Hà', N'0912345678', N'thuha@gmail.com', N'Số 45 Đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội', N'Giao giờ hành chính, gọi trước khi giao giúp mình nha shop.', 478000, N'FREESHIP', 0, 0, 0, 0, 0, 478000, N'COD', 0, N'Shipping', DATEADD(day, -1, GETDATE()), 2),
(2, N'BS-20260909-8868', N'Trần Minh Anh', N'0977112233', N'minhanh@gmail.com', N'288 Nguyễn Tri Phương, Phường 4, Quận 10, TP. Hồ Chí Minh', N'Giao buổi sáng giúp mình, đóng hộp cẩn thận nhé!', 330000, N'MEVACO50', 50000, 0, 0, 0, 0, 280000, N'BankTransfer', 1, N'Completed', DATEADD(day, -3, GETDATE()), 2);
SET IDENTITY_INSERT [dbo].[Orders] OFF;
GO

SET IDENTITY_INSERT [dbo].[OrderDetails] ON;
INSERT INTO [dbo].[OrderDetails]
([Id], [OrderId], [ProductId], [ProductVariantId], [ProductName], [ProductImage], [Size], [Color], [UnitPrice], [Quantity])
VALUES
(1, 1, 1, 1, N'Váy Công Chúa Voan Xốp Nơ Pastel', N'https://images.unsplash.com/photo-1622290291468-a28f7a7dc6a8?auto=format&fit=crop&w=800&q=80', N'1 - 2 tuổi (9 - 11kg)', N'Hồng Pastel', 259000, 1),
(2, 1, 2, 5, N'Đầm Hoa Nhí Cổ Sen Phong Cách Vintage', N'https://images.unsplash.com/photo-1596870230751-ebdfce98ec42?auto=format&fit=crop&w=800&q=80', N'Size 90 (2 - 3T, 11-13kg)', N'Vàng Nhạt', 219000, 1),
(3, 2, 6, 13, N'Bộ Bodysuit Romper Cotton Organic Xuất Hàn Sơ Sinh', N'https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=800&q=80', N'0 - 3 tháng (3 - 5kg)', N'Vàng Kem Nhạt', 165000, 2);
SET IDENTITY_INSERT [dbo].[OrderDetails] OFF;
GO

-- G. ĐÁNH GIÁ SẢN PHẨM MẪU (PRODUCT REVIEWS)
SET IDENTITY_INSERT [dbo].[ProductReviews] ON;
INSERT INTO [dbo].[ProductReviews]
([Id], [ProductId], [UserId], [CustomerName], [CustomerPhone], [Rating], [Comment], [BabyInfo], [IsVerifiedPurchase], [IsApproved], [HelpfulCount], [CreatedAt])
VALUES
(1, 1, 2, N'Mẹ Bảo An', N'0988***112', 5, N'Váy xinh xỉu các mẹ ơi! Chất vải cotton organic mềm mịn sờ mướt tay lắm, đường chỉ may phẳng phiu không có sợi thừa cọ ngứa da con. Bé mặc đi tiệc ai cũng khen như công chúa nhỏ.', N'Bé gái 18 tháng • 11.5 kg', 1, 1, 18, DATEADD(day, -2, GETDATE())),
(2, 1, NULL, N'Chị Minh Thư', N'0912***456', 5, N'Giao hàng siêu nhanh, đóng gói hộp xinh xắn thơm nức mùi em bé. Form váy xòe bồng bềnh tự nhiên, giặt máy chế độ nhẹ không hề bị co rút hay phai màu.', N'Bé 2 tuổi • 12 kg', 1, 1, 9, DATEADD(day, -5, GETDATE())),
(3, 2, NULL, N'Mẹ Sóc Nâu', N'0933***889', 5, N'Set đồ năng động siêu ngầu cho hoàng tử nhỏ! Thun co giãn 4 chiều cực thích, cạp chun mềm không hằn bụng con. Bé chạy nhảy cả buổi mà vẫn khô thoáng mồ hôi.', N'Bé trai 2.5 tuổi • 13.5 kg', 1, 1, 12, DATEADD(day, -3, GETDATE())),
(4, 6, 2, N'Mẹ Bắp Nhỏ', N'0977***665', 5, N'Bộ body chip siêu mềm mịn, cúc bấm đáy chắc chắn thay bỉm cho bé nửa đêm siêu nhanh. Vải organic không mùi hóa chất, mẹ rất yên tâm cho da con nhạy cảm.', N'Bé 5 tháng • 7.2 kg', 1, 1, 14, DATEADD(day, -4, GETDATE()));
SET IDENTITY_INSERT [dbo].[ProductReviews] OFF;
GO

-- H. PHIẾU NHẬP KHO MẪU (STOCK RECEIPTS & DETAILS)
SET IDENTITY_INSERT [dbo].[StockReceipts] ON;
INSERT INTO [dbo].[StockReceipts]
([Id], [ReceiptCode], [SupplierName], [SupplierPhone], [SupplierAddress], [Note], [TotalAmount], [Status], [CreatedBy], [CreatedAt])
VALUES
(1, N'PN-20260908-1001', N'Xưởng Dệt May Trẻ Em Minh Châu', N'0918889922', N'Lô 12, KCN Tân Bình, TP. Hồ Chí Minh', N'Nhập lô hàng đầu vụ thu đông bé gái và body chip sơ sinh', 12500000, N'Completed', N'Admin', DATEADD(day, -3, GETDATE())),
(2, N'PN-20260909-1002', N'Công Ty May Mặc Trẻ Em KidsCare Việt Nam', N'0909223344', N'Số 88 Giải Phóng, Hai Bà Trưng, Hà Nội', N'Nhập bổ sung set yếm denim bé trai và giày tập đi xuất Hàn', 9800000, N'Completed', N'Admin', DATEADD(day, -2, GETDATE()));
SET IDENTITY_INSERT [dbo].[StockReceipts] OFF;
GO

SET IDENTITY_INSERT [dbo].[StockReceiptDetails] ON;
INSERT INTO [dbo].[StockReceiptDetails]
([Id], [StockReceiptId], [ProductId], [ProductVariantId], [ProductName], [Size], [Color], [Quantity], [ImportPrice], [TotalPrice])
VALUES
(1, 1, 1, 1, N'Váy Công Chúa Voan Xốp Nơ Pastel', N'1 - 2 tuổi (9 - 11kg)', N'Hồng Pastel', 50, 150000, 7500000),
(2, 1, 6, 13, N'Bộ Bodysuit Romper Cotton Organic Xuất Hàn Sơ Sinh', N'0 - 3 tháng (3 - 5kg)', N'Vàng Kem Nhạt', 50, 100000, 5000000),
(3, 2, 4, 8, N'Set Quần Yếm Denim Phong Cách Hàn Quốc Bé Trai', N'Size 90 (9 - 11kg)', N'Xanh Denim', 40, 160000, 6400000),
(4, 2, 8, 16, N'Giày Tập Đi Da Mềm Quai Dán Chống Trượt', N'Size 11 (Chân 10.5cm, 6-9M)', N'Nâu Bò Vintage', 34, 100000, 3400000);
SET IDENTITY_INSERT [dbo].[StockReceiptDetails] OFF;
GO

-- I. LỊCH SỬ TÍCH ĐIỂM & ĐIỂM DANH HÀNG NGÀY
INSERT INTO [dbo].[PointTransactions] ([UserId], [OrderId], [PointsChanged], [Type], [Description], [CreatedAt])
VALUES
(2, 2, 125, N'Earn', N'Tích điểm tự động từ đơn hàng thành công #BS-20260909-8868', DATEADD(day, -3, GETDATE())),
(2, NULL, 125, N'Bonus', N'Thưởng chào mừng nâng hạng Thành viên hạng Bạc (Baby Club)', DATEADD(day, -2, GETDATE()));
GO

INSERT INTO [dbo].[DailyCheckIns] ([UserId], [CheckInDate], [StreakDay], [PointsAwarded], [SpinsAwarded], [CreatedAt])
VALUES
(2, CAST(DATEADD(day, -2, GETDATE()) AS DATE), 1, 10, 0, DATEADD(day, -2, GETDATE())),
(2, CAST(DATEADD(day, -1, GETDATE()) AS DATE), 2, 10, 0, DATEADD(day, -1, GETDATE())),
(2, CAST(GETDATE() AS DATE), 3, 10, 1, GETDATE());
GO

-- J. NHẬT KÝ KIỂM TOÁN ADMIN (AUDIT LOGS)
INSERT INTO [dbo].[AuditLogs] ([Action], [EntityName], [EntityId], [Description], [UserName], [IpAddress], [CreatedAt])
VALUES
(N'Create', N'Product', 1, N'Khởi tạo sản phẩm mới "Váy Công Chúa Voan Xốp Nơ Pastel"', N'admin', N'127.0.0.1', DATEADD(day, -5, GETDATE())),
(N'Restock', N'StockReceipt', 1, N'Tạo phiếu nhập kho PN-20260908-1001 (100 sản phẩm)', N'admin', N'127.0.0.1', DATEADD(day, -3, GETDATE())),
(N'Update', N'Coupon', 1, N'Cập nhật mã giảm giá BABYNEW (Giảm 10%)', N'admin', N'127.0.0.1', DATEADD(day, -2, GETDATE())),
(N'ChangeStatus', N'Order', 2, N'Chuyển trạng thái đơn hàng sang Hoàn thành (Completed)', N'admin', N'127.0.0.1', DATEADD(day, -1, GETDATE()));
GO

-- K. DỮ LIỆU ĐỔI / TRẢ HÀNG 7 NGÀY (RETURN REQUESTS)
SET IDENTITY_INSERT [dbo].[ReturnRequests] ON;
INSERT INTO [dbo].[ReturnRequests] 
([Id], [ReturnCode], [OrderId], [UserId], [CustomerName], [CustomerPhone], [CustomerEmail], [PickupAddress], [RequestType], [FaultSource], [ReasonCategory], [ReasonDetail], [ProductName], [CurrentSizeColor], [DesiredSizeColor], [ProofImages], [UnboxingVideoPath], [UnboxingVideoUrl], [HadCarrierCoCheck], [CarrierName], [CarrierTrackingCode], [BankName], [BankAccountNo], [BankAccountName], [Status], [AdminNote], [CreatedAt], [UpdatedAt], [ProcessedAt])
VALUES 
(1, N'RT-20260911-8812', 1, 2, N'Nguyễn Thu Hà', N'0912345678', N'thuha@gmail.com', N'Số 45 Đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội', 
 N'Exchange', N'ShopFault', N'Giao nhầm mẫu / sai kích thước so với đơn đặt', 
 N'Mẹ đặt set đồ size 1-2Y nhưng khi mở bưu kiện ra lại là size 3-4Y. Bé không mặc vừa, nhờ shop gửi lại đúng size giúp mẹ ạ.', 
 N'Váy Công Chúa Voan Xốp Nơ Pastel', N'Size 3-4Y (Giao nhầm)', N'Size 1-2Y (Đúng đơn)', 
 N'/images/banner-kid.jpg', NULL, N'https://www.youtube.com/watch?v=dQw4w9WgXcQ', 
 0, N'Viettel Post', N'VPBS202609101024', NULL, NULL, NULL, 
 N'Pending', NULL, DATEADD(hour, -3, GETDATE()), DATEADD(hour, -3, GETDATE()), NULL),

(2, N'RT-20260910-6321', 2, 2, N'Trần Minh Anh', N'0977112233', N'minhanh@gmail.com', N'288 Nguyễn Tri Phương, Phường 4, Quận 10, TP. Hồ Chí Minh', 
 N'Exchange', N'CarrierFault', N'Hàng bị vỡ móp / rách bao bì trong lúc vận chuyển', 
 N'Hộp bưu kiện bên ngoài bị dập nát, ướt sũng do bưu tá đi mưa. Vải áo bên trong bị lem màu nước bẩn. Có video bóc hàng quay rõ tem vận đơn của bưu cục.', 
 N'Bộ Bodysuit Romper Cotton Organic Xuất Hàn Sơ Sinh', N'Size 0-3M (Bị ướt bẩn)', N'Size 0-3M (Nguyên vẹn mới)', 
 N'/images/banner-kid.jpg', NULL, NULL, 
 0, N'Giao Hàng Nhanh (GHN)', N'GHN88291038', NULL, NULL, NULL, 
 N'Approved', N'Đã duyệt đổi sản phẩm mới ngay cho khách (miễn phí ship). Bộ phận vận hành đã gửi công văn khiếu nại bồi thường 100% đến GHN kèm video bóc hàng của khách.', DATEADD(day, -1, GETDATE()), DATEADD(hour, -4, GETDATE()), DATEADD(hour, -4, GETDATE())),

(3, N'RT-20260909-5419', 2, 2, N'Lê Thị Ngọc Lan', N'0934567890', N'ngoclan@gmail.com', N'45 Lê Duẩn, Quận 1, TP. Hồ Chí Minh', 
 N'Exchange', N'CustomerRequest', N'Bé mặc chật/rộng (Đổi size)', 
 N'Bé nhà mình trộm vía tăng cân nhanh nên mặc size 0-3M hơi kích nách, mình muốn nhờ shop hỗ trợ đổi lên size 3-6M với ạ.', 
 N'Bộ Bodysuit Romper Cotton Organic Xuất Hàn Sơ Sinh', N'Size 0-3M (Xanh)', N'Size 3-6M (Xanh)', 
 N'/images/banner-kid.jpg', NULL, NULL, 
 1, N'J&T Express', N'JT99281726', NULL, NULL, NULL, 
 N'Completed', N'Đã hoàn tất đổi size tận nhà cho mẹ qua bưu tá J&T Express. Đơn hàng mới đã được giao thành công.', DATEADD(day, -2, GETDATE()), DATEADD(day, -1, GETDATE()), DATEADD(day, -1, GETDATE()));
SET IDENTITY_INSERT [dbo].[ReturnRequests] OFF;
GO

-- L. LIVE CHAT CSKH MẪU
SET IDENTITY_INSERT [dbo].[ChatConversations] ON;
INSERT INTO [dbo].[ChatConversations] ([Id], [SessionId], [UserId], [CustomerName], [CustomerPhone], [LastMessageAt], [HasUnreadAdmin])
VALUES (1, N'SESSION-DEMO-001', 2, N'Nguyễn Thu Hà', N'0912345678', GETDATE(), 0);
SET IDENTITY_INSERT [dbo].[ChatConversations] OFF;
GO

INSERT INTO [dbo].[ChatMessages] ([ConversationId], [Sender], [SenderName], [Message], [CreatedAt], [IsRead])
VALUES
(1, N'Customer', N'Nguyễn Thu Hà', N'Dạ shop ơi, em muốn hỏi mẫu Váy Công Chúa bé 12kg thì mặc size nào vừa vặn nhất ạ?', DATEADD(minute, -15, GETDATE()), 1),
(1, N'Staff', N'CSKH BabyShop', N'Dạ chào mẹ Hà ạ! Bé 12kg mẹ chọn Size 2 - 3 tuổi (12 - 14kg) là bé mặc vừa xinh và thoải mái vận động nhất mẹ nhé. Chất voan dệt xốp lót cotton 100% mềm mịn mát lắm ạ!', DATEADD(minute, -12, GETDATE()), 1),
(1, N'Customer', N'Nguyễn Thu Hà', N'Dạ cảm ơn shop, em vừa đặt đơn luôn rồi ạ!', DATEADD(minute, -10, GETDATE()), 1);
GO

-- M. CẤU HÌNH TÀI KHOẢN NGÂN HÀNG & VIETQR MẶC ĐỊNH
SET IDENTITY_INSERT [dbo].[BankAccounts] ON;
INSERT INTO [dbo].[BankAccounts]
([Id], [BankCode], [BankName], [AccountNumber], [AccountHolder], [Branch], [QrTemplate], [Note], [IsDefault], [IsActive], [CreatedAt], [UpdatedAt])
VALUES
(1, N'MB', N'MB Bank (Ngân hàng Quân Đội)', N'212882', N'MAI TIEN DUNG', N'Chi nhánh Hà Nội', N'compact2', N'Mẹ vui lòng giữ nguyên nội dung chuyển khoản là mã đơn hàng để hệ thống tự động gạch nợ và xác nhận đơn trong 1-3 phút nhé!', 1, 1, GETDATE(), GETDATE());
SET IDENTITY_INSERT [dbo].[BankAccounts] OFF;
GO

PRINT N'============================================================================';
PRINT N'==> KHỞI TẠO TOÀN DIỆN CƠ SỞ DỮ LIỆU [WebsiteBabyShopDb] THÀNH CÔNG 100%! <==';
PRINT N'==> ĐÃ TẠO ĐẦY ĐỦ 20 BẢNG & NẠP DỮ LIỆU MẪU CHUẨN XÁC ĐỒNG BỘ VỚI C# MODEL <==';
PRINT N'============================================================================';
GO
