using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;
using WebsiteBabyShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Kestrel và FormOptions hỗ trợ upload video bóc hàng dung lượng tối đa 100MB (+20MB cho ảnh và form data)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 120 * 1024 * 1024; // 120MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 120 * 1024 * 1024; // 120MB
    options.ValueLengthLimit = 120 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 120 * 1024 * 1024;
});

// Cấu hình xuất tiếng Việt UTF-8 chuẩn đẹp, không bị mã hóa thành &#x...;
builder.Services.AddSingleton(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.All }));

// 1. Thêm kết nối Cơ sở Dữ liệu SQL Server / LocalDB
builder.Services.AddDbContext<BabyShopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Thêm Cache & Session cho Giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Thêm Xác thực Cookie (Authentication)
builder.Services.AddAuthentication("BabyShopCookieAuth")
    .AddCookie("BabyShopCookieAuth", options =>
    {
        options.Cookie.Name = "BabyShop.AuthCookie_v2";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddHttpContextAccessor();

// 4. Đăng ký Dịch vụ Real AI API (Google Gemini / OpenAI)
builder.Services.AddHttpClient<IAIService, GeminiAIService>();

// 5. Đăng ký Audit Log Service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// 6. Đăng ký Email Service gửi thông báo đơn hàng
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// 7. Đăng ký Controllers và Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Khởi tạo Database và nạp dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BabyShopDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo cơ sở dữ liệu.");
    }
}

// Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Định tuyến cho Area Admin
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Định tuyến mặc định cho Khách hàng
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
