using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class TenantRoomProfileController : ControllerBase
    {
        private readonly ITenantRoomProfileService _tenantRoomProfileService;

        public TenantRoomProfileController(ITenantRoomProfileService tenantRoomProfileService)
        {
            _tenantRoomProfileService = tenantRoomProfileService;
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException("Token không hợp lệ.");
        }

        /// <summary>
        /// Lấy thông tin phòng của user hiện tại
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = GetUserId();
                var profile = await _tenantRoomProfileService.GetByUserIdAsync(userId);
                if (profile == null)
                {
                    return NotFound(new { message = "Bạn chưa có thông tin phòng. Vui lòng tạo mới." });
                }
                return Ok(profile);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin phòng theo userId (Discovery / xem hồ sơ người khác)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{targetUserId:guid}")]
        public async Task<IActionResult> GetProfileByUserId(Guid targetUserId)
        {
            try
            {
                var profile = await _tenantRoomProfileService.GetByUserIdAsync(targetUserId.ToString());
                if (profile == null)
                {
                    return NotFound(new { message = "Người dùng chưa có thông tin phòng." });
                }
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới thông tin phòng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] CreateTenantRoomProfileDTO dto)
        {
            try
            {
                var userId = GetUserId();
                var profile = await _tenantRoomProfileService.CreateAsync(userId, dto);
                return Ok(new
                {
                    message = "Tạo thông tin phòng thành công!",
                    data = profile
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin phòng
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateTenantRoomProfileDTO dto)
        {
            try
            {
                var userId = GetUserId();
                var profile = await _tenantRoomProfileService.UpdateAsync(userId, dto);
                return Ok(new
                {
                    message = "Cập nhật thông tin phòng thành công!",
                    data = profile
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Upload ảnh phòng (tối đa 10 ảnh)
        /// </summary>
        [HttpPost("images")]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            try
            {
                var userId = GetUserId();
                var profile = await _tenantRoomProfileService.UploadImagesAsync(userId, files);
                return Ok(new
                {
                    message = "Upload ảnh thành công!",
                    data = profile
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Xóa ảnh phòng
        /// </summary>
        [HttpDelete("images")]
        public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return BadRequest(new { message = "Vui lòng cung cấp URL ảnh cần xóa." });
                }

                var userId = GetUserId();
                var profile = await _tenantRoomProfileService.DeleteImageAsync(userId, imageUrl);
                return Ok(new
                {
                    message = "Xóa ảnh thành công!",
                    data = profile
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
