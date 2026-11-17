using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Helpers
{
    public static class VietQrBankHelper
    {
        public record VietQrBankInfo(string Code, string ShortName, string DisplayName);
        public record VietQrTemplateInfo(string Value, string Label, string Description);

        public static readonly IReadOnlyList<VietQrBankInfo> SupportedBanks = new List<VietQrBankInfo>
        {
            new("MB", "MB Bank", "MB Bank (Ngân hàng Quân Đội)"),
            new("VCB", "Vietcombank", "Vietcombank (Ngân hàng Ngoại thương Việt Nam)"),
            new("TCB", "Techcombank", "Techcombank (Ngân hàng Kỹ thương Việt Nam)"),
            new("BIDV", "BIDV", "BIDV (Ngân hàng Đầu tư và Phát triển Việt Nam)"),
            new("ICB", "VietinBank", "VietinBank (Ngân hàng Công thương Việt Nam)"),
            new("ACB", "ACB", "ACB (Ngân hàng Á Châu)"),
            new("VPB", "VPBank", "VPBank (Ngân hàng Việt Nam Thịnh Vượng)"),
            new("TPB", "TPBank", "TPBank (Ngân hàng Tiên Phong)"),
            new("STB", "Sacombank", "Sacombank (Ngân hàng Sài Gòn Thương Tín)"),
            new("VBA", "Agribank", "Agribank (Ngân hàng Nông nghiệp & PTNT)"),
            new("HDB", "HDBank", "HDBank (Ngân hàng Phát triển TP.HCM)"),
            new("VIB", "VIB", "VIB (Ngân hàng Quốc tế Việt Nam)"),
            new("SHB", "SHB", "SHB (Ngân hàng Sài Gòn - Hà Nội)"),
            new("MSB", "MSB", "MSB (Ngân hàng Hàng Hải Việt Nam)"),
            new("OCB", "OCB", "OCB (Ngân hàng Phương Đông)"),
            new("SEAB", "SeABank", "SeABank (Ngân hàng Đông Nam Á)"),
            new("LPB", "LPBank", "LPBank (Ngân hàng Lộc Phát Việt Nam)"),
            new("EIB", "Eximbank", "Eximbank (Ngân hàng Xuất Nhập khẩu Việt Nam)"),
            new("NAB", "Nam A Bank", "Nam A Bank (Ngân hàng Nam Á)"),
            new("CAKE", "CAKE by VPBank", "Ngân hàng số CAKE by VPBank"),
            new("TIMO", "Timo by BVBank", "Ngân hàng số Timo")
        };

        public static readonly IReadOnlyList<VietQrTemplateInfo> SupportedTemplates = new List<VietQrTemplateInfo>
        {
            new("compact2", "Compact 2 (Khuyên dùng)", "Hiển thị mã QR kèm logo VietQR, Napas và thông tin chuyển khoản chi tiết"),
            new("compact", "Compact Gọn Nhẹ", "Hiển thị mã QR gọn gàng kèm logo nhận diện VietQR & Napas 247"),
            new("qr_only", "Chỉ Mã QR (QR Only)", "Chỉ hiển thị khung mã QR vuông tối giản, không kèm chữ"),
            new("print", "Bản Đầy Đủ (Print)", "Khung biểu mẫu đầy đủ phù hợp tải về hoặc in dán tại quầy")
        };

        public static BankAccount CreateFallbackDefault() => new()
        {
            BankCode = "MB",
            BankName = "MB Bank (Ngân hàng Quân Đội)",
            AccountNumber = "212882",
            AccountHolder = "MAI TIEN DUNG",
            Branch = "Chi nhánh Hà Nội",
            QrTemplate = "compact2",
            Note = "Mẹ vui lòng giữ nguyên nội dung chuyển khoản là mã đơn hàng để hệ thống tự động gạch nợ và xác nhận đơn trong 1-3 phút nhé!",
            IsDefault = true,
            IsActive = true
        };
    }
}
