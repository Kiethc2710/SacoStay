using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using SacoStayAPI.Services;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Account> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(UserManager<Account> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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

            return Ok(new { message = "Đã từ chối / ẩn tin đăng.", status = post.Status });
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpPost("reports/{id}/process")]
        public async Task<IActionResult> ProcessReport(
                    Guid id,
                    [FromBody] ProcessReportRequest request,
                    [FromServices] EmailService _emailService) // Inject EmailService trực tiếp vào hàm
        {
            // 1. Lấy report lên cùng với dữ liệu Phòng và Người bị report
            var report = await _unitOfWork.Repository<Report>().GetByIdAsync(id);
                //.Include(r => r.ReportedRoom)
                //.Include(r => r.ReportedUser)
                //.FirstOrDefaultAsync(r => r.ReporterId == id);

            if (report == null)
                return NotFound(new { Message = "Không tìm thấy báo cáo." });

            if (report.Status != "Pending")
                return BadRequest(new { Message = "Báo cáo này đã được xử lý rồi." });

            // =========================================================
            // TRƯỜNG HỢP 1: REPORT SAI SỰ THẬT (REJECT)
            // =========================================================
            if (!request.IsValid)
            {
                report.Status = "Rejected";
                _unitOfWork.Repository<Report>().Update(report);
                await _unitOfWork.CompleteAsync();
                return Ok(new { Message = "Đã từ chối báo cáo. Không có hình phạt nào được áp dụng." });
            }

            // =========================================================
            // TRƯỜNG HỢP 2: REPORT ĐÚNG SỰ THẬT (APPROVE)
            // =========================================================
            report.Status = "Approved";
            Guid? violatorId = null; // ID của người vi phạm

            // A. Nếu report liên quan đến Phòng đăng
            if (report.ReportedRoomId != null && report.ReportedRoom != null)
            {
                // Ẩn bài đăng
                report.ReportedRoom.Status = "Hidden";

                // Update bài đăng vào DB
                _unitOfWork.Repository<RoomPost>().Update(report.ReportedRoom);

                // Người bị phạt chính là chủ cái phòng này
                violatorId = report.ReportedRoom.UserId;
            }
            // B. Nếu report thẳng vào User
            else if (report.ReportedUserId != null)
            {
                violatorId = report.ReportedUserId;
            }

            // =========================================================
            // TRƯỜNG HỢP 3: XỬ LÝ VI PHẠM (CẢNH CÁO / KHÓA ACC)
            // =========================================================
            if (violatorId.HasValue)
            {
                var violator = await _userManager.FindByIdAsync(violatorId.Value.ToString());
                if (violator != null)
                {
                    // Đếm xem người này (hoặc phòng của người này) đã từng bị report Approved bao nhiêu lần
                    var previousViolationsCount = await _unitOfWork.Repository<Report>().GetQueryable()
                        .CountAsync(r => r.Status == "Approved" &&
                                        (r.ReportedUserId == violator.Id ||
                                        (r.ReportedRoom != null && r.ReportedRoom.UserId == violator.Id)));

                    int totalViolations = previousViolationsCount + 1; // Cộng thêm lần hiện tại

                    if (totalViolations == 1)
                    {
                        // Vi phạm Lần 1: Gửi cảnh báo
                        var subject = "Cảnh báo vi phạm nội quy SacoStay";
                        var body = $"Chào {violator.FirstName},<br><br>" +
                                   $"Chúng tôi nhận được báo cáo vi phạm hợp lệ liên quan đến tài khoản hoặc bài đăng của bạn. " +
                                   $"Bài đăng vi phạm (nếu có) đã bị ẩn.<br><br>" +
                                   $"<b>Lưu ý:</b> Nếu bạn vi phạm thêm 1 lần nữa, tài khoản của bạn sẽ bị khóa vĩnh viễn.";

                        await _emailService.SendEmailAsync(violator.Email, subject, body);
                    }
                    else if (totalViolations >= 2)
                    {
                        // Vi phạm Lần 2: Ban vĩnh viễn bằng tính năng Lockout của Identity
                        await _userManager.SetLockoutEnabledAsync(violator, true);
                        await _userManager.SetLockoutEndDateAsync(violator, DateTimeOffset.MaxValue);

                        var subject = "Tài khoản của bạn đã bị khóa vĩnh viễn";
                        var body = $"Chào {violator.FirstName},<br><br>" +
                                   $"Tài khoản của bạn đã vi phạm nội quy hệ thống nhiều lần và đã bị khóa vĩnh viễn. " +
                                   $"Bạn sẽ không thể đăng nhập hoặc sử dụng dịch vụ của chúng tôi nữa.";

                        await _emailService.SendEmailAsync(violator.Email, subject, body);
                    }
                }
            }

            // 4. Lưu tất cả thay đổi (Report & RoomPost) vào Database
            _unitOfWork.Repository<Report>().Update(report);
            await _unitOfWork.CompleteAsync();

            return Ok(new { Message = "Đã duyệt báo cáo thành công và áp dụng hình phạt tương ứng." });
        }
    }
}
