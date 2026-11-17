using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Helpers;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ShoppingCart";
        private const string CouponSessionKey = "AppliedCouponCode";
        private const string CheckoutTokenSessionKey = "CheckoutIdempotencyToken";
        private const string LastCompletedOrderSessionKey = "LastCompletedCheckoutOrder";
        private readonly BabyShopDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<CartController> _logger;

        public CartController(BabyShopDbContext context, IEmailService emailService, ILogger<CartController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CartSessionKey, cart);
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của mã giảm giá và tính toán số tiền giảm giá chính xác
        /// (bao gồm điều kiện đơn hàng tối thiểu và giới hạn giảm giá tối đa)
        /// </summary>
        private async Task<(Coupon? coupon, decimal discountAmount, string? error, bool isValid)> EvaluateCouponAsync(string? couponCode, decimal subTotal, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return (null, 0, null, false);
            }

            var cleanCode = couponCode.Trim().ToUpper();
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == cleanCode && c.IsActive);

            if (coupon == null)
            {
                return (null, 0, $"Mã giảm giá \"{couponCode.Trim()}\" không tồn tại hoặc đã ngừng áp dụng.", false);
            }

            var now = DateTime.Now;
            if (now < coupon.StartDate)
            {
                var diff = coupon.StartDate - now;
                string timeRemainingStr;
                if (diff.TotalDays >= 1)
                {
                    timeRemainingStr = $"{(int)diff.TotalDays} ngày {diff.Hours} giờ";
                }
                else if (diff.TotalHours >= 1)
                {
                    timeRemainingStr = $"{diff.Hours} giờ {diff.Minutes} phút";
                }
                else
                {
                    timeRemainingStr = $"{diff.Minutes} phút {diff.Seconds} giây";
                }

                string msg = coupon.IsFlashHunt
                    ? $"Mã giảm giá \"{coupon.Code}\" là ưu đãi Săn Giờ Vàng, sẽ chính thức mở vào lúc {coupon.StartDate:HH:mm dd/MM/yyyy} (còn lại {timeRemainingStr}). Mẹ hãy quay lại đúng giờ để săn mã nhé!"
                    : $"Mã giảm giá \"{coupon.Code}\" có hiệu lực từ {coupon.StartDate:HH:mm dd/MM/yyyy} (còn lại {timeRemainingStr}).";

                return (coupon, 0, msg, false);
            }

            if (coupon.ExpiryDate.HasValue && now > coupon.ExpiryDate.Value)
            {
                return (coupon, 0, $"Mã giảm giá \"{coupon.Code}\" đã hết hạn sử dụng vào ngày {coupon.ExpiryDate.Value:dd/MM/yyyy}.", false);
            }

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                return (coupon, 0, $"Mã giảm giá \"{coupon.Code}\" đã hết lượt sử dụng.", false);
            }

            // Kiểm tra ràng buộc đặc quyền: Mã "Dành Cho Bạn" chỉ áp dụng cho riêng tài khoản được cấp
            if (coupon.IsForYou || coupon.AssignedUserId.HasValue)
            {
                if (!userId.HasValue || userId.Value <= 0)
                {
                    return (coupon, 0, "Mã giảm giá này là đặc quyền Dành Cho Bạn, vui lòng đăng nhập tài khoản sở hữu để sử dụng.", false);
                }

                if (coupon.AssignedUserId.HasValue && coupon.AssignedUserId.Value != userId.Value)
                {
                    return (coupon, 0, $"Mã giảm giá \"{coupon.Code}\" là đặc quyền Dành Cho Bạn của tài khoản khác, bạn không thể sử dụng.", false);
                }
            }

            // Kiểm tra ràng buộc: Mỗi tài khoản chỉ được sử dụng mã giảm giá 1 lần duy nhất
            if (userId.HasValue && userId.Value > 0)
            {
                // 1. Kiểm tra xem người dùng đã từng dùng mã này trong đơn hàng trước đó chưa
                bool hasUsedInOrders = await _context.Orders.AnyAsync(o => o.UserId == userId.Value && o.CouponCode != null && o.CouponCode.ToUpper() == cleanCode && o.OrderStatus != "Cancelled");
                if (hasUsedInOrders)
                {
                    return (coupon, 0, $"Tài khoản của bạn đã sử dụng mã giảm giá \"{coupon.Code}\" cho đơn hàng trước. Mỗi tài khoản chỉ được áp dụng mã 1 lần duy nhất!", false);
                }

                // 2. Kiểm tra voucher trong kho của tài khoản (UserCoupon)
                var userCoupon = await _context.UserCoupons
                    .FirstOrDefaultAsync(uc => uc.UserId == userId.Value && uc.CouponId == coupon.Id);

                if (userCoupon != null)
                {
                    if (userCoupon.IsUsed)
                    {
                        return (coupon, 0, $"Mã giảm giá \"{coupon.Code}\" trong kho voucher của bạn đã được sử dụng.", false);
                    }
                    if (userCoupon.ExpiryDate < now)
                    {
                        return (coupon, 0, $"Mã giảm giá \"{coupon.Code}\" trong kho của bạn đã hết hạn sử dụng vào ngày {userCoupon.ExpiryDate:dd/MM/yyyy HH:mm}.", false);
                    }
                }
            }

            // Kiểm tra giá trị đơn hàng tối thiểu
            if (coupon.MinOrderAmount > 0 && subTotal < coupon.MinOrderAmount)
            {
                decimal missing = coupon.MinOrderAmount - subTotal;
                return (coupon, 0, $"Mã \"{coupon.Code}\" yêu cầu đơn hàng tối thiểu {coupon.MinOrderAmount:N0} đ (mẹ mua thêm {missing:N0} đ để dùng mã nhé).", false);
            }

            // Tính tiền giảm giá
            decimal discount = 0;
            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discount = Math.Round(subTotal * (coupon.DiscountValue / 100m), 0);
                if (coupon.MaxDiscountAmount.HasValue && coupon.MaxDiscountAmount.Value > 0 && discount > coupon.MaxDiscountAmount.Value)
                {
                    discount = coupon.MaxDiscountAmount.Value;
                }
            }
            else // FixedAmount
            {
                discount = coupon.DiscountValue;
                if (coupon.MaxDiscountAmount.HasValue && coupon.MaxDiscountAmount.Value > 0 && discount > coupon.MaxDiscountAmount.Value)
                {
                    discount = coupon.MaxDiscountAmount.Value;
                }
            }

            // Số tiền giảm không được vượt quá tiền hàng
            if (discount > subTotal)
            {
                discount = subTotal;
            }

            return (coupon, discount, null, true);
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            bool cartModified = false;

            // Đồng bộ lại tồn kho thực tế từ Database cho từng món trong giỏ hàng
            foreach (var item in cart)
            {
                var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product != null)
                {
                    ProductVariant? variant = null;
                    if (item.VariantId.HasValue && item.VariantId.Value > 0)
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId.Value);
                    }
                    if (variant == null)
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Size == item.Size && v.Color == item.Color)
                               ?? product.Variants.FirstOrDefault(v => v.Color == item.Color)
                               ?? product.Variants.FirstOrDefault(v => v.Size == item.Size)
                               ?? product.Variants.FirstOrDefault();
                    }

                    int availableStock = variant != null ? variant.StockQuantity : product.TotalStock;
                    item.AvailableStock = availableStock;

                    if (item.Quantity > availableStock)
                    {
                        item.Quantity = Math.Max(1, availableStock);
                        cartModified = true;
                    }
                }
            }

            if (cartModified)
            {
                SaveCart(cart);
                TempData["StockWarning"] = "Một số sản phẩm trong giỏ hàng đã được tự động điều chỉnh về số lượng tối đa còn trong kho!";
            }

            decimal subTotal = cart.Sum(c => c.TotalPrice);
            decimal shippingFee = (subTotal >= 300000 || subTotal == 0) ? 0 : 30000;
            decimal totalAmount = subTotal + shippingFee;

            ViewBag.SubTotal = subTotal;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.TotalAmount = totalAmount;

            return View(cart);
        }

        // POST: /Cart/ApplyCoupon
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string couponCode, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["CouponError"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToLocal(returnUrl);
            }

            var cart = GetCart();
            decimal subTotal = cart.Sum(c => c.TotalPrice);

            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int uid)) currentUserId = uid;
            }

            var (coupon, discount, error, isValid) = await EvaluateCouponAsync(couponCode, subTotal, currentUserId);
            if (!isValid || coupon == null)
            {
                TempData["CouponError"] = error ?? "Mã giảm giá không hợp lệ.";
                return RedirectToLocal(returnUrl);
            }

            HttpContext.Session.SetString(CouponSessionKey, coupon.Code);
            TempData["CouponSuccess"] = $"Áp dụng mã \"{coupon.Code}\" thành công! Bạn được giảm {discount:N0} đ.";
            return RedirectToLocal(returnUrl);
        }

        // POST: /Cart/RemoveCoupon
        [HttpPost]
        public IActionResult RemoveCoupon(string? returnUrl)
        {
            HttpContext.Session.Remove(CouponSessionKey);
            TempData["CouponSuccess"] = "Đã gỡ bỏ mã giảm giá khỏi đơn hàng.";
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Checkout");
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int? variantId, string? size, string? color, int quantity = 1, bool isAddon = false)
        {
            var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.";
                return RedirectToAction("Index", "Product");
            }

            // Lookup variant to get variant-specific image and stock
            ProductVariant? variant = null;
            if (variantId.HasValue && variantId.Value > 0)
            {
                variant = product.Variants.FirstOrDefault(v => v.Id == variantId.Value);
            }
            if (variant == null && !string.IsNullOrEmpty(color))
            {
                variant = product.Variants.FirstOrDefault(v => v.Color == color && (string.IsNullOrEmpty(size) || v.Size == size));
                if (variant == null)
                {
                    variant = product.Variants.FirstOrDefault(v => v.Color == color);
                }
            }
            if (variant == null && !string.IsNullOrEmpty(size))
            {
                variant = product.Variants.FirstOrDefault(v => v.Size == size);
            }
            if (variant == null)
            {
                variant = product.Variants.FirstOrDefault();
            }

            int availableStock = variant != null ? variant.StockQuantity : product.TotalStock;
            string variantDesc = variant != null ? $" (Size: {variant.Size}, Màu: {variant.Color})" : "";

            // Kiểm tra tồn kho
            if (availableStock <= 0)
            {
                TempData["StockWarning"] = $"Sản phẩm \"{product.Name}\"{variantDesc} hiện đã hết hàng trong kho. Mẹ vui lòng chọn phân loại khác nhé!";
                string? referer = Request.Headers["Referer"].ToString();
                return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Details", "Product", new { id = productId });
            }

            int requestedQty = Math.Max(1, quantity);
            if (requestedQty > availableStock)
            {
                TempData["StockWarning"] = $"Kho chỉ còn {availableStock} sản phẩm \"{product.Name}\"{variantDesc}. Bạn không thể đặt quá số lượng này!";
                string? referer = Request.Headers["Referer"].ToString();
                return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Details", "Product", new { id = productId });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size && c.Color == color && c.IsAddonDeal == isAddon);

            int currentInCart = existingItem?.Quantity ?? 0;
            if (currentInCart + requestedQty > availableStock)
            {
                int canAddMore = Math.Max(0, availableStock - currentInCart);
                TempData["StockWarning"] = $"Bạn đã có {currentInCart} sản phẩm trong giỏ. Trong kho chỉ còn {availableStock} sản phẩm, bạn chỉ có thể thêm tối đa {canAddMore} sản phẩm nữa!";
                return RedirectToAction("Index");
            }

            string? itemImage = (!string.IsNullOrEmpty(variant?.ImageUrl)) ? variant.ImageUrl : product.MainImageUrl;

            decimal finalPrice = (isAddon && product.AddonPrice.HasValue && product.AddonPrice.Value > 0) 
                ? product.AddonPrice.Value 
                : product.CurrentPrice;

            if (existingItem != null)
            {
                existingItem.Quantity += requestedQty;
                existingItem.AvailableStock = availableStock;
                if (!string.IsNullOrEmpty(variant?.ImageUrl))
                {
                    existingItem.ProductImage = variant.ImageUrl;
                }
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    VariantId = variant?.Id ?? variantId,
                    ProductName = product.Name,
                    ProductImage = itemImage,
                    Size = size,
                    Color = color,
                    Price = finalPrice,
                    OriginalPrice = isAddon ? product.Price : null,
                    IsAddonDeal = isAddon,
                    Quantity = requestedQty,
                    AvailableStock = availableStock
                });
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = isAddon
                ? $"Đã thêm Deal mua kèm \"{product.Name}\" với giá sốc {finalPrice:N0} đ vào giỏ hàng!"
                : $"Đã thêm \"{product.Name}\" vào giỏ hàng thành công!";

            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, string? size, string? color, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size && c.Color == color);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                    SaveCart(cart);
                    TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra tồn kho thực tế từ Database
                var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == productId);
                if (product != null)
                {
                    ProductVariant? variant = null;
                    if (item.VariantId.HasValue && item.VariantId.Value > 0)
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId.Value);
                    }
                    if (variant == null)
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Size == item.Size && v.Color == item.Color)
                               ?? product.Variants.FirstOrDefault(v => v.Color == item.Color)
                               ?? product.Variants.FirstOrDefault(v => v.Size == item.Size)
                               ?? product.Variants.FirstOrDefault();
                    }

                    int availableStock = variant != null ? variant.StockQuantity : product.TotalStock;
                    item.AvailableStock = availableStock;

                    if (quantity > availableStock)
                    {
                        item.Quantity = availableStock;
                        SaveCart(cart);
                        TempData["StockWarning"] = $"Sản phẩm \"{item.ProductName}\" ({item.Size} - {item.Color}) chỉ còn tối đa {availableStock} sản phẩm trong kho. Đã tự động điều chỉnh về số lượng tối đa!";
                        return RedirectToAction("Index");
                    }
                }

                item.Quantity = quantity;
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        public IActionResult RemoveItem(int productId, string? size, string? color)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size && c.Color == color);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/ClearCart
        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove(CouponSessionKey);
            TempData["SuccessMessage"] = "Đã làm trống giỏ hàng.";
            return RedirectToAction("Index");
        }

        // Helper xác định % chiết khấu hạng thành viên và quy tắc thời hạn (Kim Cương: 20% trọn đời; Vàng: 10%; Bạc: 8%; Đồng: 3% - 12 tháng)
        private (int discountPercent, string normalizedTier, bool isLifetime) GetTierDiscountInfo(User? user)
        {
            if (user == null)
            {
                return (0, "Khách vãng lai", false);
            }

            string rawTier = user.MemberTier ?? "Thành viên hạng Đồng";
            if (rawTier == "Mẹ Đồng") rawTier = "Thành viên hạng Đồng";
            else if (rawTier == "Mẹ Bạc") rawTier = "Thành viên hạng Bạc";
            else if (rawTier == "Mẹ Vàng") rawTier = "Thành viên hạng Vàng";
            else if (rawTier == "Mẹ Kim Cương") rawTier = "Thành viên hạng Kim Cương";

            // Riêng hạng Kim Cương áp dụng giảm 20% trọn đời (vĩnh viễn)
            if (rawTier.Contains("Kim Cương") || rawTier.Contains("KC"))
            {
                return (20, "Thành viên hạng Kim Cương", true);
            }

            // Các hạng khác có thời hạn 12 tháng
            bool isExpired = user.TierExpiryDate.HasValue && user.TierExpiryDate.Value < DateTime.Now;

            if (rawTier.Contains("Vàng"))
            {
                if (isExpired) return (3, "Thành viên hạng Đồng (Hết hạn Vàng)", false);
                return (10, "Thành viên hạng Vàng", false);
            }

            if (rawTier.Contains("Bạc"))
            {
                if (isExpired) return (3, "Thành viên hạng Đồng (Hết hạn Bạc)", false);
                return (8, "Thành viên hạng Bạc", false);
            }

            // Hạng Đồng: 3%
            return (3, "Thành viên hạng Đồng", false);
        }

        // GET: /Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng chọn sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Product");
            }

            // Kiểm tra tồn kho trước khi thanh toán
            bool hasStockError = false;
            var outOfStockItems = new List<CartItem>();
            foreach (var item in cart)
            {
                var p = await _context.Products.Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == item.ProductId);
                if (p == null || !p.IsActive)
                {
                    outOfStockItems.Add(item);
                    hasStockError = true;
                    continue;
                }

                var v = p.Variants.FirstOrDefault(x => x.Id == item.VariantId)
                     ?? p.Variants.FirstOrDefault(x => x.Size == item.Size && x.Color == item.Color)
                     ?? p.Variants.FirstOrDefault();
                int stock = v != null ? v.StockQuantity : p.TotalStock;
                item.AvailableStock = stock;
                if (stock <= 0)
                {
                    outOfStockItems.Add(item);
                    hasStockError = true;
                }
                else if (item.Quantity > stock)
                {
                    item.Quantity = stock;
                    hasStockError = true;
                }
            }
            if (hasStockError)
            {
                foreach (var rem in outOfStockItems)
                {
                    cart.Remove(rem);
                }
                SaveCart(cart);
                TempData["ErrorMessage"] = outOfStockItems.Any()
                    ? "Một số sản phẩm trong giỏ hàng đã hết hàng nên đã được cập nhật lại. Vui lòng kiểm tra giỏ hàng!"
                    : "Một số sản phẩm trong giỏ hàng đã thay đổi số lượng do giới hạn tồn kho. Vui lòng kiểm tra lại giỏ hàng!";
                return RedirectToAction("Index");
            }

            // Yêu cầu đăng nhập tài khoản trước khi tiến hành thanh toán
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                TempData["InfoMessage"] = "Vui lòng đăng nhập tài khoản để tiếp tục thanh toán đơn hàng cho bé mẹ nhé!";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            // Lấy thông tin user trước để tính toán chiết khấu hạng thành viên
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = null;
            if (int.TryParse(userIdStr, out int userId))
            {
                user = await _context.Users.FindAsync(userId);
            }
            else
            {
                var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            }

            var (tierDiscountPercent, userTier, isLifetime) = GetTierDiscountInfo(user);

            decimal subTotal = cart.Sum(c => c.TotalPrice);

            var appliedCode = HttpContext.Session.GetString(CouponSessionKey);
            Coupon? appliedCoupon = null;
            decimal discountAmount = 0;
            string? couponWarning = null;

            if (!string.IsNullOrEmpty(appliedCode))
            {
                var (evalCoupon, evalDiscount, evalMsg, isValid) = await EvaluateCouponAsync(appliedCode, subTotal, user?.Id);
                if (isValid && evalCoupon != null)
                {
                    appliedCoupon = evalCoupon;
                    discountAmount = evalDiscount;
                }
                else
                {
                    couponWarning = evalMsg;
                }
            }

            // Chiết khấu hạng thành viên Baby Club: Đồng 3%, Bạc 8%, Vàng 10%, Kim Cương 20% (Trọn đời)
            decimal memberDiscount = Math.Round(subTotal * (tierDiscountPercent / 100m), 0);
            decimal afterDiscount = Math.Max(0, subTotal - discountAmount - memberDiscount);
            decimal shippingFee = CalculateShippingFee(userTier, afterDiscount, subTotal);
            decimal totalAmount = Math.Max(0, afterDiscount + shippingFee);

            var order = new Order
            {
                SubTotal = subTotal,
                DiscountAmount = discountAmount,
                MemberDiscount = memberDiscount,
                CouponCode = appliedCoupon?.Code,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                PaymentMethod = "COD"
            };

            if (user != null)
            {
                // Lấy đúng dữ liệu thật từ tài khoản đang đăng nhập (họ tên, email, sđt, địa chỉ)
                order.CustomerName = user.FullName ?? "";
                order.CustomerEmail = user.Email ?? "";
                order.CustomerPhone = user.PhoneNumber ?? "";
                order.ShippingAddress = user.Address ?? "";
                order.UserId = user.Id;

                ViewBag.CurrentUser = user;
                ViewBag.UserRewardPoints = user.RewardPoints;
                ViewBag.UserMemberTier = userTier;
                ViewBag.TotalSpent = user.TotalSpent;
                ViewBag.TierDiscountPercent = tierDiscountPercent;
                ViewBag.IsLifetimeTier = isLifetime;
            }
            else
            {
                ViewBag.UserRewardPoints = 0;
                ViewBag.UserMemberTier = "Thành viên hạng Đồng";
                ViewBag.TotalSpent = 0;
                ViewBag.TierDiscountPercent = 3;
                ViewBag.IsLifetimeTier = false;
            }

            ViewBag.Cart = cart;
            ViewBag.AppliedCoupon = appliedCoupon;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.MemberDiscount = memberDiscount;
            ViewBag.CouponWarning = couponWarning;
            // Lấy danh sách voucher trong kho của tài khoản đang đăng nhập
            var userVouchers = new List<UserCoupon>();
            if (user != null)
            {
                var now = DateTime.Now;
                // Tự động đồng bộ các mã Săn Giờ Vàng (IsFlashHunt) đang kích hoạt vào kho của người dùng nếu chưa có
                var activeFlashCoupons = await _context.Coupons
                    .Where(c => c.IsFlashHunt && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate.Value > now))
                    .ToListAsync();

                if (activeFlashCoupons.Any())
                {
                    var existingCouponIds = await _context.UserCoupons
                        .Where(uc => uc.UserId == user.Id)
                        .Select(uc => uc.CouponId)
                        .ToListAsync();

                    var missingCoupons = activeFlashCoupons.Where(c => !existingCouponIds.Contains(c.Id)).ToList();
                    if (missingCoupons.Any())
                    {
                        foreach (var fc in missingCoupons)
                        {
                            _context.UserCoupons.Add(new UserCoupon
                            {
                                UserId = user.Id,
                                CouponId = fc.Id,
                                CollectedAt = now,
                                ExpiryDate = fc.ExpiryDate ?? now.AddDays(30),
                                IsUsed = false
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                userVouchers = await _context.UserCoupons
                    .Include(uc => uc.Coupon)
                    .Where(uc => uc.UserId == user.Id && !uc.IsUsed && uc.ExpiryDate >= now && uc.Coupon != null && uc.Coupon.IsActive)
                    .OrderBy(uc => uc.ExpiryDate)
                    .ToListAsync();
            }
            ViewBag.UserVouchers = userVouchers;

            ViewBag.AvailableCoupons = await _context.Coupons
                .Where(c => !c.IsForYou && !c.IsFlashHunt && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate > DateTime.Now))
                .OrderBy(c => c.MinOrderAmount)
                .ToListAsync();

            await PopulateBankViewBagAsync();

            var checkoutToken = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString(CheckoutTokenSessionKey, checkoutToken);
            ViewBag.CheckoutToken = checkoutToken;

            return View(order);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order, bool usePoints = false, int pointsToUse = 0, string? checkoutToken = null)
        {
            // Chống gửi lặp / nhấn đúp nút đặt hàng: nếu token đã hoàn tất trước đó thì chuyển thẳng tới trang xác nhận đơn
            if (!string.IsNullOrWhiteSpace(checkoutToken))
            {
                var lastCompleted = HttpContext.Session.GetString(LastCompletedOrderSessionKey);
                if (!string.IsNullOrEmpty(lastCompleted) && lastCompleted.StartsWith(checkoutToken + ":", StringComparison.Ordinal))
                {
                    if (int.TryParse(lastCompleted[(checkoutToken.Length + 1)..], out int existingOrderId) && existingOrderId > 0)
                    {
                        return RedirectToAction("OrderSuccess", new { id = existingOrderId });
                    }
                }
            }

            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng đã hết hạn hoặc trống.";
                return RedirectToAction("Index", "Product");
            }

            // Kiểm tra tồn kho lần cuối trước khi tạo đơn hàng
            foreach (var item in cart)
            {
                var p = await _context.Products.Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == item.ProductId);
                if (p == null || !p.IsActive)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm \"{item.ProductName}\" không còn khả dụng.";
                    return RedirectToAction("Index");
                }
                var v = p.Variants.FirstOrDefault(x => x.Id == item.VariantId)
                     ?? p.Variants.FirstOrDefault(x => x.Size == item.Size && x.Color == item.Color)
                     ?? p.Variants.FirstOrDefault();
                int stock = v != null ? v.StockQuantity : p.TotalStock;
                if (item.Quantity > stock)
                {
                    TempData["ErrorMessage"] = $"Rất tiếc, sản phẩm \"{item.ProductName}\" ({item.Size} - {item.Color}) chỉ còn {stock} trong kho (bạn đang đặt {item.Quantity}). Vui lòng cập nhật lại giỏ hàng!";
                    return RedirectToAction("Index");
                }
            }

            // Yêu cầu đăng nhập tài khoản trước khi hoàn tất đặt hàng
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                TempData["InfoMessage"] = "Vui lòng đăng nhập tài khoản để hoàn tất đơn hàng.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            User? user = null;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int uid))
                {
                    user = await _context.Users.FindAsync(uid);
                }
                else
                {
                    var username = User.FindFirst("Username")?.Value ?? User.Identity?.Name;
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                }
            }

            var (tierDiscountPercent, userTier, isLifetime) = GetTierDiscountInfo(user);

            decimal subTotal = cart.Sum(c => c.TotalPrice);

            // Chiết khấu hạng thành viên Baby Club: Đồng 3%, Bạc 8%, Vàng 10%, Kim Cương 20% (Trọn đời)
            decimal memberDiscount = Math.Round(subTotal * (tierDiscountPercent / 100m), 0);

            // Bắt buộc đối chiếu và nhập đầy đủ thông tin giao hàng & địa chỉ
            bool hasShippingError = false;
            if (string.IsNullOrWhiteSpace(order.ShippingAddress))
            {
                ModelState.AddModelError("ShippingAddress", "Vui lòng nhập địa chỉ nhận hàng chi tiết.");
                TempData["ErrorMessage"] = "Vui lòng nhập địa chỉ nhận hàng chi tiết để shipper có thể giao hàng cho bạn.";
                hasShippingError = true;
            }

            if (string.IsNullOrWhiteSpace(order.CustomerName))
            {
                ModelState.AddModelError("CustomerName", "Vui lòng nhập họ và tên người nhận.");
                TempData["ErrorMessage"] = "Vui lòng nhập họ và tên người nhận.";
                hasShippingError = true;
            }

            if (string.IsNullOrWhiteSpace(order.CustomerPhone))
            {
                ModelState.AddModelError("CustomerPhone", "Vui lòng nhập số điện thoại liên hệ.");
                TempData["ErrorMessage"] = "Vui lòng nhập số điện thoại liên hệ.";
                hasShippingError = true;
            }

            if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                ModelState.AddModelError("CustomerEmail", "Vui lòng nhập email nhận xác nhận đơn hàng.");
                TempData["ErrorMessage"] = "Vui lòng nhập email nhận xác nhận đơn hàng.";
                hasShippingError = true;
            }

            if (hasShippingError)
            {
                var errCode = HttpContext.Session.GetString(CouponSessionKey) ?? order.CouponCode;
                Coupon? errCoupon = null;
                decimal errDiscount = 0;
                if (!string.IsNullOrEmpty(errCode))
                {
                    var (cEval, dEval, _, validEval) = await EvaluateCouponAsync(errCode, subTotal, user?.Id);
                    if (validEval && cEval != null)
                    {
                        errCoupon = cEval;
                        errDiscount = dEval;
                    }
                }

                decimal errAfterDiscount = Math.Max(0, subTotal - errDiscount - memberDiscount);
                order.SubTotal = subTotal;
                order.DiscountAmount = errDiscount;
                order.MemberDiscount = memberDiscount;
                order.CouponCode = errCoupon?.Code;
                order.ShippingFee = CalculateShippingFee(userTier, errAfterDiscount, subTotal);
                order.TotalAmount = Math.Max(0, errAfterDiscount + order.ShippingFee);

                await PopulateCheckoutRedisplayAsync(cart, user, userTier, tierDiscountPercent, memberDiscount, errCoupon, errDiscount);
                return View(order);
            }

            // Kiểm tra mã giảm giá đang áp dụng (ưu tiên session hoặc từ form)
            var appliedCode = HttpContext.Session.GetString(CouponSessionKey);
            if (string.IsNullOrEmpty(appliedCode) && !string.IsNullOrEmpty(order.CouponCode))
            {
                appliedCode = order.CouponCode;
            }

            Coupon? appliedCoupon = null;
            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(appliedCode))
            {
                var (evalCoupon, evalDiscount, evalMsg, isValid) = await EvaluateCouponAsync(appliedCode, subTotal, user?.Id);
                if (isValid && evalCoupon != null)
                {
                    appliedCoupon = evalCoupon;
                    discountAmount = evalDiscount;
                }
                else
                {
                    TempData["CouponError"] = evalMsg ?? "Mã giảm giá không hợp lệ hoặc không thể áp dụng.";
                    ModelState.AddModelError("CouponCode", evalMsg ?? "Mã giảm giá không hợp lệ.");

                    decimal invalidAfterDiscount = Math.Max(0, subTotal - memberDiscount);
                    order.SubTotal = subTotal;
                    order.DiscountAmount = 0;
                    order.MemberDiscount = memberDiscount;
                    order.CouponCode = null;
                    order.ShippingFee = CalculateShippingFee(userTier, invalidAfterDiscount, subTotal);
                    order.TotalAmount = Math.Max(0, invalidAfterDiscount + order.ShippingFee);

                    await PopulateCheckoutRedisplayAsync(cart, user, userTier, tierDiscountPercent, memberDiscount, null, 0);
                    return View(order);
                }
            }
            decimal afterDiscount = Math.Max(0, subTotal - discountAmount - memberDiscount);
            decimal shippingFee = CalculateShippingFee(userTier, afterDiscount, subTotal);

            // Xử lý điểm thưởng Baby Club
            int actualPointsUsed = 0;
            decimal pointDiscount = 0;
            if (usePoints && user != null && user.RewardPoints > 0)
            {
                int maxPointsPossible = (int)(afterDiscount / 100);
                actualPointsUsed = Math.Min(user.RewardPoints, Math.Min(pointsToUse > 0 ? pointsToUse : user.RewardPoints, maxPointsPossible));
                if (actualPointsUsed > 0)
                {
                    pointDiscount = actualPointsUsed * 100;
                    user.RewardPoints -= actualPointsUsed;
                }
            }

            decimal totalAmount = Math.Max(0, afterDiscount - pointDiscount + shippingFee);

            order.SubTotal = subTotal;
            order.CouponCode = appliedCoupon?.Code;
            order.DiscountAmount = discountAmount;
            order.MemberDiscount = memberDiscount;
            order.PointsUsed = actualPointsUsed;
            order.PointDiscount = pointDiscount;
            order.ShippingFee = shippingFee;
            order.TotalAmount = totalAmount;
            order.OrderCode = "BS-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
            order.CreatedAt = DateTime.Now;
            order.OrderStatus = "Pending";
            order.IsPaid = false;

            // Tăng số lượt sử dụng của mã giảm giá
            if (appliedCoupon != null)
            {
                appliedCoupon.UsedCount += 1;
                _context.Coupons.Update(appliedCoupon);
            }

            if (user != null)
            {
                order.UserId = user.Id;

                // Tự động lưu và cập nhật các thông tin giao hàng làm mặc định cho tài khoản
                bool userUpdated = false;

                if (!string.IsNullOrWhiteSpace(order.ShippingAddress))
                {
                    user.Address = order.ShippingAddress.Trim();
                    userUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
                {
                    user.PhoneNumber = order.CustomerPhone.Trim();
                    userUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(order.CustomerName))
                {
                    user.FullName = order.CustomerName.Trim();
                    userUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(order.CustomerEmail) && string.IsNullOrWhiteSpace(user.Email))
                {
                    user.Email = order.CustomerEmail.Trim();
                    userUpdated = true;
                }

                if (userUpdated)
                {
                    _context.Users.Update(user);
                }
            }

            // Thêm chi tiết đơn hàng
            foreach (var item in cart)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.VariantId,
                    ProductName = item.ProductName,
                    ProductImage = item.ProductImage,
                    Size = item.Size,
                    Color = item.Color,
                    UnitPrice = item.Price,
                    Quantity = item.Quantity
                });

                // Trừ tồn kho biến thể thực tế trong DB
                if (item.VariantId.HasValue && item.VariantId.Value > 0)
                {
                    var v = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (v != null)
                    {
                        v.StockQuantity = Math.Max(0, v.StockQuantity - item.Quantity);
                        _context.ProductVariants.Update(v);
                    }
                }
                else
                {
                    var v = await _context.ProductVariants.FirstOrDefaultAsync(pv => pv.ProductId == item.ProductId && pv.Size == item.Size && pv.Color == item.Color)
                         ?? await _context.ProductVariants.FirstOrDefaultAsync(pv => pv.ProductId == item.ProductId);
                    if (v != null)
                    {
                        v.StockQuantity = Math.Max(0, v.StockQuantity - item.Quantity);
                        _context.ProductVariants.Update(v);
                    }
                }

                // Cập nhật số lượng đã bán nếu là sản phẩm Flash Sale
                var pFlash = await _context.Products.FindAsync(item.ProductId);
                if (pFlash != null && pFlash.IsFlashSale)
                {
                    pFlash.FlashSaleSold += item.Quantity;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Cập nhật trạng thái voucher trong Kho (UserCoupons)
            if (appliedCoupon != null && user != null)
            {
                var userCoupon = await _context.UserCoupons
                    .FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.CouponId == appliedCoupon.Id);
                if (userCoupon != null)
                {
                    userCoupon.IsUsed = true;
                    userCoupon.UsedAt = DateTime.Now;
                    userCoupon.OrderId = order.Id;
                    _context.UserCoupons.Update(userCoupon);
                }
                else
                {
                    // Người dùng nhập trực tiếp mã mà chưa lưu kho -> tự động tạo bản ghi và đánh dấu đã sử dụng
                    _context.UserCoupons.Add(new UserCoupon
                    {
                        UserId = user.Id,
                        CouponId = appliedCoupon.Id,
                        CollectedAt = DateTime.Now,
                        ExpiryDate = appliedCoupon.ExpiryDate ?? DateTime.Now.AddDays(30),
                        IsUsed = true,
                        UsedAt = DateTime.Now,
                        OrderId = order.Id
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Ghi nhận lịch sử dùng điểm thưởng Baby Club
            if (actualPointsUsed > 0 && user != null)
            {
                _context.PointTransactions.Add(new PointTransaction
                {
                    UserId = user.Id,
                    OrderId = order.Id,
                    PointsChanged = -actualPointsUsed,
                    Type = "Spend",
                    Description = $"Dùng {actualPointsUsed} điểm thưởng Baby Club thanh toán đơn hàng #{order.OrderCode}",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // Xóa giỏ hàng và mã giảm giá sau khi đặt thành công
            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove(CouponSessionKey);

            var activeCheckoutToken = !string.IsNullOrWhiteSpace(checkoutToken)
                ? checkoutToken
                : HttpContext.Session.GetString(CheckoutTokenSessionKey);
            if (!string.IsNullOrWhiteSpace(activeCheckoutToken))
            {
                HttpContext.Session.SetString(LastCompletedOrderSessionKey, $"{activeCheckoutToken}:{order.Id}");
                HttpContext.Session.Remove(CheckoutTokenSessionKey);
            }

            // Tạo bản sao HTML email xác nhận ngay lập tức và gửi SMTP ở nền để chuyển thẳng tới trang OrderSuccess
            try
            {
                if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
                {
                    await _emailService.PrepareAndDispatchOrderConfirmationAsync(order, order.OrderDetails);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý gửi email xác nhận cho đơn hàng #{OrderCode} tới {Email}", order.OrderCode, order.CustomerEmail);
            }

            return RedirectToAction("OrderSuccess", new { id = order.Id });
        }

        // GET: /Cart/OrderSuccess/5
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            await PopulateBankViewBagAsync();

            return View(order);
        }

        private static decimal CalculateShippingFee(string userTier, decimal afterDiscount, decimal subTotal)
        {
            if (userTier.Contains("Kim Cương")) return 0;
            if (userTier.Contains("Vàng")) return (afterDiscount >= 100000 || subTotal == 0) ? 0 : 30000;
            if (userTier.Contains("Bạc")) return (afterDiscount >= 200000 || subTotal == 0) ? 0 : 30000;
            return (afterDiscount >= 300000 || subTotal == 0) ? 0 : 30000;
        }

        private async Task PopulateCheckoutRedisplayAsync(
            List<CartItem> cart,
            User? user,
            string userTier,
            int tierDiscountPercent,
            decimal memberDiscount,
            Coupon? appliedCoupon,
            decimal discountAmount)
        {
            ViewBag.Cart = cart;
            ViewBag.CurrentUser = user;
            ViewBag.UserRewardPoints = user?.RewardPoints ?? 0;
            ViewBag.UserMemberTier = userTier;
            ViewBag.TierDiscountPercent = tierDiscountPercent;
            ViewBag.MemberDiscount = memberDiscount;
            ViewBag.AppliedCoupon = appliedCoupon;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.AvailableCoupons = await _context.Coupons
                .Where(c => !c.IsForYou && !c.IsFlashHunt && c.IsActive && (!c.ExpiryDate.HasValue || c.ExpiryDate > DateTime.Now))
                .OrderBy(c => c.MinOrderAmount)
                .ToListAsync();
            ViewBag.UserVouchers = user != null ? await _context.UserCoupons
                .Include(uc => uc.Coupon)
                .Where(uc => uc.UserId == user.Id && !uc.IsUsed && uc.ExpiryDate >= DateTime.Now && uc.Coupon != null && uc.Coupon.IsActive)
                .OrderBy(uc => uc.ExpiryDate)
                .ToListAsync() : new List<UserCoupon>();

            await PopulateBankViewBagAsync();

            var token = HttpContext.Session.GetString(CheckoutTokenSessionKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                token = Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString(CheckoutTokenSessionKey, token);
            }
            ViewBag.CheckoutToken = token;
        }

        private async Task PopulateBankViewBagAsync()
        {
            List<BankAccount> activeBanks;
            try
            {
                activeBanks = await _context.BankAccounts
                    .AsNoTracking()
                    .Where(b => b.IsActive)
                    .OrderByDescending(b => b.IsDefault)
                    .ThenBy(b => b.Id)
                    .ToListAsync();
            }
            catch
            {
                activeBanks = new List<BankAccount>();
            }

            if (!activeBanks.Any())
            {
                activeBanks.Add(WebsiteBabyShop.Helpers.VietQrBankHelper.CreateFallbackDefault());
            }

            ViewBag.ActiveBanks = activeBanks;
            ViewBag.DefaultBank = activeBanks.First();
        }
    }
}
