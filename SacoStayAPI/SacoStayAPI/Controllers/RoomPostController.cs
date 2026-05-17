using Amazon.S3.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomPostController : ControllerBase
    {
        private readonly IRoomPostService _roomPostService;

        public RoomPostController(IRoomPostService roomPostService)
        {
            _roomPostService = roomPostService;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePost([FromForm] CreateRoomPostDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _roomPostService.CreatePostAsync(dto, Guid.Parse(userId));
                return Ok(new { message = "Đăng tin phòng trọ thành công!", data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi hệ thống khi đăng tin.",
                    errorDetail = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("my-posts")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _roomPostService.GetMyPostsAsync(Guid.Parse(userId));
            return Ok(result);
        }

        [HttpGet("search-nearby")]
        public async Task<IActionResult> GetRoomsNearby([FromQuery] double userLat, [FromQuery] double userLng, [FromQuery] double radiusInKm = 3.0)
        {
            var result = await _roomPostService.GetRoomsNearbyAsync(userLat, userLng, radiusInKm);
            return Ok(result);
        }
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("{id}/analytics")]
        public async Task<IActionResult> GetRoomAnalytics(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _roomPostService.GetRoomAnalyticsAsync(id, Guid.Parse(userId));
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
        }

        // API xem chi tiết tin đăng trọ của khách hàng -> Tự động kích hoạt ghi nhận lịch sử xem tin
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("{id}/view")]
        public async Task<IActionResult> TriggerViewRoom(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _roomPostService.RecordViewAsync(id, Guid.Parse(userId));
            return Ok();
        }
    }
}