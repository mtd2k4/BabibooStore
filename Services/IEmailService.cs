using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email HTML cơ bản đến một địa chỉ nhận
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);

        /// <summary>
        /// Gửi email thông báo đặt hàng thành công với template đầy đủ chi tiết đơn hàng
        /// </summary>
        Task<bool> SendOrderConfirmationEmailAsync(Order order, IEnumerable<OrderDetail> details);

        /// <summary>
        /// Tạo HTML xác nhận đơn hàng, lưu bản sao cục bộ (nếu bật) và gửi SMTP ở nền để không làm treo trang Thanh toán
        /// </summary>
        Task PrepareAndDispatchOrderConfirmationAsync(Order order, IEnumerable<OrderDetail> details);
    }
}
