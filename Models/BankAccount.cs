using System.ComponentModel.DataAnnotations;

namespace WebsiteBabyShop.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngân hàng (mã VietQR)")]
        [StringLength(20)]
        [Display(Name = "Ngân hàng (Mã VietQR)")]
        public string BankCode { get; set; } = "MB";

        [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng thụ hưởng")]
        [StringLength(150)]
        [Display(Name = "Tên ngân hàng hiển thị")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản ngân hàng")]
        [StringLength(50)]
        [Display(Name = "Số tài khoản")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
        [StringLength(100)]
        [Display(Name = "Chủ tài khoản (In hoa không dấu)")]
        public string AccountHolder { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Chi nhánh mở tài khoản")]
        public string? Branch { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Mẫu giao diện mã VietQR")]
        public string QrTemplate { get; set; } = "compact2";

        [StringLength(500)]
        [Display(Name = "Ghi chú hướng dẫn chuyển khoản")]
        public string? Note { get; set; }

        [Display(Name = "Đặt làm tài khoản nhận tiền mặc định")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Kích hoạt nhận thanh toán")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Cập nhật lần cuối")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string BuildVietQrUrl(decimal amount, string transferMemo)
        {
            var code = string.IsNullOrWhiteSpace(BankCode) ? "MB" : BankCode.Trim();
            var accNo = string.IsNullOrWhiteSpace(AccountNumber) ? "" : AccountNumber.Trim();
            var template = string.IsNullOrWhiteSpace(QrTemplate) ? "compact2" : QrTemplate.Trim();
            var holder = string.IsNullOrWhiteSpace(AccountHolder) ? "" : AccountHolder.Trim().ToUpperInvariant();
            var memo = transferMemo ?? string.Empty;
            var amt = (long)Math.Max(0, amount);

            return $"https://img.vietqr.io/image/{Uri.EscapeDataString(code)}-{Uri.EscapeDataString(accNo)}-{Uri.EscapeDataString(template)}.png?amount={amt}&addInfo={Uri.EscapeDataString(memo)}&accountName={Uri.EscapeDataString(holder)}";
        }
    }
}
