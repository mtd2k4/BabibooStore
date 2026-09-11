using Microsoft.AspNetCore.Mvc;

namespace WebsiteBabyShop.Controllers
{
    public class PolicyController : Controller
    {
        // 1. Chính sách đổi trả trong 7 ngày
        [Route("chinh-sach-doi-tra")]
        public IActionResult Return()
        {
            ViewData["Title"] = "Chính Sách Đổi Trả Trong 7 Ngày Tận Nhà - BabyShop Studio Kids";
            return View();
        }

        // 2. Chính sách giao hàng toàn quốc
        [Route("chinh-sach-giao-hang")]
        public IActionResult Shipping()
        {
            ViewData["Title"] = "Chính Sách Giao Hàng Toàn Quốc - BabyShop Studio Kids";
            return View();
        }

        // 3. Hướng dẫn bảo quản trang phục cho bé
        [Route("huong-dan-bao-quan")]
        public IActionResult Care()
        {
            ViewData["Title"] = "Hướng Dẫn Giặt & Bảo Quản Đồ Bé Yêu - BabyShop Studio Kids";
            return View();
        }

        // 4. Chính sách bảo mật thông tin
        [Route("chinh-sach-bao-mat")]
        public IActionResult Privacy()
        {
            ViewData["Title"] = "Chính Sách Bảo Mật Thông Tin Khách Hàng - BabyShop Studio Kids";
            return View();
        }
    }
}
