using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebsiteBabyShop.Models
{
    public class ChatConversation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = "Khách Hàng";

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        public DateTime LastMessageAt { get; set; } = DateTime.Now;

        public bool HasUnreadAdmin { get; set; } = true;

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }
        public virtual ChatConversation? Conversation { get; set; }

        [Required]
        [StringLength(20)]
        public string Sender { get; set; } = "Customer"; // "Customer" or "Staff"

        [Required]
        [StringLength(100)]
        public string SenderName { get; set; } = "Khách Hàng";

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
