using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class OrderTrackingController : Controller
    {
        private readonly BabyShopDbContext _context;

        public OrderTrackingController(BabyShopDbContext context)
        {
            _context = context;
        }

        // GET: /tra-cuu-don-hang hoặc /OrderTracking
        [Route("tra-cuu-don-hang")]
        [Route("OrderTracking")]
        public async Task<IActionResult> Index(string? orderCode, string? phone)
        {
            ViewData["Title"] = "Tra Cứu Đơn Hàng - BabyShop Studio Kids";

            // Nếu người dùng đã đăng nhập và chưa nhập SĐT, gợi ý sẵn SĐT
            if (string.IsNullOrWhiteSpace(phone) && (User.Identity?.IsAuthenticated ?? false))
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                {
                    phone = user.PhoneNumber;
                }
            }

            ViewBag.OrderCode = orderCode?.Trim() ?? "";
            ViewBag.Phone = phone?.Trim() ?? "";

            // Nếu chưa nhập đủ thông tin tìm kiếm
            if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(phone))
            {
                // Nếu khách chỉ vừa mở trang, không báo lỗi
                return View();
            }

            var cleanCode = orderCode.Trim();
            var cleanPhone = phone.Trim().Replace(" ", "").Replace(".", "");

            // Tìm đơn hàng theo OrderCode hoặc ID
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.OrderCode.ToLower() == cleanCode.ToLower() || o.Id.ToString() == cleanCode);

            if (order == null)
            {
                ViewBag.ErrorMessage = $"Không tìm thấy đơn hàng với mã \"{cleanCode}\". Mẹ vui lòng kiểm tra lại mã đơn trong tin nhắn SMS hoặc xác nhận đơn hàng nhé.";
                return View();
            }

            // Kiểm tra số điện thoại bảo mật
            var dbPhone = (order.CustomerPhone ?? "").Replace(" ", "").Replace(".", "");
            if (!dbPhone.EndsWith(cleanPhone) && !cleanPhone.EndsWith(dbPhone))
            {
                ViewBag.ErrorMessage = "Số điện thoại không khớp với số điện thoại nhận hàng của đơn này. Vì lý do bảo mật thông tin bé và gia đình, vui lòng nhập chính xác số điện thoại đặt hàng.";
                return View();
            }

            return View(order);
        }
    }
}
