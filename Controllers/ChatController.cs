using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

namespace WebsiteBabyShop.Controllers
{
    public class ChatController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(BabyShopDbContext context, IAIService aiService, ILogger<ChatController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public class ChatMessageRequest
        {
            public string Message { get; set; } = string.Empty;
            public string Mode { get; set; } = "AI"; // "AI" or "Staff"
            public string? SessionId { get; set; }
            public string? CustomerName { get; set; }
            public string? CustomerPhone { get; set; }
        }

        public class ProductSuggestionDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return Json(new { success = false, reply = "Dạ mẹ vui lòng nhập nội dung tin nhắn nhé!" });
            }

            string userMsg = request.Message.Trim();
            string sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? ("sess_" + Guid.NewGuid().ToString("N")) : request.SessionId;

            // =========================================================================
            // 1. Chế độ chat với Nhân Viên CSKH (Staff Mode) - Lưu vào CSDL & chuyển Admin
            // =========================================================================
            if (request.Mode != null && request.Mode.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                var conversation = await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);

                if (conversation == null)
                {
                    int? userId = null;
                    string custName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Khách Hàng" : request.CustomerName;
                    
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                        if (user != null)
                        {
                            userId = user.Id;
                            custName = user.FullName;
                        }
                    }

                    conversation = new ChatConversation
                    {
                        SessionId = sessionId,
                        UserId = userId,
                        CustomerName = custName,
                        CustomerPhone = request.CustomerPhone,
                        LastMessageAt = DateTime.Now,
                        HasUnreadAdmin = true
                    };
                    _context.ChatConversations.Add(conversation);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    conversation.LastMessageAt = DateTime.Now;
                    conversation.HasUnreadAdmin = true;
                    if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
                    {
                        conversation.CustomerPhone = request.CustomerPhone;
                    }
                }

                // Lưu tin nhắn của khách hàng
                var customerMsg = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Sender = "Customer",
                    SenderName = conversation.CustomerName,
                    Message = userMsg,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                _context.ChatMessages.Add(customerMsg);
                await _context.SaveChangesAsync();

                // Lời nhắn phản hồi đầu tiên nếu hội thoại mới bắt đầu
                bool isFirstMessage = conversation.Messages.Count <= 1;
                string staffReply = isFirstMessage 
                    ? "Dạ em chào mẹ ạ! Lời nhắn của mẹ đã được chuyển đến bộ phận CSKH BabyShop 👩‍💼.\n\nChuyên viên tư vấn đang xem và sẽ phản hồi trực tiếp cho mẹ ngay tại khung chat này trong ít phút nhé!"
                    : "Đã gửi tin nhắn đến tư vấn viên.";

                return Json(new
                {
                    success = true,
                    mode = "Staff",
                    messageId = customerMsg.Id,
                    sessionId = conversation.SessionId,
                    reply = staffReply,
                    senderName = "Hệ Thống CSKH",
                    time = DateTime.Now.ToString("HH:mm")
                });
            }

            // =========================================================================
            // 2. Chế độ chat với Trợ Lý Ảo Real AI (AI Mode - Gemini / OpenAI)
            // =========================================================================
            string aiReply = await _aiService.GetAIResponseAsync(userMsg);
            List<ProductSuggestionDto> suggestedProducts = new List<ProductSuggestionDto>();

            // Phân tích từ khóa tìm kiếm sản phẩm để gửi kèm thẻ sản phẩm từ Database
            string normMsg = RemoveDiacritics(userMsg);
            if (normMsg.Contains("vay") || normMsg.Contains("dam") || normMsg.Contains("cong chua") || normMsg.Contains("be gai"))
            {
                var prods = await _context.Products
                    .Where(p => p.IsActive && (p.Gender == "Bé gái" || p.Name.Contains("Váy") || p.Name.Contains("Đầm")))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                suggestedProducts = prods.Select(p => new ProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.CurrentPrice,
                    ImageUrl = p.MainImageUrl ?? ""
                }).ToList();
            }
            else if (normMsg.Contains("be trai") || normMsg.Contains("polo") || normMsg.Contains("yem") || normMsg.Contains("kaki") || normMsg.Contains("short"))
            {
                var prods = await _context.Products
                    .Where(p => p.IsActive && (p.Gender == "Bé trai" || p.Name.Contains("Polo") || p.Name.Contains("Yếm") || p.Name.Contains("Short")))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                suggestedProducts = prods.Select(p => new ProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.CurrentPrice,
                    ImageUrl = p.MainImageUrl ?? ""
                }).ToList();
            }
            else if (normMsg.Contains("so sinh") || normMsg.Contains("body") || normMsg.Contains("day thang") || normMsg.Contains("be nho"))
            {
                var prods = await _context.Products
                    .Where(p => p.IsActive && (p.Gender == "Sơ sinh" || p.Name.Contains("Sơ Sinh") || p.Name.Contains("Body")))
                    .Take(3)
                    .ToListAsync();

                suggestedProducts = prods.Select(p => new ProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.CurrentPrice,
                    ImageUrl = p.MainImageUrl ?? ""
                }).ToList();
            }
            else if (normMsg.Contains("sale") || normMsg.Contains("khuyen mai") || normMsg.Contains("giam gia") || normMsg.Contains("uu dai"))
            {
                var prods = await _context.Products
                    .Where(p => p.IsActive && p.DiscountPrice.HasValue && p.DiscountPrice < p.Price)
                    .OrderByDescending(p => (p.Price - (p.DiscountPrice ?? 0)))
                    .Take(3)
                    .ToListAsync();

                suggestedProducts = prods.Select(p => new ProductSuggestionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.CurrentPrice,
                    ImageUrl = p.MainImageUrl ?? ""
                }).ToList();
            }

            return Json(new
            {
                success = true,
                mode = "AI",
                reply = aiReply,
                senderName = "Trợ Lý AI",
                time = DateTime.Now.ToString("HH:mm"),
                suggestedProducts = suggestedProducts
            });
        }

        // API tải lịch sử tin nhắn của phiên chat với nhân viên CSKH
        [HttpGet]
        public async Task<IActionResult> GetStaffMessages(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Json(new { success = false, messages = new List<object>() });
            }

            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

            if (conversation == null)
            {
                return Json(new { success = true, messages = new List<object>() });
            }

            var msgs = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    sender = m.Sender, // "Customer" or "Staff"
                    senderName = m.SenderName,
                    message = m.Message,
                    time = m.CreatedAt.ToString("HH:mm"),
                    isRead = m.IsRead
                })
                .ToList();

            return Json(new { success = true, messages = msgs });
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
