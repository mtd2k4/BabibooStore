using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatController : Controller
    {
        private readonly BabyShopDbContext _context;

        public ChatController(BabyShopDbContext context)
        {
            _context = context;
        }

        public class AdminReplyDto
        {
            public int ConversationId { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        // 1. Giao diện Live Chat Center
        public IActionResult Index()
        {
            ViewData["Title"] = "Trung Tâm Live Chat CSKH";
            return View();
        }

        // 2. Lấy danh sách các cuộc trò chuyện của khách hàng
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var conversations = await _context.ChatConversations
                .Include(c => c.Messages)
                .OrderByDescending(c => c.LastMessageAt)
                .Take(50)
                .Select(c => new
                {
                    id = c.Id,
                    sessionId = c.SessionId,
                    customerName = c.CustomerName,
                    customerPhone = c.CustomerPhone ?? "",
                    lastMessageAt = c.LastMessageAt.ToString("HH:mm dd/MM"),
                    hasUnreadAdmin = c.HasUnreadAdmin,
                    unreadCount = c.Messages.Count(m => m.Sender == "Customer" && !m.IsRead),
                    lastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Message).FirstOrDefault() ?? ""
                })
                .ToListAsync();

            return Json(conversations);
        }

        // 3. Lấy toàn bộ tin nhắn trong một cuộc trò chuyện và đánh dấu đã đọc
        [HttpGet]
        public async Task<IActionResult> GetMessages(int id)
        {
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy cuộc hội thoại" });
            }

            // Đánh dấu tất cả tin nhắn từ khách hàng là đã đọc
            var unreadMsgs = conversation.Messages.Where(m => m.Sender == "Customer" && !m.IsRead).ToList();
            if (unreadMsgs.Any())
            {
                foreach (var msg in unreadMsgs)
                {
                    msg.IsRead = true;
                }
                conversation.HasUnreadAdmin = false;
                await _context.SaveChangesAsync();
            }

            var msgs = conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    sender = m.Sender,
                    senderName = m.SenderName,
                    message = m.Message,
                    time = m.CreatedAt.ToString("HH:mm dd/MM")
                })
                .ToList();

            return Json(new
            {
                success = true,
                conversationId = conversation.Id,
                customerName = conversation.CustomerName,
                customerPhone = conversation.CustomerPhone ?? "",
                messages = msgs
            });
        }

        // 4. Nhân viên gửi phản hồi cho khách hàng
        [HttpPost]
        public async Task<IActionResult> SendReply([FromBody] AdminReplyDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message) || request.ConversationId <= 0)
            {
                return Json(new { success = false, message = "Nội dung tin nhắn không hợp lệ." });
            }

            var conversation = await _context.ChatConversations.FindAsync(request.ConversationId);
            if (conversation == null)
            {
                return Json(new { success = false, message = "Cuộc trò chuyện không tồn tại." });
            }

            string staffName = "Thu Hà (CSKH)";
            if (User.Identity?.IsAuthenticated == true)
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (adminUser != null)
                {
                    staffName = adminUser.FullName + " (CSKH)";
                }
            }

            var replyMsg = new ChatMessage
            {
                ConversationId = conversation.Id,
                Sender = "Staff",
                SenderName = staffName,
                Message = request.Message.Trim(),
                CreatedAt = DateTime.Now,
                IsRead = true
            };

            conversation.LastMessageAt = DateTime.Now;
            conversation.HasUnreadAdmin = false;

            _context.ChatMessages.Add(replyMsg);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = replyMsg.Message,
                senderName = replyMsg.SenderName,
                time = replyMsg.CreatedAt.ToString("HH:mm dd/MM")
            });
        }

        // 5. Đếm số hội thoại chưa đọc để hiển thị Badge đỏ trên Sidebar Admin
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _context.ChatConversations.CountAsync(c => c.HasUnreadAdmin);
            return Json(new { unreadCount = count });
        }
    }
}
