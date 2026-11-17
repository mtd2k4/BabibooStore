using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Helpers;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.ViewComponents
{
    public class VietQrPaymentViewModel
    {
        public string ComponentUid { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransferMemo { get; set; } = string.Empty;
        public bool IsPreview { get; set; }
        public bool SyncFromCheckoutTotal { get; set; }
        public List<BankAccount> ActiveBanks { get; set; } = new();
        public BankAccount SelectedBank { get; set; } = VietQrBankHelper.CreateFallbackDefault();
        public string InitialQrUrl { get; set; } = string.Empty;
    }

    public class VietQrPaymentViewComponent : ViewComponent
    {
        private readonly BabyShopDbContext _context;

        public VietQrPaymentViewComponent(BabyShopDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tải cấu hình ngân hàng đang hoạt động và hiển thị khung thanh toán VietQR dùng chung.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync(
            decimal amount,
            string transferMemo,
            bool isPreview = false,
            bool syncFromCheckoutTotal = false,
            List<BankAccount>? activeBanks = null,
            BankAccount? defaultBank = null)
        {
            var banks = activeBanks?.Where(b => b != null).ToList() ?? new List<BankAccount>();

            if (banks.Count == 0)
            {
                try
                {
                    banks = await _context.BankAccounts
                        .AsNoTracking()
                        .Where(b => b.IsActive)
                        .OrderByDescending(b => b.IsDefault)
                        .ThenBy(b => b.Id)
                        .ToListAsync();
                }
                catch
                {
                    banks = new List<BankAccount>();
                }
            }

            if (banks.Count == 0)
            {
                banks.Add(VietQrBankHelper.CreateFallbackDefault());
            }

            var selected = defaultBank
                           ?? banks.FirstOrDefault(b => b.IsDefault)
                           ?? banks.FirstOrDefault()
                           ?? VietQrBankHelper.CreateFallbackDefault();

            var safeMemo = string.IsNullOrWhiteSpace(transferMemo) ? "BABYSHOP" : transferMemo.Trim();
            var safeAmount = amount < 0 ? 0m : amount;

            var model = new VietQrPaymentViewModel
            {
                ComponentUid = "vqr-" + Guid.NewGuid().ToString("N")[..8],
                Amount = safeAmount,
                TransferMemo = safeMemo,
                IsPreview = isPreview,
                SyncFromCheckoutTotal = syncFromCheckoutTotal,
                ActiveBanks = banks,
                SelectedBank = selected,
                InitialQrUrl = selected.BuildVietQrUrl(safeAmount, safeMemo)
            };

            return View(model);
        }
    }
}
