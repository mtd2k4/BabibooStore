namespace WebsiteBabyShop.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderName { get; set; } = "BabyShop Studio Kids";
        public string SenderEmail { get; set; } = "babyshop.cskh@gmail.com";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool SaveToDisk { get; set; } = true;
    }
}
