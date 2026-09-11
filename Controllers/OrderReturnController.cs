using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBabyShop.Models;

namespace WebsiteBabyShop.Controllers
{
    public class OrderReturnController : Controller
    {
        private readonly BabyShopDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<OrderReturnController> _logger;

        // Giới hạn file video tối đa 100MB (104,857,600 bytes)
        private const long MaxVideoSize = 100 * 1024 * 1024;
        private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mov", ".webm", ".avi", ".mkv" };
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public OrderReturnController(BabyShopDbContext context, IWebHostEnvironment env, ILogger<OrderReturnController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: /OrderReturn/Create?orderId=5
        public async Task<IActionResult> Create(int? orderId, string? orderCode)
        {
            Order? order = null;
            if (orderId.HasValue)
            {
                order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(orderCode))
            {
                order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode.Trim());
            }

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng để yêu cầu đổi/trả.";
                return RedirectToAction("MyOrders", "Account");
            }

            // Kiểm tra xem đơn hàng đã có yêu cầu đổi trả đang xử lý hay chưa
            var existingRequest = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.OrderId == order.Id && (r.Status == "Pending" || r.Status == "Approved"));

            if (existingRequest != null)
            {
                TempData["InfoMessage"] = $"Đơn hàng #{order.OrderCode} đã có yêu cầu đổi/trả mã #{existingRequest.ReturnCode} đang được xử lý.";
                return RedirectToAction("Details", new { id = existingRequest.Id });
            }

            // Lấy thông tin tài khoản hiện tại (nếu có)
            int? currentUserId = null;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int uid)) currentUserId = uid;
            }

            var model = new ReturnRequest
            {
                OrderId = order.Id,
                Order = order,
                UserId = currentUserId ?? order.UserId,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerEmail = order.CustomerEmail,
                PickupAddress = order.ShippingAddress,
                FaultSource = "ShopFault",
                RequestType = "Exchange",
                ReasonCategory = "Bé mặc chật/rộng (Đổi size)",
                CarrierName = "ViettelPost / Giao Hàng Nhanh",
                CarrierTrackingCode = "VN-" + order.OrderCode
            };

            ViewBag.Order = order;
            ViewBag.OrderDetails = order.OrderDetails.ToList();

            return View(model);
        }

        // POST: /OrderReturn/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(120 * 1024 * 1024)] // 120MB tổng request
        [RequestFormLimits(MultipartBodyLengthLimit = 120 * 1024 * 1024)]
        public async Task<IActionResult> Create(ReturnRequest model, List<IFormFile>? proofFiles, IFormFile? unboxingVideo)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng cần đổi/trả.";
                return RedirectToAction("MyOrders", "Account");
            }

            // 1. Kiểm tra và upload video bóc hàng (tối đa 100MB)
            if (unboxingVideo != null && unboxingVideo.Length > 0)
            {
                if (unboxingVideo.Length > MaxVideoSize)
                {
                    ModelState.AddModelError("UnboxingVideoPath", "Video bóc hàng vượt quá dung lượng tối đa cho phép (100MB). Vui lòng nén video hoặc chọn video ngắn hơn.");
                }
                else
                {
                    var videoExt = Path.GetExtension(unboxingVideo.FileName).ToLowerInvariant();
                    if (!AllowedVideoExtensions.Contains(videoExt))
                    {
                        ModelState.AddModelError("UnboxingVideoPath", $"Định dạng video \"{videoExt}\" không được hỗ trợ. Vui lòng tải file .mp4, .mov, .webm, .avi, .mkv.");
                    }
                    else
                    {
                        try
                        {
                            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                            var videoDir = Path.Combine(webRoot, "uploads", "returns", "videos");
                            if (!Directory.Exists(videoDir)) Directory.CreateDirectory(videoDir);

                            var fileName = $"unboxing-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}{videoExt}";
                            var fullPath = Path.Combine(videoDir, fileName);

                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                await unboxingVideo.CopyToAsync(stream);
                            }

                            model.UnboxingVideoPath = $"/uploads/returns/videos/{fileName}";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi lưu video bóc hàng");
                            ModelState.AddModelError("UnboxingVideoPath", "Không thể tải lên video bóc hàng. Vui lòng thử lại.");
                        }
                    }
                }
            }

            // 2. Kiểm tra và upload các hình ảnh bằng chứng (ảnh lỗi sản phẩm, ảnh kiện hàng)
            var uploadedImages = new List<string>();
            if (proofFiles != null && proofFiles.Count > 0)
            {
                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var imgDir = Path.Combine(webRoot, "uploads", "returns");
                if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                foreach (var file in proofFiles)
                {
                    if (file.Length > 0 && file.Length <= 15 * 1024 * 1024) // Tối đa 15MB/ảnh
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (AllowedImageExtensions.Contains(ext))
                        {
                            var imgName = $"proof-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
                            var fullPath = Path.Combine(imgDir, imgName);

                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            uploadedImages.Add($"/uploads/returns/{imgName}");
                        }
                    }
                }
                if (uploadedImages.Any())
                {
                    model.ProofImages = string.Join(";", uploadedImages);
                }
            }

            // Ràng buộc nếu chọn hoàn tiền phải có STK ngân hàng
            if (model.RequestType == "Refund" && string.IsNullOrWhiteSpace(model.BankAccountNo))
            {
                ModelState.AddModelError("BankAccountNo", "Vui lòng nhập Số tài khoản ngân hàng để Shop hoàn tiền.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Order = order;
                ViewBag.OrderDetails = order.OrderDetails.ToList();
                return View(model);
            }

            // Tạo mã yêu cầu đổi trả RT-YYYYMMDD-XXXX
            model.ReturnCode = "RT-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
            model.Status = "Pending";
            model.CreatedAt = DateTime.Now;

            _context.ReturnRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Gửi yêu cầu đổi/trả hàng thành công! Mã phiếu yêu cầu: #{model.ReturnCode}. Đội ngũ CSKH BabyShop sẽ thẩm định bằng chứng và phản hồi trong 2-4 giờ.";

            return RedirectToAction("Details", new { id = model.Id });
        }

        // GET: /OrderReturn/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o!.OrderDetails)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin yêu cầu đổi/trả hàng.";
                return RedirectToAction("MyOrders", "Account");
            }

            return View(returnRequest);
        }

        // GET: /OrderReturn/Track
        public async Task<IActionResult> Track(string? returnCode, string? phone)
        {
            ViewData["Title"] = "Tra Cứu Tiến Độ Đổi Trả - BabyShop";

            if (string.IsNullOrWhiteSpace(returnCode) && string.IsNullOrWhiteSpace(phone))
            {
                return View();
            }

            var query = _context.ReturnRequests
                .Include(r => r.Order)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(returnCode))
            {
                query = query.Where(r => r.ReturnCode == returnCode.Trim());
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                query = query.Where(r => r.CustomerPhone == phone.Trim());
            }

            var results = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.Results = results;
            ViewBag.SearchedCode = returnCode;
            ViewBag.SearchedPhone = phone;

            return View();
        }

        // GET: /OrderReturn/MyReturns
        public async Task<IActionResult> MyReturns()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/OrderReturn/MyReturns" });
            }

            int currentUserId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int uid)) currentUserId = uid;

            var list = await _context.ReturnRequests
                .Include(r => r.Order)
                .Where(r => r.UserId == currentUserId || r.Order!.UserId == currentUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(list);
        }
    }
}
