namespace WebsiteBabyShop.Services
{
    public interface IAIService
    {
        Task<string> GetAIResponseAsync(string userMessage, string? conversationContext = null);
    }
}
