using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using SacoStayAPI.Service;
using SacoStayAPI.Services;
using System.Net;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Account> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationDispatcher _notificationDispatcher;

        public AdminController(
            UserManager<Account> userManager,
            IUnitOfWork unitOfWork,
            INotificationDispatcher notificationDispatcher)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _notificationDispatcher = notificationDispatcher;
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var posts = (await _unitOfWork.Repository<RoomPost>().GetAllAsync()).ToList();
            var pendingApproval = posts.Count(p =>
                p.Status == "PendingApproval" || p.Status == "PendingPayment");

            return Ok(new
            {
                totalUsers = _userManager.Users.Count(),
                totalRoomPosts = posts.Count,
                pendingRoomPosts = pendingApproval,
                activeRoomPosts = posts.Count(p => p.Status == "Active"),
                hiddenRoomPosts = posts.Count(p => p.Status == "Hidden")
            });
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int limit = 100)
        {
            var users = _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToList();

            var result = new List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var avatar = u.ProfileImages?.FirstOrDefault();
                result.Add(new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.PhoneNumber,
                    u.FirstName,
                    u.LastName,
                    u.CreatedAt,
                    Roles = roles,
                    Avatar = avatar,
                    DisplayName = $"{u.FirstName} {u.LastName}".Trim()
                });
            }

            return Ok(result);
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpGet("room-posts")]
        public async Task<IActionResult> GetRoomPosts([FromQuery] string? status)
        {
            var posts = (await _unitOfWork.Repository<RoomPost>().GetAllAsync()).ToList();

            if (!string.IsNullOrWhiteSpace(status))
            {
                posts = posts.Where(p =>
                    string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            posts = posts.OrderByDescending(p => p.CreatedAt).ToList();

            var result = new List<object>();
            foreach (var p in posts)
            {
                var landlord = await _userManager.FindByIdAsync(p.UserId.ToString());
                result.Add(new
                {
                    p.Id,
                    p.Title,
                    p.Price,
                    p.City,
                    p.District,
                    p.DetailedAddress,
                    p.Status,
                    p.PackageTier,
                    p.CreatedAt,
                    p.Images,
                    p.UserId,
                    LandlordName = landlord != null
                        ? $"{landlord.FirstName} {landlord.LastName}".Trim()
                        : "",
                    LandlordEmail = landlord?.Email
                });
            }

            return Ok(result);
        }

        /// <summary>Thống kê doanh thu / giao dịch PayOS cho admin dashboard.</summary>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpGet("payment-stats")]
        public async Task<IActionResult> GetPaymentStats()
        {
            var txs = (await _unitOfWork.Repository<PaymentTransaction>().GetAllAsync()).ToList();
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastMonthStart = monthStart.AddMonths(-1);

            var success = txs.Where(t =>
                string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase)).ToList();

            decimal revenueThisMonth = success.Where(t => t.CreatedAt >= monthStart).Sum(t => t.Amount);
            decimal revenueLastMonth = success
                .Where(t => t.CreatedAt >= lastMonthStart && t.CreatedAt < monthStart)
                .Sum(t => t.Amount);
            double? growth = null;
            if (revenueLastMonth > 0)
                growth = Math.Round((double)((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100), 1);
            else if (revenueThisMonth > 0)
                growth = 100;

            var byBuyer = success
                .GroupBy(t => string.IsNullOrWhiteSpace(t.BuyerType) ? "Unknown" : t.BuyerType)
                .Select(g => new
                {
                    buyerType = g.Key,
                    count = g.Count(),
                    amount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            var byStatus = txs
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Status) ? "Unknown" : t.Status)
                .Select(g => new { status = g.Key, count = g.Count(), amount = g.Sum(x => x.Amount) })
                .ToList();

            // Chỉ trả các ngày có giao dịch thành công (bỏ qua ngày 0đ), tối đa ~90 ngày gần nhất
            var dayFrom = now.Date.AddDays(-89);
            var daily = success
                .Where(t => t.CreatedAt.Date >= dayFrom)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    label = g.Key.ToString("dd/MM"),
                    amount = g.Sum(x => x.Amount),
                    count = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Ok(new
            {
                totalRevenue = success.Sum(t => t.Amount),
                revenueThisMonth,
                revenueLastMonth,
                revenueGrowthPercent = growth,
                totalTransactions = txs.Count,
                successCount = success.Count,
                pendingCount = txs.Count(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                failedCount = txs.Count(t => string.Equals(t.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
                cancelledCount = txs.Count(t => string.Equals(t.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)),
                byBuyerType = byBuyer,
                byStatus,
                dailyRevenue = daily
            });
        }

        /// <summary>Danh sách toàn bộ giao dịch thanh toán (admin).</summary>
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string? status,
            [FromQuery] string? buyerType,
            [FromQuery] int limit = 200)
        {
            var txs = (await _unitOfWork.Repository<PaymentTransaction>().GetAllAsync()).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                txs = txs.Where(t =>
                    string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(buyerType))
            {
                txs = txs.Where(t =>
                    string.Equals(t.BuyerType, buyerType, StringComparison.OrdinalIgnoreCase));
            }

            var list = txs
                .OrderByDescending(t => t.CreatedAt)
                .Take(Math.Clamp(limit, 1, 1000))
                .ToList();

            var roomIds = list.Where(t => t.RoomPostId.HasValue).Select(t => t.RoomPostId!.Value).Distinct().ToList();
            var rooms = roomIds.Count == 0
                ? new Dictionary<Guid, RoomPost>()
                : (await _unitOfWork.Repository<RoomPost>().GetAllAsync())
                    .Where(r => roomIds.Contains(r.Id))
                    .ToDictionary(r => r.Id);

            var result = new List<object>();
            foreach (var t in list)
            {
                string? buyerName = null;
                string? buyerEmail = null;
                Guid? buyerUserId = t.UserId;

                // Gói chủ trọ cũ: UserId null — lấy chủ tin từ RoomPost.UserId
                if (!buyerUserId.HasValue && t.RoomPostId.HasValue && rooms.TryGetValue(t.RoomPostId.Value, out var roomForBuyer))
                    buyerUserId = roomForBuyer.UserId;

                if (buyerUserId.HasValue)
                {
                    var u = await _userManager.FindByIdAsync(buyerUserId.Value.ToString());
                    if (u != null)
                    {
                        buyerName = $"{u.FirstName} {u.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(buyerName)) buyerName = u.UserName;
                        buyerEmail = u.Email;
                    }
                }

                string? roomTitle = null;
                if (t.RoomPostId.HasValue && rooms.TryGetValue(t.RoomPostId.Value, out var room))
                    roomTitle = room.Title;

                result.Add(new
                {
                    t.Id,
                    t.OrderId,
                    t.Amount,
                    t.Status,
                    t.PaymentMethod,
                    t.TransactionNo,
                    t.CreatedAt,
                    t.RoomPostId,
                    RoomTitle = roomTitle,
                    t.PackageName,
                    t.BuyerType,
                    UserId = buyerUserId,
                    BuyerName = buyerName,
                    BuyerEmail = buyerEmail
                });
            }

            return Ok(result);
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpPost("room-posts/{id}/approve")]
        public async Task<IActionResult> ApproveRoomPost(Guid id)
        {
            var post = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(id);
            if (post == null) return NotFound(new { message = "Không tìm thấy tin đăng." });

            post.Status = "Active";
            if (!post.PackageExpiresAt.HasValue)
            {
                post.PackageExpiresAt = DateTime.UtcNow.AddDays(30);
            }

            _unitOfWork.Repository<RoomPost>().Update(post);
            await _unitOfWork.CompleteAsync();

            await _notificationDispatcher.NotifyAsync(
                post.UserId,
                "Bài đăng đã được duyệt",
                $"Bài đăng '{post.Title}' của bạn đã được admin duyệt và hiển thị công khai.",
                "system",
                $"/owner/my-posts?roomPostId={post.Id}");

            return Ok(new { message = "Đã duyệt tin đăng. Tin hiển thị công khai.", status = post.Status });
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpPost("room-posts/{id}/reject")]
        public async Task<IActionResult> RejectRoomPost(Guid id)
        {
            var post = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(id);
            if (post == null) return NotFound(new { message = "Không tìm thấy tin đăng." });

            post.Status = "Hidden";
            _unitOfWork.Repository<RoomPost>().Update(post);
            await _unitOfWork.CompleteAsync();

            await _notificationDispatcher.NotifyAsync(
                post.UserId,
                "Bài đăng bị từ chối",
                $"Bài đăng '{post.Title}' đã bị admin từ chối / ẩn.",
                "system",
                $"/owner/my-posts?roomPostId={post.Id}");

            return Ok(new { message = "Đã từ chối / ẩn tin đăng.", status = post.Status });
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpPost("reports/{id}/process")]
        public async Task<IActionResult> ProcessReport(
            Guid id,
            [FromBody] ProcessReportRequest request,
            [FromServices] EmailService emailService)
        {
            var report = await _unitOfWork.Repository<Report>().GetQueryable()
                .Include(r => r.ReportedRoom)
                .Include(r => r.ReportedUser)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (report == null)
                return NotFound(new { message = "Không tìm thấy báo cáo." });

            if (!string.Equals(report.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Báo cáo này đã được xử lý rồi." });

            if (!request.IsValid)
            {
                report.Status = "Rejected";
                _unitOfWork.Repository<Report>().Update(report);
                await _unitOfWork.CompleteAsync();
                return Ok(new { message = "Đã từ chối báo cáo. Không áp dụng hình phạt cho chủ trọ / người bị báo cáo." });
            }

            report.Status = "Approved";
            Guid? violatorId = null;
            Guid? hiddenRoomId = null;
            string? hiddenRoomTitle = null;

            if (report.ReportedRoomId.HasValue)
            {
                var room = report.ReportedRoom
                    ?? await _unitOfWork.Repository<RoomPost>().GetByIdAsync(report.ReportedRoomId.Value);
                if (room != null)
                {
                    room.Status = "Hidden";
                    _unitOfWork.Repository<RoomPost>().Update(room);
                    violatorId = room.UserId;
                    hiddenRoomId = room.Id;
                    hiddenRoomTitle = room.Title;
                }
            }
            else if (report.ReportedUserId.HasValue)
            {
                violatorId = report.ReportedUserId;
            }

            _unitOfWork.Repository<Report>().Update(report);
            await _unitOfWork.CompleteAsync();

            if (violatorId.HasValue)
            {
                var violator = await _userManager.FindByIdAsync(violatorId.Value.ToString());
                if (violator != null)
                {
                    var landlordRoomIds = await _unitOfWork.Repository<RoomPost>().GetQueryable()
                        .Where(p => p.UserId == violator.Id)
                        .Select(p => p.Id)
                        .ToListAsync();

                    var previousApproved = await _unitOfWork.Repository<Report>().GetQueryable()
                        .CountAsync(r =>
                            r.ReportId != report.ReportId &&
                            r.Status == "Approved" &&
                            (r.ReportedUserId == violator.Id ||
                             (r.ReportedRoomId != null && landlordRoomIds.Contains(r.ReportedRoomId.Value))));

                    var totalViolations = previousApproved + 1;

                    var displayName = !string.IsNullOrWhiteSpace(violator.FirstName)
                        ? $"{violator.FirstName.Trim()} {violator.LastName?.Trim()}".Trim()
                        : (violator.UserName ?? "bạn");
                    var reasonText = string.IsNullOrWhiteSpace(report.Reason)
                        ? "Vi phạm nội quy SacoStay"
                        : report.Reason.Replace(";", ", ");
                    var reasonHtml = WebUtility.HtmlEncode(reasonText);
                    var roomEmailNote = hiddenRoomTitle != null
                        ? $"<br>Tin \"{WebUtility.HtmlEncode(hiddenRoomTitle)}\" đã bị ẩn."
                        : "";
                    var roomNotifyNote = hiddenRoomTitle != null
                        ? $" Tin \"{hiddenRoomTitle}\" đã bị ẩn."
                        : "";
                    var deterrence = GetReportDeterrenceLine(report.Reason);

                    var notifyLink = hiddenRoomId.HasValue
                        ? $"/owner/my-posts?roomPostId={hiddenRoomId}"
                        : "/landlord-profile";

                    await _notificationDispatcher.NotifyAsync(
                        violator.Id,
                        "Cảnh báo — yêu cầu không tái phạm",
                        $"Báo cáo về bạn đã được xác minh.{roomNotifyNote} Lý do: {reasonText}. {deterrence}",
                        "system",
                        notifyLink);

                    if (!string.IsNullOrWhiteSpace(violator.Email))
                    {
                        try
                        {
                            var nameHtml = WebUtility.HtmlEncode(displayName);
                            if (totalViolations >= 2)
                            {
                                await _userManager.SetLockoutEnabledAsync(violator, true);
                                await _userManager.SetLockoutEndDateAsync(violator, DateTimeOffset.MaxValue);
                                await emailService.SendEmailAsync(
                                    violator.Email,
                                    "Tài khoản SacoStay đã bị khóa",
                                    $"Chào {nameHtml},<br><br>" +
                                    $"Báo cáo mới về tài khoản/tin đăng của bạn đã được chấp nhận. <b>Lý do:</b> {reasonHtml}.{roomEmailNote}<br><br>" +
                                    $"{deterrence}<br><br>" +
                                    "Do tái phạm sau cảnh báo trước, tài khoản đã bị <b>khóa vĩnh viễn</b> — bạn không thể đăng nhập.<br><br>" +
                                    "SacoStay không tiết lộ thông tin người báo cáo.<br><br>Trân trọng,<br>SacoStay");
                            }
                            else
                            {
                                await emailService.SendEmailAsync(
                                    violator.Email,
                                    "Cảnh báo vi phạm — SacoStay",
                                    $"Chào {nameHtml},<br><br>" +
                                    $"Chúng tôi đã xác minh báo cáo về tài khoản/tin đăng của bạn là hợp lệ. <b>Lý do:</b> {reasonHtml}.{roomEmailNote}<br><br>" +
                                    $"{deterrence}<br><br>" +
                                    "Vui lòng sửa hoặc gỡ nội dung vi phạm và tuân thủ điều khoản. Vi phạm thêm có thể bị <b>khóa tài khoản vĩnh viễn</b>.<br><br>" +
                                    "Chúng tôi không công bố thông tin người báo cáo.<br><br>Trân trọng,<br>SacoStay");
                            }
                        }
                        catch
                        {
                            // Email lỗi (SMTP) không chặn xử lý báo cáo — admin vẫn nhận OK, user vẫn có thông báo in-app.
                        }
                    }
                }
            }

            return Ok(new
            {
                message = "Đã chấp nhận báo cáo. Tin vi phạm (nếu có) đã ẩn; chủ trọ đã được cảnh báo.",
                status = report.Status
            });
        }

        /// <summary>Một dòng nhắc nhở theo lý do báo cáo (không lộ người báo cáo).</summary>
        private static string GetReportDeterrenceLine(string? reason)
        {
            var r = reason ?? "";
            if (r.Contains("Lừa đảo", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("Scam", StringComparison.OrdinalIgnoreCase))
                return "Không được lừa đảo, thu tiền trước hoặc mô tả sai — vi phạm sẽ bị xử lý nghiêm.";
            if (r.Contains("quấy rối", StringComparison.OrdinalIgnoreCase))
                return "Quấy rối người dùng khác bị cấm; tái phạm có thể khóa tài khoản.";
            if (r.Contains("giả mạo", StringComparison.OrdinalIgnoreCase))
                return "Hồ sơ/ảnh giả mạo không được phép trên SacoStay.";
            if (r.Contains("sai lệch", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("không đúng", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("minh bạch", StringComparison.OrdinalIgnoreCase))
                return "Thông tin tin đăng phải đúng sự thật (giá, ảnh, địa chỉ).";
            if (r.Contains("Spam", StringComparison.OrdinalIgnoreCase) ||
                r.Contains("Quảng cáo", StringComparison.OrdinalIgnoreCase))
                return "Không spam tin hoặc quảng cáo trái phép.";
            if (r.Contains("không phù hợp", StringComparison.OrdinalIgnoreCase))
                return "Nội dung khiêu dâm, bạo lực hoặc gây shock sẽ bị gỡ.";
            return "Vui lòng rà soát lại tin đăng và hành vi trên nền tảng.";
        }
    }
}
