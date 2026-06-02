using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomApprovalController : ControllerBase
    {
        private readonly INotificationDispatcher _notificationDispatcher;
        private readonly IRoomPostService _roomPostService;

        public RoomApprovalController(INotificationDispatcher notificationDispatcher, IRoomPostService roomPostService)
        {
            _notificationDispatcher = notificationDispatcher;
            _roomPostService = roomPostService;
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpPost("{roomPostId}/approve")]
        public async Task<IActionResult> Approve(Guid roomPostId)
        {
            var room = await _roomPostService.UpdateRoomPostStatusAsync(roomPostId, Guid.Empty, "Active", null);
            await _notificationDispatcher.NotifyAsync(
                room.UserId,
                "Bài đăng đã được duyệt",
                $"Bài đăng '{room.Title}' của bạn đã được admin duyệt.",
                "system",
                $"/owner/my-posts?roomPostId={room.Id}"
            );
            return Ok(new { message = "Đã duyệt bài đăng." });
        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "admin")]
        [HttpPost("{roomPostId}/reject")]
        public async Task<IActionResult> Reject(Guid roomPostId, [FromQuery] string reason = "Bài đăng không phù hợp")
        {
            var room = await _roomPostService.UpdateRoomPostStatusAsync(roomPostId, Guid.Empty, "Rejected", null);
            await _notificationDispatcher.NotifyAsync(
                room.UserId,
                "Bài đăng bị từ chối",
                $"Bài đăng '{room.Title}' đã bị từ chối. Lý do: {reason}",
                "system",
                $"/owner/my-posts?roomPostId={room.Id}"
            );
            return Ok(new { message = "Đã từ chối bài đăng." });
        }
    }
}
