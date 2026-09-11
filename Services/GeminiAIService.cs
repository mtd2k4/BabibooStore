using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebsiteBabyShop.Services
{
    public class GeminiAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiAIService> _logger;

        private const string SystemPrompt = @"Bạn là Trợ lý AI thông minh, ngọt ngào và chu đáo của BabyShop - thương hiệu thời trang trẻ em cao cấp chuẩn phong cách Scandinavian tại Việt Nam.
Nhiệm vụ của bạn:
1. Tư vấn chọn size quần áo cho bé chuẩn xác theo cân nặng và độ tuổi:
- Bé dưới 9kg (0 - 12 tháng): Size 80 hoặc đồ sơ sinh sợi tre mềm mịn, phom vừa vặn.
- Bé 9 - 12.5kg (1 - 2 tuổi): Size 90 (1-2T).
- Bé 12.5 - 15.5kg (2 - 3 tuổi): Size 100 (2-3T).
- Bé 15.5 - 18.5kg (3 - 4 tuổi): Size 110 (3-4T).
- Bé 18.5 - 22.5kg (5 - 6 tuổi): Size 120 (5-6T).
- Bé 22.5 - 26.5kg (7 - 8 tuổi): Size 130 (7-8T).
2. Chất liệu sản phẩm: 100% Cotton Organic sợi tre kháng khuẩn, co giãn 4 chiều, mềm mát, không gây kích ứng da bé.
3. Chính sách bán hàng của BabyShop:
- MIỄN PHÍ VẬN CHUYỂN toàn quốc cho đơn từ 300.000đ (dưới 300K đồng giá 30.000đ).
- ĐỔI SIZE MIỄN PHÍ TẬN NHÀ TRONG 7 NGÀY (shipper mang size mới đến và lấy lại đồ cũ).
- Được mở kiểm tra hàng trước khi thanh toán. Hỗ trợ COD và chuyển khoản VietQR 24/7.
4. Phong cách trả lời: Xưng hô 'em' và 'mẹ' (hoặc 'ba'), nhẹ nhàng, tận tình, dùng biểu tượng cảm xúc (emoji) dễ thương. Trả lời ngắn gọn, có cấu trúc bullet point rõ ràng, dễ đọc trên điện thoại.
5. Nếu khách hàng muốn gặp người thật hoặc yêu cầu đổi hàng phức tạp, hướng dẫn khách chuyển sang tab 'Nhân Viên' để chuyên viên CSKH hỗ trợ trực tiếp.";

        public GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetAIResponseAsync(string userMessage, string? conversationContext = null)
        {
            var provider = _configuration["AIService:Provider"] ?? "Gemini";
            var apiKey = _configuration["AIService:ApiKey"];
            var model = _configuration["AIService:Model"] ?? "gemini-2.5-flash";

            // Nếu chưa cấu hình API Key thật
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_") || apiKey.Length < 10)
            {
                return GenerateFallbackResponse(userMessage, isKeyMissing: true);
            }

            try
            {
                if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    return await CallOpenAIApiAsync(userMessage, apiKey, model);
                }
                else
                {
                    return await CallGeminiApiAsync(userMessage, apiKey, model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Real AI API ({Provider})", provider);
                return GenerateFallbackResponse(userMessage, isKeyMissing: false);
            }
        }

        private async Task<string> CallGeminiApiAsync(string userMessage, string apiKey, string model)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = SystemPrompt }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userMessage }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 800
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(responseString);
                var replyText = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                if (!string.IsNullOrWhiteSpace(replyText))
                {
                    return replyText.Trim();
                }
            }
            else
            {
                var errContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API trả về mã lỗi HTTP {StatusCode}: {Error}", response.StatusCode, errContent);
            }

            return GenerateFallbackResponse(userMessage, isKeyMissing: false);
        }

        private async Task<string> CallOpenAIApiAsync(string userMessage, string apiKey, string model)
        {
            var url = "https://api.openai.com/v1/chat/completions";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.7,
                max_tokens = 800
            };

            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(responseString);
                var replyText = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();

                if (!string.IsNullOrWhiteSpace(replyText))
                {
                    return replyText.Trim();
                }
            }

            return GenerateFallbackResponse(userMessage, isKeyMissing: false);
        }

        private string GenerateFallbackResponse(string userMessage, bool isKeyMissing)
        {
            string norm = RemoveDiacritics(userMessage);

            string content;
            if (System.Text.RegularExpressions.Regex.IsMatch(norm, @"(\d+(\.\d+)?)\s*(kg|kilo|ki|can)"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(norm, @"(\d+(\.\d+)?)\s*(kg|kilo|ki|can)");
                if (double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double weight))
                {
                    if (weight < 9.0)
                        content = $"Dạ bé nặng {weight}kg (dưới 1 tuổi) thì mẹ nên chọn **Size 80** hoặc các set đồ sơ sinh sợi tre mềm mịn nhé! Phom vừa vặn, không gây cọ xát da bé ạ.";
                    else if (weight <= 12.5)
                        content = $"Dạ bé {weight}kg (khoảng 1 - 2 tuổi) mặc chuẩn nhất là **Size 90 (1-2T)** mẹ nhé! Vải cotton co giãn 4 chiều mềm mại giúp bé thoải mái vận động cả ngày ạ.";
                    else if (weight <= 15.5)
                        content = $"Dạ bé {weight}kg (khoảng 2 - 3 tuổi) chọn **Size 100 (2-3T)** là vừa vặn đẹp nhất mẹ nhé! Nếu bé trộm vía bụ bẫm, mẹ có thể lấy tăng 1 size lên 110 ạ.";
                    else if (weight <= 18.5)
                        content = $"Dạ bé {weight}kg (khoảng 3 - 4 tuổi) mẹ chọn **Size 110 (3-4T)** cho bé là chuẩn dáng luôn nhé!";
                    else if (weight <= 22.5)
                        content = $"Dạ bé {weight}kg (khoảng 5 - 6 tuổi) thì mẹ chọn **Size 120 (5-6T)** cho bé nhé!";
                    else
                        content = $"Dạ bé {weight}kg mẹ nên chọn **Size 130 (7-8T)** hoặc chuyển qua tab 'Nhân Viên' để em đo chuẩn vòng ngực và chiều cao cho bé nhé!";
                }
                else
                {
                    content = "Dạ mẹ cho em xin cân nặng của bé để em tư vấn size chuẩn xác nhất cho bé nhé!";
                }
            }
            else if (norm.Contains("ship") || norm.Contains("van chuyen") || norm.Contains("phi") || norm.Contains("giao hang"))
            {
                content = "📦 **Chính sách giao hàng của BabyShop:**\n• **MIỄN PHÍ VẬN CHUYỂN** toàn quốc cho đơn từ **300.000đ**!\n• Đơn dưới 300K phí giao đồng giá 30.000đ.\n• Thời gian giao: 1-2 ngày (Hà Nội, TP.HCM) và 2-3 ngày (các tỉnh khác).\n• Mẹ được mở kiểm tra hàng trước khi thanh toán cho shipper ạ!";
            }
            else if (norm.Contains("doi") || norm.Contains("tra") || norm.Contains("loi") || norm.Contains("bao hanh"))
            {
                content = "✨ **Chính sách đổi hàng của BabyShop:**\n• Hỗ trợ **ĐỔI SIZE MIỄN PHÍ TRONG 7 NGÀY** tận nhà.\n• Shipper sẽ mang size mới tới tận nhà và nhận lại đồ cũ, mẹ không cần phải ra bưu cục gửi hàng.\n• Đồ đổi còn nguyên tem mác và chưa qua giặt ủi mẹ nhé!";
            }
            else if (norm.Contains("thanh toan") || norm.Contains("chuyen khoan") || norm.Contains("cod") || norm.Contains("vietqr"))
            {
                content = "💳 **Phương thức thanh toán tiện lợi:**\n1. **Thanh toán khi nhận hàng (COD):** Kiểm tra đồ ưng ý rồi mới trả tiền.\n2. **Chuyển khoản VietQR:** Quét mã QR 24/7 xác nhận đơn hàng tự động tức thì.";
            }
            else
            {
                content = "Dạ em chào mẹ! Em là **Trợ lý AI của BabyShop** 🤖.\n\n" +
                          "Em có thể hỗ trợ mẹ tức thì:\n" +
                          "• **Tư vấn size:** Nhắn số cân nặng (ví dụ: *'bé 12kg'*, *'bé 15kg'*)\n" +
                          "• **Gợi ý mẫu hot:** Gõ *'váy bé gái'*, *'đồ bé trai'*, hoặc *'đồ sơ sinh'*\n" +
                          "• **Chính sách:** Gõ *'freeship'*, *'đổi size'*, hoặc *'thanh toán'*\n\n" +
                          "Hoặc mẹ chuyển qua tab **'Nhân Viên'** để gặp chuyên viên CSKH hỗ trợ trực tiếp nhé!";
            }

            if (isKeyMissing)
            {
                content += "\n\n*(💡 Lưu ý: Bạn có thể điền Gemini/OpenAI API Key vào file appsettings.json tại mục AIService:ApiKey để kích hoạt AI đàm thoại thời gian thực).*";
            }

            return content;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLower().Replace('đ', 'd').Replace('Đ', 'd');
        }
    }
}
