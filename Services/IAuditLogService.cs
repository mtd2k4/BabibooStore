namespace WebsiteBabyShop.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, string entityName, int? entityId, string description, string? oldValues = null, string? newValues = null, string? userName = null, int? userId = null);
    }
}
