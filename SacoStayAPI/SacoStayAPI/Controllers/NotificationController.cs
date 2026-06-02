using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var notifications = await _notificationService.GetMyNotificationsAsync(parsedUserId, page, pageSize);
            return Ok(notifications);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var count = await _notificationService.GetUnreadCountAsync(parsedUserId);
            return Ok(new { unreadCount = count });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPatch("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var result = await _notificationService.MarkAsReadAsync(parsedUserId, notificationId);
            if (!result) return NotFound(new { message = "Không tìm thấy notification." });

            return Ok(new { message = "Đã đánh dấu đã đọc." });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var count = await _notificationService.MarkAllAsReadAsync(parsedUserId);
            return Ok(new { message = "Đã đánh dấu tất cả là đã đọc.", updated = count });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpDelete("{notificationId}")]
        public async Task<IActionResult> Delete(Guid notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var result = await _notificationService.DeleteAsync(parsedUserId, notificationId);
            if (!result) return NotFound(new { message = "Không tìm thấy notification." });

            return Ok(new { message = "Đã xoá notification." });
        }
    }
}
