using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using SacoStayAPI.Services;
using System.IdentityModel.Tokens.Jwt; // Thêm thư viện này để đọc JwtRegisteredClaimNames
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")] // Bắt buộc đăng nhập JWT
    public class KycController : ControllerBase
    {
        private readonly IKycService _kycService;

        public KycController(IKycService kycService)
        {
            _kycService = kycService;
        }

        /// <summary>
        /// API nhận ảnh CCCD, Selfie từ Client để tự động xác minh qua FPT.AI
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitKyc([FromForm] SubmitKycRequestDTO dto)
        {
            // Lấy ID chuẩn theo cấu trúc Token của SacoStay Auth
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng hoặc token không hợp lệ." });
            }

            // Đẩy xuống tầng Service để tự động hóa bằng FPT.AI
            var result = await _kycService.SubmitKycAsync(userId, dto);

            if (result.IsSuccess)
            {
                return Ok(new { message = result.Message });
            }

            return BadRequest(new { message = result.Message });
        }

        /// <summary>
        /// API lấy trạng thái định danh hiện tại của User (Đã duyệt, Từ chối, hay Chưa nộp)
        /// </summary>
        [HttpGet("my-status")]
        public async Task<IActionResult> GetMyKycStatus()
        {
            // Lấy ID chuẩn theo cấu trúc Token của SacoStay Auth
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng hoặc phiên đăng nhập hết hạn." });
            }

            // Gọi Service lấy data hồ sơ mới nhất trong Database
            var statusData = await _kycService.GetUserKycStatusAsync(userId);

            // Nếu chưa từng nộp hồ sơ nào thì trả về trạng thái mặc định để FE xử lý giao diện
            if (statusData == null)
            {
                return Ok(new { status = "NotSubmitted" });
            }

            return Ok(statusData);
        }
    }
}