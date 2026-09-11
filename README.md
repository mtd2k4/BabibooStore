# 👶 HỆ THỐNG THƯƠNG MẠI ĐIỆN TỬ THỜI TRANG & ĐỒ DÙNG TRẺ EM - BABYSHOP STUDIO KIDS

> **Tài liệu Kỹ thuật & Hướng dẫn Vận hành Toàn diện Dự án (.NET 8.0 MVC - Code First)**  
> Phiên bản: **2.0 Enterprise** | Nền tảng: **ASP.NET Core 8.0** | Cơ sở dữ liệu: **SQL Server / LocalDB**

---

## 📑 MỤC LỤC CHI TIẾT
1. [Giới Thiệu Tổng Quan](#1-giới-thiệu-tổng-quan)
2. [Kiến Trúc Kỹ Thuật & Công Nghệ](#2-kiến-trúc-kỹ-thuật--công-nghệ)
3. [Cấu Trúc Thư Mục Toàn Dự Án](#3-cấu-trúc-thư-mục-toàn-dự-án)
4. [Mô Hình Cơ Sở Dữ Liệu (Database Schema)](#4-mô-hình-cơ-sở-dữ-liệu-database-schema)
5. [Chi Tiết Chức Năng Phía Khách Hàng (Client Storefront)](#5-chi-tiết-chức-năng-phía-khách-hàng-client-storefront)
6. [Chi Tiết Chức Năng Phân Hệ Quản Trị (Admin Area)](#6-chi-tiết-chức-năng-phân-hệ-quản-trị-admin-area)
7. [Các Quy Trình Nghiệp Vụ Cốt Lõi (Core Workflows)](#7-các-quy-trình-nghiệp-vụ-cốt-lõi-core-workflows)
   - [7.1 Quy trình Quản lý Tồn kho Biến thể (Stock Management)](#71-quy-trình-quản-lý-tồn-kho-biến-thể-stock-management)
   - [7.2 Quy trình Đặt hàng & Xác thực Địa chỉ (Checkout Flow)](#72-quy-trình-đặt-hàng--xác-thực-địa-chỉ-checkout-flow)
   - [7.3 Hệ thống Thành viên Baby Club & Tích lũy Điểm thưởng](#73-hệ-thống-thành-viên-baby-club--tích-lũy-điểm-thưởng)
   - [7.4 Thanh toán Tự động VietQR 24/7](#74-thanh-toán-tự-động-vietqr-247)
   - [7.5 Dịch vụ Gửi Email Xác nhận Đơn hàng (Email Service)](#75-dịch-vụ-gửi-email-xác-nhận-đơn-hàng-email-service)
   - [7.6 Trợ lý Tư vấn Bán hàng Trí tuệ Nhân tạo (Google Gemini AI)](#76-trợ-lý-tư-vấn-bán-hàng-trí-tuệ-nhân-tạo-google-gemini-ai)
   - [7.7 Hệ thống Nhật ký Kiểm toán (Audit Logs)](#77-hệ-thống-nhật-ký-kiểm-toán-audit-logs)
8. [Danh Mục Controller & Endpoints](#8-danh-mục-controller--endpoints)
9. [Hướng Dẫn Cài Đặt, Cấu Hình & Chuyển Sang Máy Khác](#9-hướng-dẫn-cài-đặt-cấu-hình--chuyển-sang-máy-khác)
10. [Danh Sách Tài Khoản Mẫu](#10-danh-sách-tài-khoản-mẫu)
11. [Xử Lý Sự Cố Thường Gặp (Troubleshooting)](#11-xử-lý-sự-cố-thường-gặp-troubleshooting)

---

## 1. GIỚI THIỆU TỔNG QUAN

**BabyShop Studio Kids** là giải pháp website thương mại điện tử chuyên biệt dành cho ngành hàng Mẹ & Bé, tập trung vào các mặt hàng thời trang, phụ kiện và đồ dùng cao cấp cho trẻ từ 0 - 6 tuổi.

### Các giá trị cốt lõi của hệ thống:
- **Trải nghiệm mua sắm mượt mà**: Thiết kế theo phong cách *Contemporary Boutique*, tối ưu trên cả máy tính và thiết bị di động (Mobile Bottom Navigation, Offcanvas Menu).
- **Kiểm soát kho vận chặt chẽ**: Quản lý đa biến thể chuyên sâu theo từng tổ hợp `Size` và `Màu sắc`, chống bán vượt tồn kho (overselling) và theo dõi lịch sử nhập kho qua phiếu nhập.
- **Tự động hóa thanh toán & thông báo**: Thanh toán quét mã **VietQR** chuẩn Napas 24/7 và hệ thống tự động gửi **Email HTML xác nhận đơn hàng** tức thời.
- **Tương tác thông minh**: Trợ lý ảo **Google Gemini AI 2.5 Flash** hỗ trợ tư vấn chọn size, phối đồ cho bé dựa trên độ tuổi, cân nặng và giới tính.
- **Giữ chân khách hàng (Retention)**: Gamification với Vòng quay may mắn, Điểm danh nhận thưởng 7 ngày, Kho voucher cá nhân hóa (*Voucher For You*) và Hạng thẻ thành viên tích điểm trọn đời.

---

## 2. KIẾN TRÚC KỸ THUẬT & CÔNG NGHỆ

### Backend Stack
- **Framework**: .NET 8.0 (ASP.NET Core Web App MVC)
- **Cơ sở dữ liệu**: Microsoft SQL Server / LocalDB
- **ORM**: Entity Framework Core 8.0 (tiếp cận Code-First kết hợp Migration & DbInitializer tự động)
- **Xác thực & Phân quyền**: ASP.NET Core Cookie Authentication (`BabyShopCookieAuth`) với Claims-based Authorization
- **Dependency Injection**: Tích hợp sẵn trong BCL (Scoped cho Context & Services, Transient cho HttpClients, Singleton cho Encoders)
- **Mã hóa ngôn ngữ**: Cấu hình xuất tiếng Việt UTF-8 không mã hóa (`UnicodeRanges.All`)

### Tích hợp Bên thứ ba (Third-Party Integrations)
- **Google Gemini AI**: Sử dụng model `gemini-2.5-flash` qua RESTful API, tự động xử lý prompt chuyên biệt cho sản phẩm thời trang trẻ em.
- **VietQR API**: Sinh mã QR động theo chuẩn ngân hàng quốc gia (`https://img.vietqr.io`) chứa số tài khoản, tên thụ hưởng, số tiền chính xác và mã đơn hàng tự động.
- **Google SMTP Mail**: Gửi email thông báo đơn hàng chuẩn giao thức RFC 821/2821 qua cổng 587 TLS/SSL với cơ chế lưu bản sao dự phòng (Fail-safe).

### Frontend Stack
- **View Engine**: ASP.NET Core Razor Pages / Views (.cshtml)
- **CSS Framework**: Bootstrap 5.3 kết hợp Contemporary Kids Custom Design System (`/css/babyshop.css`)
- **Icon Library**: Bootstrap Icons 1.11.3
- **Thông báo & Tương tác**: SweetAlert2 (Popup xác nhận, cảnh báo tồn kho, thông báo đăng ký)
- **Client Scripting**: Vanilla JavaScript tối ưu tốc độ, sessionStorage / localStorage an toàn theo từng phiên đăng nhập.

---

## 3. CẤU TRÚC THƯ MỤC TOÀN DỰ ÁN

```plaintext
e:\NetNangCao\WebsiteBabyShop\
│
├── Areas/
│   └── Admin/                                # Phân hệ Quản trị (Dành riêng cho Admin)
│       ├── Controllers/
│       │   ├── AuditLogsController.cs        # Xem và lọc nhật ký thao tác CSDL
│       │   ├── CategoriesController.cs       # Quản lý danh mục sản phẩm
│       │   ├── ChatController.cs             # Tiếp nhận và trả lời Live Chat với khách
│       │   ├── CouponsController.cs          # Quản lý mã giảm giá, voucher ForYou, FlashHunt
│       │   ├── DashboardController.cs        # Thống kê doanh thu, đơn hàng, biểu đồ
│       │   ├── OrdersController.cs           # Duyệt đơn, đổi trạng thái, in hóa đơn
│       │   ├── ProductsController.cs         # Thêm/Sửa/Xóa sản phẩm, biến thể, Flash Sale
│       │   ├── ReviewsController.cs          # Kiểm duyệt và phản hồi đánh giá
│       │   ├── StockReceiptsController.cs    # Lập phiếu nhập kho theo biến thể Size/Màu
│       │   └── UsersController.cs            # Quản lý tài khoản, hạng thẻ, điểm thưởng
│       └── Views/                            # Toàn bộ view Razor quản trị
│
├── Controllers/                              # Controllers phía Khách hàng
│   ├── AccountController.cs                  # Đăng nhập, Đăng ký, Đổi pass, Hồ sơ, Voucher, Đơn hàng
│   ├── CartController.cs                     # Giỏ hàng, Áp dụng Voucher, Điểm thưởng, Checkout, OrderSuccess
│   ├── ChatController.cs                     # Khung chat tư vấn AI Gemini & Live CSKH
│   ├── HomeController.cs                     # Trang chủ, Tìm kiếm, Banner, Sản phẩm mới/bán chạy
│   ├── OrderTrackingController.cs            # Tra cứu hành trình đơn hàng bằng Mã đơn & SĐT
│   ├── PolicyController.cs                   # Các trang chính sách (Đổi trả, Vận chuyển, Bảo mật)
│   ├── ProductController.cs                  # Danh sách sản phẩm, Bộ lọc đa tiêu chí, Chi tiết sản phẩm
│   ├── RewardsController.cs                  # Điểm danh 7 ngày, Vòng quay may mắn, Nhận voucher ForYou
│   └── WishlistController.cs                 # Quản lý danh sách sản phẩm yêu thích
│
├── Models/                                   # Các Entity Model & ViewModels
│   ├── AccountViewModels.cs                  # Login, Register, Profile, ChangePassword ViewModels
│   ├── AuditLog.cs                           # Entity nhật ký kiểm toán hệ thống
│   ├── BabyShopDbContext.cs                  # DbContext Entity Framework kết nối CSDL
│   ├── CartItem.cs                           # Model sản phẩm lưu trong Session giỏ hàng
│   ├── Category.cs                           # Entity danh mục sản phẩm
│   ├── ChatModels.cs                         # ChatConversation, ChatMessage
│   ├── Coupon.cs                             # Entity mã giảm giá
│   ├── DailyCheckIn.cs                       # Entity lưu lịch sử điểm danh theo ngày
│   ├── DbInitializer.cs                      # Nạp dữ liệu mẫu ban đầu và kiểm tra khởi tạo CSDL
│   ├── LuckyWheelHistory.cs                  # Entity lưu lịch sử quay thưởng
│   ├── Order.cs                              # Entity đơn hàng
│   ├── OrderDetail.cs                        # Entity chi tiết mặt hàng trong đơn
│   ├── PointTransaction.cs                   # Entity biến động số dư điểm thưởng Baby Club
│   ├── Product.cs                            # Entity sản phẩm
│   ├── ProductImage.cs                       # Entity thư viện ảnh phụ sản phẩm
│   ├── ProductReview.cs                      # Entity đánh giá & xếp hạng sao
│   ├── ProductVariant.cs                     # Entity biến thể (Size, Color, StockQuantity, Sku)
│   ├── StockReceipt.cs                       # Entity phiếu nhập kho & chi tiết phiếu nhập
│   └── User.cs                               # Entity tài khoản người dùng
│
├── Services/                                 # Tầng Business Logic & Dịch vụ ngoài
│   ├── AuditLogService.cs / IAuditLogService.cs # Ghi nhật ký kiểm toán bất đồng bộ
│   ├── EmailService.cs / IEmailService.cs       # Gửi mail SMTP, render HTML & lưu bản sao
│   ├── EmailSettings.cs                         # Model cấu hình máy chủ SMTP
│   └── GeminiAIService.cs / IAIService.cs       # Xử lý hội thoại AI tư vấn qua Google Gemini API
│
├── Helpers/                                  # Tiện ích bổ trợ
│   └── SessionExtensions.cs                  # GetObject / SetObject lưu trữ đối tượng trong Session
│
├── Views/                                    # Giao diện phía người dùng
│   ├── Account/                              # View Đăng nhập, Đăng ký, Profile, MyOrders, MyVouchers
│   ├── Cart/                                 # View Giỏ hàng (Index), Thanh toán (Checkout), Thành công (OrderSuccess)
│   ├── Home/                                 # View Trang chủ (Index), Tìm kiếm, Giới thiệu
│   ├── Product/                              # View Danh sách sản phẩm (Index), Chi tiết sản phẩm (Details)
│   ├── Rewards/                              # View Vòng quay may mắn & Điểm danh 7 ngày
│   ├── OrderTracking/                        # View Tra cứu đơn hàng
│   ├── Policy/                               # View các điều khoản chính sách
│   └── Shared/                               # _Layout.cshtml, _ValidationScriptsPartial, Header, Footer
│
├── wwwroot/                                  # File tĩnh phân phối ra client
│   ├── css/babyshop.css                      # Bộ CSS thương hiệu BabyShop
│   ├── js/                                   # Javascript xử lý tương tác
│   ├── images/                               # Hình ảnh sản phẩm, banner, logo
│   └── emails/                               # Thư mục lưu bản sao HTML các email xác nhận đơn hàng
│
├── appsettings.json                          # Cấu hình CSDL, Google Gemini API, Gmail SMTP
├── Database_WebsiteBabyShop.sql              # Script SQL Server dự phòng nạp dữ liệu thủ công
├── Program.cs                                # Điểm khởi đầu ứng dụng, cấu hình DI & Pipeline
└── WebsiteBabyShop.sln                       # Solution Visual Studio 2022
```

---

## 4. MÔ HÌNH CƠ SỞ DỮ LIỆU (DATABASE SCHEMA)

Cơ sở dữ liệu bao gồm **15 bảng** liên kết chặt chẽ:

```
[Users] ──┬──< [Orders] ──< [OrderDetails] >── [ProductVariants] >── [Products] >── [Categories]
          │                                                               │
          ├──< [UserCoupons] >── [Coupons]                                ├──< [ProductReviews]
          │                                                               └──< [ProductImages]
          ├──< [PointTransactions]
          ├──< [DailyCheckIns]
          ├──< [LuckyWheelHistories]
          └──< [ChatConversations] ──< [ChatMessages]

[StockReceipts] ──< [StockReceiptDetails] >── [ProductVariants]
[AuditLogs] (Độc lập, ghi nhận thao tác của Admin)
```

### Chi tiết các thực thể chính:

#### 1. `Users` (Người dùng & Thành viên)
- `Id` (PK, int): Mã định danh duy nhất.
- `Username` (nvarchar(50)): Tên đăng nhập.
- `PasswordHash` (nvarchar(255)): Mật khẩu đã băm (BCrypt/SHA256).
- `FullName`, `Email`, `PhoneNumber`, `Address`: Thông tin liên hệ thật.
- `Role` (nvarchar(20)): Phân quyền (`Admin` hoặc `Customer`).
- `MemberTier` (nvarchar(50)): Hạng thành viên (`Đồng`, `Bạc`, `Vàng`, `Kim Cương`).
- `TotalSpent` (decimal(18,2)): Tổng tiền đã mua hàng thành công.
- `RewardPoints` (int): Số điểm thưởng tích lũy khả dụng.
- `DailyStreakCount` (int): Chuỗi ngày điểm danh liên tục (1 đến 7).
- `LastCheckInDate` (datetime2): Ngày điểm danh gần nhất.
- `LuckyWheelSpinsAvailable` (int): Lượt quay vòng quay may mắn còn lại.

#### 2. `Products` & `ProductVariants` (Sản phẩm & Biến thể)
- `Products`: `Id`, `Name`, `Slug`, `Price`, `DiscountPrice`, `MainImageUrl`, `AgeRange`, `Gender`, `Material`, `CareInstructions`, `IsFlashSale`, `FlashSalePrice`, `FlashSaleEndTime`, `FlashSaleQuantity`, `FlashSaleSold`, `IsActive`, `CategoryId`.
- `ProductVariants`: `Id`, `ProductId` (FK), `Size` (ví dụ: `0-3M`, `6-9M`, `1-2T`), `Color` (ví dụ: `Hồng Pastel`, `Xanh Mint`), `ColorHex`, `StockQuantity` (Số lượng tồn kho thực tế), `Sku`.

#### 3. `Orders` & `OrderDetails` (Đơn hàng & Chi tiết)
- `Orders`: `Id`, `OrderCode` (BS-YYYYMMDD-XXXX), `CustomerName`, `CustomerPhone`, `CustomerEmail`, `ShippingAddress`, `Note`, `SubTotal`, `CouponCode`, `DiscountAmount`, `MemberDiscount`, `PointsUsed`, `PointDiscount`, `ShippingFee`, `TotalAmount`, `PaymentMethod` (`COD`, `BankTransfer`), `IsPaid`, `OrderStatus` (`Pending`, `Processing`, `Shipping`, `Completed`, `Cancelled`), `CreatedAt`, `UserId` (FK null).
- `OrderDetails`: `Id`, `OrderId` (FK), `ProductId` (FK), `ProductVariantId` (FK null), `ProductName`, `ProductImage`, `Size`, `Color`, `UnitPrice`, `Quantity`, `TotalPrice`.

#### 4. `Coupons` & `UserCoupons` (Mã giảm giá)
- `Coupons`: `Id`, `Code`, `Title`, `DiscountType` (`Percent`, `FixedAmount`), `DiscountValue`, `MinOrderAmount`, `MaxDiscountAmount`, `UsageLimit`, `UsedCount`, `ExpiryDate`, `IsActive`, `IsForYou` (Voucher đặc quyền), `IsFlashHunt` (Voucher săn đón).
- `UserCoupons`: `Id`, `UserId` (FK), `CouponId` (FK), `CollectedAt`, `ExpiryDate`, `IsUsed`, `UsedAt`, `OrderId` (FK null).

#### 5. `StockReceipts` & `StockReceiptDetails` (Quản lý Nhập kho)
- `StockReceipts`: `Id`, `ReceiptCode`, `SupplierName`, `TotalAmount`, `CreatedBy`, `CreatedAt`, `Note`.
- `StockReceiptDetails`: `Id`, `StockReceiptId` (FK), `ProductVariantId` (FK), `Quantity`, `ImportPrice`, `TotalPrice`.

#### 6. `AuditLogs` (Nhật ký Kiểm toán Hệ thống)
- `Id`, `UserId`, `Action` (`Create`, `Update`, `Delete`), `EntityName` (`Product`, `Order`, `Coupon`, `User`...), `RecordId`, `OldValues` (JSON), `NewValues` (JSON), `IpAddress`, `Timestamp`.

---

## 5. CHI TIẾT CHỨC NĂNG PHÍA KHÁCH HÀNG (CLIENT STOREFRONT)

### 1. Trang Chủ (`HomeController.cs`)
- Hero Banner trượt tương tác quảng bá các bộ sưu tập theo mùa.
- Thanh thông báo tiện ích cam kết chất lượng: 100% Cotton Organic, Đổi trả 7 ngày tận nhà, Freeship từ 300.000đ.
- Danh mục trực quan: Bé gái, Bé trai, Đồ sơ sinh, Giày dép & Phụ kiện.
- Khối **Flash Sale**: Hiển thị sản phẩm có giá sốc, thanh trạng thái số lượng đã bán (`FlashSaleSold` / `FlashSaleQuantity`) và đồng hồ đếm ngược kết thúc thời gian thực.

### 2. Danh Mục & Bộ Lọc Sản Phẩm (`ProductController.cs`)
- Hỗ trợ lọc đồng thời nhiều tiêu chí:
  - **Giới tính**: `Bé gái`, `Bé trai`, `Sơ sinh`.
  - **Độ tuổi**: `0-3M`, `3-6M`, `6-12M`, `1-2T`, `2-4T`, `4-6T`.
  - **Mức giá**: Dưới 100k, 100k - 200k, 200k - 300k, Trên 300k.
  - **Sắp xếp**: Mới nhất, Bán chạy nhất, Giá tăng dần, Giá giảm dần.
- Phân trang bất đồng bộ, giữ nguyên trạng thái các bộ lọc đã chọn khi chuyển trang.

### 3. Trang Chi Tiết Sản Phẩm & Đánh Giá (`Product/Details`)
- Chọn Size và Màu sắc trực quan qua nút bấm chuyển trạng thái.
- **Kiểm tra tồn kho thời gian thực**:
  - Khi chọn một cặp Size + Màu, giao diện lập tức hiển thị số lượng còn lại trong kho.
  - Nếu hết hàng: Tự động vô hiệu hóa nút *"Thêm vào giỏ hàng"* và hiển thị nhãn *"Tạm hết hàng"*.
  - Ô số lượng bị giới hạn tối đa bằng số lượng tồn kho của phân loại đó.
- **Bảng đo Size chuẩn (Size Guide Modal)**: Hướng dẫn mẹ chọn size theo chiều cao (cm) và cân nặng (kg) của bé.
- **Hệ thống Đánh giá & Đánh sao (1 - 5 sao)**:
  - Chỉ người dùng đã đăng nhập mới được viết bình luận và gửi xếp hạng sao.
  - Tự động tính điểm đánh giá trung bình hiển thị ra đầu trang.

### 4. Giỏ Hàng (`Cart/Index`)
- Quản lý số lượng mặt hàng với nút tăng/giảm số lượng mượt mà.
- Kiểm tra lại tồn kho: Nếu số lượng đặt lớn hơn số lượng tồn kho còn lại, hệ thống tự động điều chỉnh về số tối đa kèm thông báo cảnh báo.
- Xem trước chi phí: Tạm tính, Phí ship (Tự động miễn phí nếu đơn >= 300.000đ).

### 5. Đặt Hàng & Thanh Toán (`Cart/Checkout`)
- **Tự động đối chiếu hồ sơ người dùng thật**:
  - Tự động trích xuất Họ tên, SĐT, Email, Địa chỉ từ tài khoản đã đăng nhập.
  - Đánh dấu trực quan các trường đã có sẵn (*"Từ tài khoản"*).
  - **Bắt buộc nhập địa chỉ chi tiết** nếu tài khoản chưa có địa chỉ (hiển thị cảnh báo vàng).
  - Sau khi đặt hàng thành công, tự động lưu thông tin giao hàng mới này vào tài khoản làm địa chỉ mặc định cho các lần mua sau.
- **Tính toán chiết khấu đa tầng**:
  1. *Chiết khấu Voucher*: Áp dụng mã giảm giá nhập tay hoặc chọn từ Kho Voucher.
  2. *Chiết khấu Thành viên Baby Club*: Tự động áp dụng theo hạng thẻ (Đồng 3%, Bạc 8%, Vàng 10%, Kim Cương 20%).
  3. *Điểm thưởng Baby Club*: Khách hàng có thể chọn số điểm muốn dùng (100 điểm = 10.000đ) để trừ trực tiếp vào đơn.
  4. *Miễn phí vận chuyển*: Tự động áp dụng freeship theo hạng thẻ hoặc điều kiện đơn hàng.
- **Lựa chọn phương thức thanh toán**:
  - `COD`: Thanh toán tiền mặt khi nhận hàng.
  - `BankTransfer`: Chuyển khoản ngân hàng tự động với mã VietQR.

### 6. Trang Hoàn Tất Đơn Hàng (`Cart/OrderSuccess`)
- Hiển thị thông báo đặt hàng thành công kèm mã đơn `#BS-YYYYMMDD-XXXX`.
- **Khung mã VietQR tự động** (nếu chọn Chuyển khoản):
  - Hiển thị hình ảnh mã QR ngân hàng MB Bank (STK `212882`, Chủ TK `MAI TIEN DUNG`).
  - Các nút *"Sao chép"* tiện lợi cho STK, Số tiền và Nội dung chuyển khoản.
- **Thông báo Email Xác Nhận**:
  - Banner thông báo xác nhận: *"Email xác nhận đơn hàng đã được gửi thành công tới [email người nhận]!"*.
  - Nút bấm **"Xem nội dung email xác nhận"** mở xem trước bản sao email HTML vừa sinh ra.

### 7. Baby Club & Gamification (`RewardsController.cs`)
- **Điểm danh 7 ngày liên tiếp (Streak Check-in)**:
  - Mỗi tài khoản được điểm danh 1 lần/ngày.
  - Hệ thống kiểm tra chuỗi ngày liên tiếp: Ngày 1 nhận 50 điểm, Ngày 2 nhận 60 điểm... Ngày 7 nhận quà lớn 200 điểm + 1 Lượt quay vòng quay may mắn.
  - Nếu bỏ lỡ 1 ngày, chuỗi tự động quay về Ngày 1.
- **Vòng quay may mắn (Lucky Wheel)**:
  - Quay trúng các phần thưởng: +50 Điểm, +100 Điểm, Voucher giảm 50k, Quà gấu bông BabyShop, Thêm lượt quay.
  - Cơ chế quay bằng CSS3 Canvas động, chống spam quay nhiều lần.
- **Kho Voucher của tôi (`Account/MyVouchers`)**:
  - Quản lý toàn bộ voucher đã lưu.
  - Nhận voucher đặc quyền *Voucher For You* (được hệ thống gợi ý ngẫu nhiên mỗi 24 giờ).
  - Nút sao chép mã và chuyển thẳng tới trang Checkout để sử dụng.

### 8. Tra Cứu Đơn Hàng Không Cần Đăng Nhập (`OrderTrackingController.cs`)
- Khách hàng chỉ cần nhập **Mã đơn hàng** và **Số điện thoại** đặt hàng.
- Hiển thị timeline tiến độ trực quan: `Đã tiếp nhận` ➔ `Đang xử lý đóng gói` ➔ `Đang giao hàng` ➔ `Giao hàng thành công`.

---

## 6. CHI TIẾT CHỨC NĂNG PHÂN HỆ QUẢN TRỊ (ADMIN AREA)

Truy cập đường dẫn: `/Admin` (Yêu cầu đăng nhập tài khoản có `Role = "Admin"`).

### 1. Dashboard Thống Kê (`Admin/Dashboard`)
- 4 thẻ KPI chỉ số quan trọng: Tổng doanh thu hôm nay, Số đơn hàng mới, Tổng khách hàng, Số sản phẩm sắp hết hàng (kho <= 5).
- Biểu đồ đường doanh thu 7 ngày gần nhất.
- Bảng danh sách đơn hàng mới cần duyệt gấp.
- Danh sách top sản phẩm bán chạy nhất tháng.

### 2. Quản Lý Sản Phẩm & Biến Thể (`Admin/Products`)
- Danh sách sản phẩm kèm bộ lọc tìm kiếm theo danh mục và trạng thái.
- Form Thêm mới / Chỉnh sửa:
  - Thông tin cơ bản: Tên, Mã SKU, Danh mục, Giá niêm yết, Giá khuyến mãi, Chất liệu, Độ tuổi, Giới tính.
  - Upload hình ảnh chính và thư viện ảnh chi tiết.
  - Thiết lập Flash Sale: Bật/Tắt, Giờ kết thúc, Giá Flash Sale, Số lượng bán Flash Sale.
  - **Bảng quản lý Biến thể động**: Thêm/sửa nhiều biến thể Size (0-3M, 6-9M...) và Màu sắc kèm mã màu Hex và số lượng tồn kho ban đầu.

### 3. Quản Lý Nhập Kho (`Admin/StockReceipts`)
- Giải quyết bài toán nhập hàng thực tế từ nhà sản xuất.
- Tạo phiếu nhập kho: Chọn nhà cung cấp, ghi chú.
- Chi tiết phiếu nhập: Chọn sản phẩm ➔ Chọn chính xác phân loại Size & Màu sắc ➔ Nhập số lượng và đơn giá nhập.
- Sau khi lưu phiếu: Hệ thống tự động **cộng dồn số lượng tồn kho** vào đúng biến thể trong CSDL và ghi nhận lịch sử vào `AuditLogs`.

### 4. Quản Lý Đơn Hàng (`Admin/Orders`)
- Lọc đơn hàng theo trạng thái: Chờ duyệt, Đang chuẩn bị, Đang giao, Hoàn thành, Đã hủy.
- Xem chi tiết đơn: Danh sách món đồ, size, màu, giảm giá, thông tin giao hàng.
- Thao tác nhanh:
  - Duyệt đơn và chuyển trạng thái giao hàng.
  - Cập nhật trạng thái thanh toán (Đã thanh toán / Chưa thanh toán).
  - In hóa đơn bán lẻ chuẩn khổ in A4/A5 để dán lên kiện hàng.

### 5. Quản Lý Mã Giảm Giá (`Admin/Coupons`)
- Tạo mã khuyến mãi mới: Mã Code, Tiêu đề, Loại giảm (% hoặc Tiền mặt), Giá trị giảm, Đơn tối thiểu, Giảm tối đa, Giới hạn lượt dùng, Ngày hết hạn.
- Phân loại đặc biệt:
  - `IsForYou`: Dành riêng cho hệ thống phân phối quà tặng tự động.
  - `IsFlashHunt`: Dành riêng cho các sự kiện săn voucher chớp nhoáng.

### 6. Quản Lý Khách Hàng & Thành Viên (`Admin/Users`)
- Danh sách thành viên kèm phân hạng thẻ (`Đồng`, `Bạc`, `Vàng`, `Kim Cương`).
- Xem chi tiết lịch sử mua sắm và tổng tiền đã đóng góp của khách hàng.
- Quản lý điểm thưởng: Cho phép Admin cộng hoặc trừ điểm thưởng thủ công (kèm lý do ghi nhận vào `PointTransactions`).
- Khóa / Mở khóa tài khoản người dùng vi phạm chính sách.

### 7. Kiểm Duyệt Đánh Giá (`Admin/Reviews`)
- Danh sách toàn bộ đánh giá của khách hàng từ trang sản phẩm.
- Duyệt hiển thị hoặc ẩn các đánh giá không phù hợp.
- Phản hồi bình luận từ phía Quản trị viên (chăm sóc khách hàng).

### 8. Nhật Ký Kiểm Toán Hệ Thống (`Admin/AuditLogs`)
- Tự động ghi nhận mọi hành động: **Thêm mới**, **Cập nhật**, **Xóa** trên các bảng dữ liệu quan trọng (`Product`, `Order`, `Coupon`, `User`...).
- Lưu chi tiết: Ai làm (`UserId`), vào thời điểm nào (`Timestamp`), địa chỉ IP nào, giá trị cũ (`OldValues`) và giá trị mới (`NewValues`) dưới định dạng JSON.
- Đảm bảo tính minh bạch và truy vết khi có sự cố dữ liệu.

### 9. Trung Tâm Live Chat CSKH (`Admin/Chat`)
- Giao diện tiếp nhận tin nhắn khách hàng dạng Chat Messenger.
- Hiển thị danh sách khách đang chờ hỗ trợ kèm chấm đỏ báo tin nhắn mới.
- Nhân viên Admin có thể chat trực tiếp với khách để chốt đơn hoặc tư vấn đổi trả.

---

## 7. CÁC QUY TRÌNH NGHIỆP VỤ CỐT LÕI (CORE WORKFLOWS)

### 7.1 Quy trình Quản lý Tồn kho Biến thể (Stock Management)
```
[Nhập hàng mới] ──> Lập Phiếu Nhập Kho (StockReceipt) ──> Tự động cộng tồn kho (ProductVariants.StockQuantity)
                                                                       │
[Khách mua hàng] <── [Thanh toán thành công] <── Kiểm tra kho hợp lệ <─┘
         │
         └──> Tự động trừ tồn kho (ProductVariants.StockQuantity)
```
- **Tại trang chi tiết sản phẩm**: Client gửi truy vấn AJAX lấy số lượng tồn kho của cặp `(Size, Color)`. Nếu tồn kho = 0, giao diện khóa nút thêm vào giỏ.
- **Tại giỏ hàng**: Mỗi khi mở giỏ hàng hoặc bấm thanh toán, hệ thống truy vấn lại CSDL thực tế. Nếu khách chọn số lượng vượt quá số tồn kho hiện có, giỏ hàng tự động ép về số tồn kho tối đa kèm cảnh báo đỏ.
- **Khi hoàn tất Checkout**: Trong cùng một Transaction CSDL, hệ thống trừ tồn kho `ProductVariants.StockQuantity = StockQuantity - Quantity`. Nếu là sản phẩm Flash Sale, tự động cộng dồn `FlashSaleSold += Quantity`.

---

### 7.2 Quy trình Đặt hàng & Xác thực Địa chỉ (Checkout Flow)
- **Kiểm tra đăng nhập**: Bắt buộc người dùng phải đăng nhập tài khoản trước khi hoàn tất đặt hàng (nhằm tích lũy điểm thưởng và bảo vệ quyền lợi thành viên).
- **Đối chiếu dữ liệu hồ sơ**:
  - Trích xuất thông tin tài khoản hiện tại.
  - Bắt buộc kiểm tra 4 trường: `CustomerName`, `CustomerPhone`, `CustomerEmail`, `ShippingAddress`.
  - Nếu tài khoản chưa có địa chỉ, chặn submit form tại cả Client (HTML5 Validation) và Server (ModelState Validation), hiển thị thông báo yêu cầu nhập địa chỉ cụ thể.
- **Lưu địa chỉ mặc định**: Khi đơn hàng được tạo thành công, hệ thống tự động cập nhật `User.Address = order.ShippingAddress` nếu trước đó tài khoản chưa có địa chỉ.
- **Xóa Session**: Dọn sạch session giỏ hàng (`ShoppingCart`) và session voucher (`AppliedCouponCode`) ngay sau khi đơn hàng được ghi nhận thành công.

---

### 7.3 Hệ thống Thành viên Baby Club & Tích lũy Điểm thưởng
- **Bảng phân hạng thành viên**:
  | Hạng thẻ | Điều kiện chi tiêu | Quyền lợi chiết khấu | Quyền lợi vận chuyển |
  | :--- | :--- | :--- | :--- |
  | **Đồng (Bronze)** | 0 - 5.000.000đ | Giảm **3%** trên tổng tiền hàng | Freeship đơn từ 300k |
  | **Bạc (Silver)** | 5.000.000đ - 15.000.000đ | Giảm **8%** trên tổng tiền hàng | Freeship đơn từ 200k |
  | **Vàng (Gold)** | 15.000.000đ - 30.000.000đ | Giảm **10%** trên tổng tiền hàng | Freeship đơn từ 100k |
  | **Kim Cương (Diamond)** | Trên 30.000.000đ | Giảm **20%** trên tổng tiền hàng | **Miễn phí vận chuyển mọi đơn hàng trọn đời** |

- **Cơ chế Tích điểm & Tiêu điểm**:
  - *Tích điểm*: Mỗi **1.000đ** thanh toán thành công được tích lũy **1 điểm** (100.000đ = 100 điểm).
  - *Tiêu điểm*: **100 điểm** tương đương **10.000đ** giảm giá trực tiếp vào đơn hàng.
  - Mọi biến động điểm đều được lưu lại trong bảng `PointTransactions` để tra cứu minh bạch.

---

### 7.4 Thanh toán Tự động VietQR 24/7
- Khi khách hàng chọn phương thức thanh toán **Chuyển khoản ngân hàng (`BankTransfer`)**:
  - Hệ thống tự động gọi API sinh mã QR:  
    `https://img.vietqr.io/image/{BankId}-{AccountNo}-compact2.png?amount={TotalAmount}&addInfo={OrderCode}&accountName={AccountName}`
  - Trong đó:
    - `BankId`: `MB` (Ngân hàng Quân Đội)
    - `AccountNo`: `212882`
    - `AccountName`: `MAI TIEN DUNG`
    - `Amount`: Tổng tiền chính xác sau khi đã trừ voucher, giảm giá thành viên và điểm thưởng
    - `AddInfo`: Mã đơn hàng duy nhất (`BS-YYYYMMDD-XXXX`)
- Khách hàng mở bất kỳ App ngân hàng hoặc Ví điện tử (Momo, ZaloPay, ViettelPay) quét mã QR, toàn bộ thông tin số tiền và nội dung sẽ được điền tự động 100%, không cần gõ tay.

---

### 7.5 Dịch vụ Gửi Email Xác nhận Đơn hàng (Email Service)
- **Triển khai kỹ thuật**: `Services/EmailService.cs` cài đặt giao diện `IEmailService.cs`.
- **Giao thức**: Sử dụng `System.Net.Mail.SmtpClient` kết nối tới `smtp.gmail.com:587` với mã hóa TLS/SSL.
- **Nội dung Email HTML chuẩn thương hiệu**:
  - Header gradient hồng pastel `BabyShop Studio Kids`.
  - Huy hiệu trạng thái: `✓ ĐẶT HÀNG THÀNH CÔNG`.
  - Bảng thông tin người nhận, số điện thoại, địa chỉ và phương thức thanh toán.
  - Bảng chi tiết mặt hàng: Tên, phân loại Size & Màu, số lượng, đơn giá, thành tiền.
  - Bảng tổng kết tài chính: Tiền hàng, giảm giá voucher, giảm giá thành viên, điểm thưởng, phí ship, và **Tổng thanh toán**.
  - **Khung hướng dẫn VietQR trong Email**: Nếu đơn hàng chọn Chuyển khoản, email sẽ tự động nhúng hình ảnh mã VietQR kèm số tài khoản MB Bank để khách hàng có thể quét mã thanh toán trực tiếp ngay trong email.
  - Nút bấm tra cứu đơn hàng trực tiếp và thông tin Hotline hỗ trợ 1900 6868.
- **Cơ chế Fail-safe & Sao lưu**:
  - Gửi email hoàn toàn bất đồng bộ trong khối `try-catch`, đảm bảo nếu máy chủ SMTP gặp lỗi hoặc mất mạng thì **đơn hàng của khách vẫn được lưu thành công tuyệt đối**.
  - Luôn tự động lưu một bản sao email HTML tại `wwwroot/emails/order-{OrderCode}.html` để quản trị viên và khách hàng có thể xem lại bất cứ lúc nào.

---

### 7.6 Trợ lý Tư vấn Bán hàng Trí tuệ Nhân tạo (Google Gemini AI)
- **Triển khai kỹ thuật**: `Services/GeminiAIService.cs` kế thừa `IAIService.cs`.
- **Mô hình AI**: Sử dụng model tiên tiến nhất của Google: **`gemini-2.5-flash`**.
- **System Instruction**: AI được huấn luyện với vai trò Chuyên gia tư vấn thời trang trẻ em của BabyShop Studio Kids:
  - Am hiểu chất liệu vải an toàn cho làn da nhạy cảm của bé (Cotton 100%, sợi tre Bamboo, Organic cotton).
  - Nắm rõ bảng size chuẩn theo tháng tuổi và cân nặng (kg).
  - Tự động gợi ý các danh mục và sản phẩm đang có sẵn trên website kèm lời khuyên chân thành, chu đáo.

---

### 7.7 Hệ thống Nhật ký Kiểm toán (Audit Logs)
- Tự động ghi vết khi Admin thực hiện các hành động can thiệp dữ liệu:
  - Thêm / Sửa / Xóa sản phẩm hoặc biến thể.
  - Cập nhật trạng thái đơn hàng.
  - Tạo mới phiếu nhập kho.
  - Điều chỉnh điểm thưởng của thành viên.
  - Bật / Tắt mã giảm giá.
- Cung cấp giao diện lọc tại `/Admin/AuditLogs` theo: Tên bảng dữ liệu, Hành động (`Create`, `Update`, `Delete`), Khoảng thời gian.

---

## 8. DANH MỤC CONTROLLER & ENDPOINTS

### Phân hệ Khách hàng (Client Endpoints)
| Controller | Action / Endpoint | Phương thức | Mô tả chức năng |
| :--- | :--- | :---: | :--- |
| `Home` | `/` hoặc `/Home/Index` | GET | Trang chủ, banner, sản phẩm nổi bật, flash sale |
| `Home` | `/Home/Search?q=...` | GET | Tìm kiếm sản phẩm theo từ khóa |
| `Product` | `/Product/Index` | GET | Danh sách sản phẩm, lọc theo tuổi, giới tính, giá, màu |
| `Product` | `/Product/Details/{id}` | GET | Chi tiết sản phẩm, chọn size/màu, xem tồn kho |
| `Product` | `/Product/SubmitReview` | POST | Gửi đánh giá sao & nhận xét sản phẩm |
| `Cart` | `/Cart` hoặc `/Cart/Index` | GET | Xem giỏ hàng hiện tại |
| `Cart` | `/Cart/AddToCart` | POST | Thêm sản phẩm vào giỏ hàng (kèm kiểm tra tồn kho) |
| `Cart` | `/Cart/UpdateQuantity` | POST | Cập nhật số lượng sản phẩm trong giỏ |
| `Cart` | `/Cart/RemoveItem` | POST | Xóa sản phẩm khỏi giỏ hàng |
| `Cart` | `/Cart/ApplyCoupon` | POST | Áp dụng mã giảm giá |
| `Cart` | `/Cart/RemoveCoupon` | POST | Gỡ bỏ mã giảm giá |
| `Cart` | `/Cart/Checkout` | GET | Trang thanh toán, kiểm tra hồ sơ và địa chỉ |
| `Cart` | `/Cart/Checkout` | POST | Xử lý tạo đơn hàng, trừ kho, tích điểm, gửi email |
| `Cart` | `/Cart/OrderSuccess/{id}`| GET | Trang xác nhận đặt hàng thành công, hiển thị VietQR |
| `Account` | `/Account/Login` | GET/POST | Đăng nhập tài khoản |
| `Account` | `/Account/Register` | GET/POST | Đăng ký tài khoản mới |
| `Account` | `/Account/Logout` | POST | Đăng xuất và dọn sạch session cache |
| `Account` | `/Account/Profile` | GET/POST | Xem và cập nhật thông tin cá nhân |
| `Account` | `/Account/MyOrders` | GET | Lịch sử đơn hàng của tôi |
| `Account` | `/Account/MyVouchers` | GET | Kho voucher của tôi |
| `Rewards` | `/Rewards` | GET | Giao diện Vòng quay may mắn & Điểm danh 7 ngày |
| `Rewards` | `/Rewards/CheckIn` | POST | Thực hiện điểm danh nhận điểm thưởng |
| `Rewards` | `/Rewards/SpinWheel` | POST | Quay vòng quay may mắn |
| `OrderTracking` | `/OrderTracking` | GET/POST | Tra cứu tiến độ đơn hàng |
| `Wishlist` | `/Wishlist` | GET | Quản lý danh sách sản phẩm yêu thích |
| `Chat` | `/Chat/SendMessage` | POST | Gửi tin nhắn tới AI Gemini hoặc CSKH |

### Phân hệ Quản trị (Admin Endpoints - `/Admin`)
| Controller | Action / Endpoint | Phương thức | Mô tả chức năng |
| :--- | :--- | :---: | :--- |
| `Dashboard` | `/Admin` hoặc `/Admin/Dashboard` | GET | Bảng điều khiển thống kê kinh doanh |
| `Products` | `/Admin/Products` | GET | Quản lý danh sách sản phẩm |
| `Products` | `/Admin/Products/Create` | GET/POST | Thêm mới sản phẩm và các biến thể size/màu |
| `Products` | `/Admin/Products/Edit/{id}` | GET/POST | Chỉnh sửa thông tin sản phẩm và biến thể |
| `Products` | `/Admin/Products/Delete/{id}` | POST | Xóa sản phẩm |
| `StockReceipts` | `/Admin/StockReceipts` | GET | Danh sách phiếu nhập kho |
| `StockReceipts` | `/Admin/StockReceipts/Create` | GET/POST | Tạo phiếu nhập kho theo biến thể Size & Màu |
| `Orders` | `/Admin/Orders` | GET | Quản lý danh sách đơn hàng |
| `Orders` | `/Admin/Orders/Details/{id}` | GET | Xem chi tiết đơn hàng |
| `Orders` | `/Admin/Orders/UpdateStatus` | POST | Cập nhật trạng thái đơn (Pending, Processing...) |
| `Orders` | `/Admin/Orders/PrintInvoice/{id}` | GET | Xem và in hóa đơn bán hàng |
| `Coupons` | `/Admin/Coupons` | GET | Quản lý danh sách mã giảm giá |
| `Coupons` | `/Admin/Coupons/Create` | GET/POST | Tạo mã giảm giá mới |
| `Users` | `/Admin/Users` | GET | Quản lý danh sách tài khoản người dùng |
| `Users` | `/Admin/Users/AdjustPoints` | POST | Cộng / Trừ điểm thưởng thành viên |
| `Reviews` | `/Admin/Reviews` | GET | Quản lý và kiểm duyệt đánh giá |
| `AuditLogs` | `/Admin/AuditLogs` | GET | Tra cứu nhật ký kiểm toán hệ thống |
| `Chat` | `/Admin/Chat` | GET/POST | Trực tiếp trả lời tin nhắn của khách hàng |

---

## 9. HƯỚNG DẪN CÀI ĐẶT, CẤU HÌNH & CHUYỂN SANG MÁY KHÁC

### 1. Yêu cầu Môi trường
- Hệ điều hành: Windows 10/11, macOS, hoặc Linux.
- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (bản 8.0.100 trở lên).
- **SQL Server**: Khuyên dùng **SQL Server LocalDB** (đi kèm Visual Studio) hoặc **SQL Server Express 2019/2022**.
- Công cụ phát triển: **Visual Studio 2022** (khuyên dùng, chọn workload *"ASP.NET and web development"*) hoặc **Visual Studio Code** (cài extension *C# Dev Kit*).

---

### 2. Cấu hình file `appsettings.json`
Mở file `appsettings.json` tại thư mục gốc dự án:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WebsiteBabyShopDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AIService": {
    "Provider": "Gemini",
    "ApiKey": "AIzaSyDS_9Yad3L0ebdv5fKsEhZ2KwVh61dO36s",
    "Model": "gemini-2.5-flash"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "SenderName": "BabyShop Studio Kids",
    "SenderEmail": "kutunoi206@gmail.com",
    "Username": "kutunoi206@gmail.com",
    "Password": "hfjm prfj aqqx xdqb",
    "SaveToDisk": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> **Hướng dẫn cấu hình:**
> - **ConnectionStrings**: Nếu máy mới dùng SQL Server Express thay vì LocalDB, bạn chỉ cần đổi `(localdb)\\mssqllocaldb` thành `.\\SQLEXPRESS` hoặc `localhost`.
> - **AIService**: Đã tích hợp sẵn API Key Google Gemini hoạt động ổn định.
> - **EmailSettings**: Để gửi email thật tới khách hàng, sử dụng Gmail của bạn và tạo Mật khẩu ứng dụng 16 ký tự tại: [https://myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords).

---

### 3. Quy trình Chuyển dự án sang Máy tính khác

#### Bước A: Dọn dẹp trên máy cũ trước khi nén gửi
Mở thư mục dự án và **xóa các thư mục build tạm thời** để file zip nhẹ và không bị xung đột:
- Xóa thư mục `bin/`
- Xóa thư mục `obj/`
- Xóa thư mục `.vs/` (thư mục ẩn)
- Nén toàn bộ thư mục `WebsiteBabyShop` thành file `WebsiteBabyShop.zip` và copy sang máy mới.

#### Bước B: Chạy trên máy tính mới
1. Giải nén thư mục dự án vào một ổ đĩa bất kỳ (ví dụ: `D:\WebsiteBabyShop`).
2. Mở file `WebsiteBabyShop.sln` bằng **Visual Studio 2022**.
3. Nhấn **F5** (hoặc nút **Run**).
4. **Cơ chế Khởi tạo CSDL Tự động (`DbInitializer`)**:
   - Khi chạy lần đầu, hệ thống tự động kiểm tra CSDL. Nếu chưa có CSDL `WebsiteBabyShopDb`, lệnh `context.Database.EnsureCreated()` sẽ **tự động tạo toàn bộ bảng và nạp đầy đủ danh mục, sản phẩm, biến thể và tài khoản mẫu**. Bạn hoàn toàn không cần chạy script SQL bằng tay.

#### Cách chạy bằng dòng lệnh (Terminal / PowerShell):
```bash
# Di chuyển vào thư mục dự án
cd e:\NetNangCao\WebsiteBabyShop

# Phục hồi thư viện
dotnet restore

# Biên dịch dự án (đảm bảo 0 Warning, 0 Error)
dotnet build

# Chạy web server
dotnet run --urls=http://localhost:5299
```
Mở trình duyệt truy cập: **`http://localhost:5299`**

---

## 10. DANH SÁCH TÀI KHOẢN MẪU

Hệ thống đã chuẩn bị sẵn các tài khoản mẫu với các phân quyền và hạng thành viên khác nhau để kiểm thử:

| Tài khoản | Mật khẩu | Vai trò (Role) | Hạng thành viên | Đặc điểm kiểm thử |
| :--- | :--- | :---: | :---: | :--- |
| **`admin`** | `Admin@123` | **Admin** | Quản trị viên | Toàn quyền quản trị hệ thống tại `/Admin` |
| **`maitiendung`** | `Password123!` | Customer | **Kim Cương** | Hạng cao nhất: Giảm 20% mọi đơn hàng, Freeship trọn đời |
| **`khachhang`** | `123456` | Customer | **Bạc** | Đã có sẵn địa chỉ (Hà Nội), giảm 8%, Freeship đơn từ 200k |
| **`me_thao_1465`** | `Password@123` | Customer | **Đồng** | Khách hàng mới, trải nghiệm tích lũy điểm thưởng và chuỗi điểm danh |

---

## 11. XỬ LÝ SỰ CỐ THƯỜNG GẶP (TROUBLESHOOTING)

### 1. Lỗi kết nối CSDL (Cannot connect to database / LocalDB error)
- **Hiện tượng**: Báo lỗi `SqlException: Cannot open database "WebsiteBabyShopDb" requested by the login`.
- **Khắc phục**:
  - Mở CMD chạy lệnh: `sqllocaldb start mssqllocaldb` để khởi động LocalDB.
  - Hoặc mở file [Database_WebsiteBabyShop.sql](file:///e:/NetNangCao/WebsiteBabyShop/Database_WebsiteBabyShop.sql) trong SQL Server Management Studio (SSMS) và nhấn **Execute (F5)** để nạp CSDL thủ công.

### 2. Lỗi cổng mạng đang bị chiếm dụng (Port 5299 already in use)
- **Khắc phục**: Đổi cổng trong file `Properties/launchSettings.json` hoặc chạy với tham số cổng mới:
  ```bash
  dotnet run --urls=http://localhost:5000
  ```

### 3. Lỗi tiếng Việt bị mã hóa ký tự lạ (Mojibake)
- Toàn bộ file mã nguồn C# và Razor (.cshtml) trong dự án đã được chuyển đổi sang chuẩn **UTF-8 with BOM**.
- Trong `Program.cs` đã tích hợp cấu hình:
  ```csharp
  builder.Services.AddSingleton(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.All }));
  ```
  Giúp toàn bộ ký tự tiếng Việt xuất ra trình duyệt đẹp mắt, không bị mã hóa thành các ký tự `&#x...;`.

### 4. Gửi email xác nhận không nhận được hộp thư
- Kiểm tra lại mật khẩu ứng dụng Gmail (App Password 16 ký tự) trong `appsettings.json`.
- Kiểm tra thư mục **Spam (Thư rác)** hoặc **Quảng cáo** trong Gmail.
- Hệ thống luôn lưu một bản sao email HTML tại `wwwroot/emails/order-{OrderCode}.html` để bạn có thể kiểm tra nội dung ngay lập tức mà không phụ thuộc vào kết nối mạng.

---

## 📞 LIÊN HỆ & BẢN QUYỀN
- **Dự án**: Website Thương mại Điện tử Thời trang & Đồ dùng Cho Bé - BabyShop Studio Kids
- **Đơn vị phát triển**: Nhóm Nghiên cứu & Phát triển Công nghệ .NET Nâng Cao
- **Hotline CSKH**: 1900 6868 | **Email**: babyshop.cskh@gmail.com
- **Địa chỉ Showroom**: Số 123 Đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội.
