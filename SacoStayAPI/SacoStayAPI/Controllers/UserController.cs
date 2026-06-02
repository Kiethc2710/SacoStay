using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserProfileService _profileService;

        public UserController(IUserProfileService profileService)
        {
            _profileService = profileService;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("profile-images")]
        public async Task<IActionResult> GetProfileImages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var images = await _profileService.GetProfileImagesAsync(parsedUserId);
            return Ok(images);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("profile-images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfileImages([FromForm] UploadProfileImageDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            try
            {
                var uploaded = await _profileService.UploadProfileImagesAsync(parsedUserId, dto.Files);
                return Ok(new
                {
                    message = "Upload ảnh profile thành công.",
                    images = uploaded
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpDelete("profile-images")]
        public async Task<IActionResult> DeleteProfileImage([FromQuery] string imageUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            if (string.IsNullOrWhiteSpace(imageUrl))
                return BadRequest(new { message = "Thiếu imageUrl." });

            try
            {
                var deleted = await _profileService.DeleteProfileImageAsync(parsedUserId, imageUrl);
                if (!deleted)
                    return NotFound(new { message = "Không tìm thấy ảnh cần xoá." });

                return Ok(new { message = "Xoá ảnh profile thành công." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
