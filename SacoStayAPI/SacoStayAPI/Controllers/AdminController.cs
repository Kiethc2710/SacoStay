using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
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
    }
}
