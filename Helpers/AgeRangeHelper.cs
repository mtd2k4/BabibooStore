using System.Globalization;
using System.Text.RegularExpressions;

namespace WebsiteBabyShop.Helpers
{
    public static class AgeRangeHelper
    {
        public static readonly IReadOnlyList<string> StandardOptions = new[]
        {
            "Sơ sinh (0 - 24 tháng)",
            "Bé tập đi (1 - 3 tuổi)",
            "Mẫu giáo (3 - 6 tuổi)",
            "Tiểu học (6 - 12 tuổi)"
        };

        /// <summary>
        /// Kiểm tra xem giá trị nhóm tuổi của sản phẩm có khớp với nhóm tuổi đang lọc hay không.
        /// Hỗ trợ khớp chính xác các nhóm tuổi chuẩn hóa (và cô lập giữa các nhóm chuẩn hóa),
        /// đồng thời hỗ trợ chồng lấn khoảng tuổi cho dữ liệu cũ dạng "1 - 6 tuổi", "0 - 12 tháng"...
        /// </summary>
        public static bool Matches(string? productAgeRange, string? filterAgeRange)
        {
            if (string.IsNullOrWhiteSpace(filterAgeRange))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(productAgeRange))
            {
                return false;
            }

            string prodTrim = productAgeRange.Trim();
            string filterTrim = filterAgeRange.Trim();

            if (string.Equals(prodTrim, filterTrim, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Nếu sản phẩm đã được gán một nhóm tuổi chuẩn hóa khác với bộ lọc đang chọn thì không gộp chéo
            if (StandardOptions.Any(opt => string.Equals(opt, prodTrim, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Hỗ trợ dữ liệu dạng cũ (VD: "1 - 6 tuổi", "2 - 7 tuổi", "0 - 12 tháng") theo giao khoảng tháng tuổi
            if (TryParseIntervalMonths(prodTrim, out double pMin, out double pMax, out bool pIsMonths) &&
                TryParseIntervalMonths(filterTrim, out double fMin, out double fMax, out _))
            {
                // Bộ lọc Sơ sinh (0 - 24 tháng) chỉ khớp các sản phẩm cũ tính theo tháng hoặc bắt đầu dưới 1 tuổi
                if (fMin == 0 && fMax <= 24 && !pIsMonths && pMin >= 12)
                {
                    return false;
                }

                return pMax > fMin && pMin < fMax;
            }

            return prodTrim.Contains(filterTrim, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseIntervalMonths(string text, out double minMonths, out double maxMonths, out bool isMonths)
        {
            minMonths = 0;
            maxMonths = 0;
            isMonths = false;

            var match = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*[-–~]\s*(\d+(?:[.,]\d+)?)");
            if (!match.Success)
            {
                return false;
            }

            if (!double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double minVal) ||
                !double.TryParse(match.Groups[2].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double maxVal))
            {
                return false;
            }

            isMonths = text.Contains("tháng", StringComparison.OrdinalIgnoreCase)
                       || Regex.IsMatch(text, @"\d\s*M\b", RegexOptions.IgnoreCase);

            double multiplier = isMonths ? 1.0 : 12.0;
            minMonths = minVal * multiplier;
            maxMonths = maxVal * multiplier;
            return true;
        }
    }
}
