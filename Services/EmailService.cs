using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly BabyShopDbContext _context;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            IWebHostEnvironment env,
            BabyShopDbContext context)
        {
            _settings = settings.Value;
            _logger = logger;
            _env = env;
            _context = context;
        }

        public Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return SendSmtpMailAsync(_settings, _logger, toEmail, subject, htmlBody);
        }

        private static async Task<bool> SendSmtpMailAsync(
            EmailSettings settings,
            ILogger logger,
            string toEmail,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                logger.LogWarning("[EmailService] Không thể gửi email do địa chỉ người nhận trống.");
                return false;
            }

            // Kiểm tra xem đã cấu hình tài khoản SMTP thực tế chưa
            bool hasSmtpCredentials = !string.IsNullOrWhiteSpace(settings.Username) && !string.IsNullOrWhiteSpace(settings.Password);

            if (!hasSmtpCredentials)
            {
                logger.LogInformation("[EmailService] SMTP chưa được cấu hình tài khoản (Username/Password trống). Hệ thống ghi nhận nội dung email cho {ToEmail}.", toEmail);
                return true;
            }

            try
            {
                var username = settings.Username.Trim();
                var cleanPassword = settings.Password.Trim().Replace(" ", "");
                var senderAddress = !string.IsNullOrWhiteSpace(settings.SenderEmail) && settings.SenderEmail != "babyshop.cskh@gmail.com"
                    ? settings.SenderEmail.Trim()
                    : username;
                var senderName = string.IsNullOrWhiteSpace(settings.SenderName) ? "BabyShop Studio Kids" : settings.SenderName.Trim();

                using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(username, cleanPassword),
                    Timeout = 15000 // 15 giây
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(senderAddress, senderName, Encoding.UTF8),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mail.To.Add(toEmail.Trim());

                await client.SendMailAsync(mail);
                logger.LogInformation("[EmailService] Đã gửi email thành công tới {ToEmail} qua SMTP server {Server}.", toEmail, settings.SmtpServer);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[EmailService] Gặp lỗi khi gửi email qua SMTP tới {ToEmail}. Ngoại lệ: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(Order order, IEnumerable<OrderDetail> details)
        {
            var prepared = await PrepareOrderEmailAsync(order, details);
            if (prepared == null) return false;

            return await SendSmtpMailAsync(_settings, _logger, prepared.Value.toEmail, prepared.Value.subject, prepared.Value.htmlBody);
        }

        public async Task PrepareAndDispatchOrderConfirmationAsync(Order order, IEnumerable<OrderDetail> details)
        {
            var prepared = await PrepareOrderEmailAsync(order, details);
            if (prepared == null) return;

            var settingsSnapshot = new EmailSettings
            {
                SmtpServer = _settings.SmtpServer,
                SmtpPort = _settings.SmtpPort,
                EnableSsl = _settings.EnableSsl,
                SenderName = _settings.SenderName,
                SenderEmail = _settings.SenderEmail,
                Username = _settings.Username,
                Password = _settings.Password,
                SaveToDisk = _settings.SaveToDisk
            };
            var logger = _logger;
            var (toEmail, subject, htmlBody) = prepared.Value;

            _ = Task.Run(() => SendSmtpMailAsync(settingsSnapshot, logger, toEmail, subject, htmlBody));
        }

        private async Task<(string toEmail, string subject, string htmlBody)?> PrepareOrderEmailAsync(Order order, IEnumerable<OrderDetail> details)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                _logger.LogWarning("[EmailService] Bỏ qua gửi email xác nhận vì đơn hàng hoặc email khách hàng không hợp lệ.");
                return null;
            }

            var subject = $"[BabyShop Studio Kids] Xác nhận đơn hàng thành công #{order.OrderCode}";
            var htmlBody = GenerateOrderConfirmationHtml(order, details);

            // Lưu bản sao HTML email vào thư mục wwwroot/emails/ ngay trước khi chuyển sang OrderSuccess
            if (_settings.SaveToDisk)
            {
                try
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var emailDir = Path.Combine(webRoot, "emails");
                    if (!Directory.Exists(emailDir))
                    {
                        Directory.CreateDirectory(emailDir);
                    }

                    var filePath = Path.Combine(emailDir, $"order-{order.OrderCode}.html");
                    await File.WriteAllTextAsync(filePath, htmlBody, Encoding.UTF8);
                    _logger.LogInformation("[EmailService] Đã lưu bản sao email xác nhận đơn hàng tại: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[EmailService] Không thể lưu bản sao file email HTML: {Message}", ex.Message);
                }
            }

            return (order.CustomerEmail.Trim(), subject, htmlBody);
        }

        private string GenerateOrderConfirmationHtml(Order order, IEnumerable<OrderDetail> details)
        {
            var paymentText = order.PaymentMethod == "COD" ? "Thanh toán khi nhận hàng (COD)" : "Chuyển khoản ngân hàng VietQR";
            var itemsList = details?.ToList() ?? new List<OrderDetail>();

            var sbItems = new StringBuilder();
            foreach (var item in itemsList)
            {
                var variantBadge = string.Empty;
                if (!string.IsNullOrWhiteSpace(item.Size) || !string.IsNullOrWhiteSpace(item.Color))
                {
                    variantBadge = $"<div style=\"color: #6c757d; font-size: 13px; margin-top: 3px;\">Phân loại: <b>{item.Size}</b> / <b>{item.Color}</b></div>";
                }

                sbItems.Append($@"
                    <tr style=""border-bottom: 1px solid #f1f1f1;"">
                        <td style=""padding: 12px 8px; font-size: 14px; color: #2d3748;"">
                            <div style=""font-weight: 600;"">{item.ProductName}</div>
                            {variantBadge}
                        </td>
                        <td style=""padding: 12px 8px; font-size: 14px; text-align: center; color: #4a5568;"">
                            x{item.Quantity}
                        </td>
                        <td style=""padding: 12px 8px; font-size: 14px; text-align: right; color: #4a5568;"">
                            {item.UnitPrice:N0} đ
                        </td>
                        <td style=""padding: 12px 8px; font-size: 14px; text-align: right; font-weight: 600; color: #e53e3e;"">
                            {item.TotalPrice:N0} đ
                        </td>
                    </tr>");
            }

            var discountRow = string.Empty;
            if (order.DiscountAmount > 0)
            {
                discountRow = $@"
                    <tr>
                        <td colspan=""3"" style=""padding: 6px 8px; text-align: right; color: #28a745; font-size: 13px;"">
                            Voucher giảm giá ({order.CouponCode}):
                        </td>
                        <td style=""padding: 6px 8px; text-align: right; color: #e53e3e; font-weight: 600; font-size: 13px;"">
                            -{order.DiscountAmount:N0} đ
                        </td>
                    </tr>";
            }

            var memberDiscountRow = string.Empty;
            if (order.MemberDiscount > 0)
            {
                memberDiscountRow = $@"
                    <tr>
                        <td colspan=""3"" style=""padding: 6px 8px; text-align: right; color: #007bff; font-size: 13px;"">
                            Chiết khấu thành viên Baby Club:
                        </td>
                        <td style=""padding: 6px 8px; text-align: right; color: #e53e3e; font-weight: 600; font-size: 13px;"">
                            -{order.MemberDiscount:N0} đ
                        </td>
                    </tr>";
            }

            var pointDiscountRow = string.Empty;
            if (order.PointDiscount > 0)
            {
                pointDiscountRow = $@"
                    <tr>
                        <td colspan=""3"" style=""padding: 6px 8px; text-align: right; color: #d39e00; font-size: 13px;"">
                            Điểm thưởng Baby Club ({order.PointsUsed} pts):
                        </td>
                        <td style=""padding: 6px 8px; text-align: right; color: #e53e3e; font-weight: 600; font-size: 13px;"">
                            -{order.PointDiscount:N0} đ
                        </td>
                    </tr>";
            }

            var shippingFeeText = order.ShippingFee == 0 ? "<span style=\"color: #28a745; font-weight: 600;\">Miễn phí giao hàng</span>" : $"{order.ShippingFee:N0} đ";

            // Khung VietQR nếu là Chuyển khoản (đọc từ cấu hình Ngân hàng trong Admin)
            var qrSection = string.Empty;
            if (order.PaymentMethod == "BankTransfer")
            {
                BankAccount? bankConfig = null;
                try
                {
                    bankConfig = _context.BankAccounts
                        .AsNoTracking()
                        .Where(b => b.IsActive)
                        .OrderByDescending(b => b.IsDefault)
                        .ThenBy(b => b.Id)
                        .FirstOrDefault();
                }
                catch { }

                bankConfig ??= WebsiteBabyShop.Helpers.VietQrBankHelper.CreateFallbackDefault();

                var bankName = bankConfig.BankName;
                var accountNo = bankConfig.AccountNumber;
                var accountName = bankConfig.AccountHolder;
                var transferAmount = (long)order.TotalAmount;
                var transferMemo = order.OrderCode;
                var qrUrl = bankConfig.BuildVietQrUrl(order.TotalAmount, transferMemo);

                qrSection = $@"
                <div style=""background: #fdf8f6; border: 1px solid #fbd38d; border-radius: 12px; padding: 18px; margin: 24px 0;"">
                    <h3 style=""color: #c05621; font-size: 16px; margin-top: 0; margin-bottom: 12px; display: flex; align-items: center;"">
                        ⚡ Hướng Dẫn Thanh Toán Chuyển Khoản Tự Động (VietQR)
                    </h3>
                    <p style=""font-size: 13px; color: #744210; margin-bottom: 15px;"">
                        Mẹ có thể mở ứng dụng Ngân hàng bất kỳ hoặc Ví điện tử để quét mã QR bên dưới để thanh toán nhanh 24/7:
                    </p>
                    <table style=""width: 100%; border-collapse: collapse;"">
                        <tr>
                            <td style=""width: 160px; text-align: center; vertical-align: middle; padding-right: 15px;"">
                                <img src=""{qrUrl}"" alt=""VietQR {order.OrderCode}"" style=""width: 150px; height: auto; border-radius: 8px; border: 1px solid #e2e8f0; background: #fff; padding: 4px;"" />
                            </td>
                            <td style=""vertical-align: top;"">
                                <table style=""width: 100%; font-size: 13px; color: #2d3748; line-height: 1.8;"">
                                    <tr>
                                        <td style=""color: #718096; width: 130px;"">Ngân hàng:</td>
                                        <td><b>{bankName}</b></td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #718096;"">Số tài khoản:</td>
                                        <td style=""color: #3182ce; font-family: monospace; font-size: 15px; font-weight: bold;"">{accountNo}</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #718096;"">Chủ tài khoản:</td>
                                        <td><b>{accountName}</b></td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #718096;"">Số tiền:</td>
                                        <td style=""color: #e53e3e; font-size: 15px; font-weight: bold;"">{transferAmount:N0} đ</td>
                                    </tr>
                                    <tr>
                                        <td style=""color: #718096;"">Nội dung CK:</td>
                                        <td style=""color: #dd6b20; font-family: monospace; font-size: 15px; font-weight: bold; background: #fffaf0; padding: 2px 6px; border-radius: 4px;"">{transferMemo}</td>
                                    </tr>
                                </table>
                                <div style=""font-size: 12px; color: #c53030; margin-top: 8px;"">
                                    * Vui lòng giữ nguyên nội dung chuyển khoản <b>{transferMemo}</b> để hệ thống xác nhận thanh toán tự động ngay lập tức!
                                </div>
                            </td>
                        </tr>
                    </table>
                </div>";
            }

            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác nhận đơn hàng #{order.OrderCode}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f7fafc; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; -webkit-font-smoothing: antialiased;"">
    <div style=""max-width: 650px; margin: 20px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); border: 1px solid #edf2f7;"">
        
        <!-- Header -->
        <div style=""background: linear-gradient(135deg, #ff758c 0%, #ff7eb3 100%); padding: 30px 24px; text-align: center; color: #ffffff;"">
            <h1 style=""margin: 0; font-size: 26px; font-weight: 800; letter-spacing: 0.5px; text-transform: uppercase;"">BabyShop Studio Kids</h1>
            <p style=""margin: 6px 0 0 0; font-size: 14px; opacity: 0.95;"">Thời trang & Đồ dùng cao cấp cho Mẹ và Bé yêu</p>
        </div>

        <!-- Success Banner -->
        <div style=""padding: 24px 28px 10px 28px; text-align: center;"">
            <div style=""display: inline-block; background-color: #e6fffa; border: 1px solid #b2f5ea; color: #234e52; font-size: 14px; font-weight: 600; padding: 6px 18px; border-radius: 50px; margin-bottom: 12px;"">
                ✓ ĐẶT HÀNG THÀNH CÔNG
            </div>
            <h2 style=""color: #1a202c; font-size: 22px; margin: 0 0 8px 0; font-weight: 700;"">Cảm ơn bạn đã đặt hàng!</h2>
            <p style=""color: #4a5568; font-size: 14px; line-height: 1.6; margin: 0;"">
                Chào <b>{order.CustomerName}</b>, đơn hàng của bạn đã được chuyển tới trung tâm xử lý BabyShop. Đội ngũ chúng tôi sẽ sớm đóng gói và bàn giao cho đơn vị vận chuyển.
            </p>
        </div>

        <!-- Order & Delivery Info Box -->
        <div style=""padding: 16px 28px;"">
            <div style=""background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 18px;"">
                <table style=""width: 100%; border-collapse: collapse; font-size: 14px; line-height: 1.8;"">
                    <tr>
                        <td style=""color: #718096; width: 140px;"">Mã đơn hàng:</td>
                        <td><b style=""color: #e53e3e; font-size: 15px;"">#{order.OrderCode}</b></td>
                    </tr>
                    <tr>
                        <td style=""color: #718096;"">Thời gian đặt:</td>
                        <td style=""color: #2d3748;"">{order.CreatedAt:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td style=""color: #718096;"">Người nhận:</td>
                        <td style=""color: #2d3748;""><b>{order.CustomerName}</b> ({order.CustomerPhone})</td>
                    </tr>
                    <tr>
                        <td style=""color: #718096;"">Email nhận tin:</td>
                        <td style=""color: #2d3748;"">{order.CustomerEmail}</td>
                    </tr>
                    <tr>
                        <td style=""color: #718096;"">Địa chỉ nhận hàng:</td>
                        <td style=""color: #2d3748;"">{order.ShippingAddress}</td>
                    </tr>
                    <tr>
                        <td style=""color: #718096;"">Thanh toán:</td>
                        <td style=""color: #3182ce; font-weight: 600;"">{paymentText}</td>
                    </tr>
                    {(string.IsNullOrWhiteSpace(order.Note) ? "" : $@"<tr><td style=""color: #718096;"">Ghi chú:</td><td style=""color: #718096; font-style: italic;"">{order.Note}</td></tr>")}
                </table>
            </div>
        </div>

        <!-- Items Table -->
        <div style=""padding: 10px 28px;"">
            <h3 style=""font-size: 16px; color: #2d3748; margin-top: 10px; margin-bottom: 12px; font-weight: 700;"">Chi Tiết Sản Phẩm</h3>
            <table style=""width: 100%; border-collapse: collapse;"">
                <thead>
                    <tr style=""background: #edf2f7; color: #4a5568; font-size: 13px; text-transform: uppercase;"">
                        <th style=""padding: 10px 8px; text-align: left; border-radius: 6px 0 0 6px;"">Sản phẩm</th>
                        <th style=""padding: 10px 8px; text-align: center;"">SL</th>
                        <th style=""padding: 10px 8px; text-align: right;"">Đơn giá</th>
                        <th style=""padding: 10px 8px; text-align: right; border-radius: 0 6px 6px 0;"">Thành tiền</th>
                    </tr>
                </thead>
                <tbody>
                    {sbItems}
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan=""3"" style=""padding: 12px 8px 4px 8px; text-align: right; color: #718096; font-size: 13px;"">Tiền hàng:</td>
                        <td style=""padding: 12px 8px 4px 8px; text-align: right; color: #2d3748; font-weight: 600; font-size: 14px;"">{order.SubTotal:N0} đ</td>
                    </tr>
                    {discountRow}
                    {memberDiscountRow}
                    {pointDiscountRow}
                    <tr>
                        <td colspan=""3"" style=""padding: 4px 8px; text-align: right; color: #718096; font-size: 13px;"">Phí giao hàng:</td>
                        <td style=""padding: 4px 8px; text-align: right; font-size: 13px;"">{shippingFeeText}</td>
                    </tr>
                    <tr style=""border-top: 2px solid #e2e8f0;"">
                        <td colspan=""3"" style=""padding: 14px 8px; text-align: right; font-size: 16px; font-weight: 700; color: #1a202c;"">TỔNG THANH TOÁN:</td>
                        <td style=""padding: 14px 8px; text-align: right; font-size: 18px; font-weight: 800; color: #e53e3e;"">{order.TotalAmount:N0} đ</td>
                    </tr>
                </tfoot>
            </table>
        </div>

        <!-- VietQR Section if applicable -->
        <div style=""padding: 0 28px;"">
            {qrSection}
        </div>

        <!-- Call to Action -->
        <div style=""padding: 20px 28px; text-align: center; background: #fffaf0; border-top: 1px dashed #feebc8; margin-top: 20px;"">
            <p style=""font-size: 13px; color: #7b341e; margin: 0 0 14px 0;"">
                Mẹ có thể theo dõi hành trình giao nhận hàng trực tiếp bất kỳ lúc nào trên hệ thống:
            </p>
            <a href=""http://localhost:5299/OrderTracking?orderCode={order.OrderCode}&phone={order.CustomerPhone}"" style=""display: inline-block; background: #3182ce; color: #ffffff; text-decoration: none; font-size: 14px; font-weight: 600; padding: 10px 24px; border-radius: 50px; box-shadow: 0 2px 6px rgba(49,130,206,0.3);"">
                🚚 Tra Cứu & Theo Dõi Đơn Hàng
            </a>
        </div>

        <!-- Footer -->
        <div style=""background: #edf2f7; padding: 24px; text-align: center; color: #718096; font-size: 12px; line-height: 1.6;"">
            <p style=""margin: 0 0 6px 0; font-weight: 600; color: #4a5568;"">BABYSHOP STUDIO KIDS - ĐỒNG HÀNH CÙNG TUỔI THƠ CỦA BÉ</p>
            <p style=""margin: 0 0 4px 0;"">📍 Địa chỉ: Số 123 Đường Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, Hà Nội</p>
            <p style=""margin: 0 0 4px 0;"">📞 Hotline CSKH: <b>1900 6868</b> | Email: <b>babyshop.cskh@gmail.com</b></p>
            <p style=""margin: 8px 0 0 0; color: #a0aec0; font-size: 11px;"">Đây là email tự động từ hệ thống quản lý đơn hàng BabyShop. Vui lòng không trả lời trực tiếp email này.</p>
        </div>

    </div>
</body>
</html>";
        }
    }
}
