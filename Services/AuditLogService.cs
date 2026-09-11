using System.Security.Claims;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly BabyShopDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(BabyShopDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string action,
            string entityName,
            int? entityId,
            string description,
            string? oldValues = null,
            string? newValues = null,
            string? userName = null,
            int? userId = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (string.IsNullOrEmpty(userName) && httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    userName = httpContext.User.Identity.Name ?? httpContext.User.FindFirst("Username")?.Value ?? "Admin";
                    var idClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out int uid))
                    {
                        userId = uid;
                    }
                }

                string? ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

                var log = new AuditLog
                {
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Description = description,
                    OldValues = oldValues,
                    NewValues = newValues,
                    UserName = userName ?? "Hệ thống",
                    UserId = userId,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.Now
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Tránh lỗi audit log làm gián đoạn nghiệp vụ chính
            }
        }
    }
}
