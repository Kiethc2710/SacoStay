using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using SacoStayAPI.Services;
using System.IdentityModel.Tokens.Jwt; // ✨ Đã thêm thư viện này để đọc JwtRegisteredClaimNames giống hệt KYC
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")] // ✨ Đã đồng bộ bắt buộc đăng nhập JWT theo chuẩn Bearer
    public class SharedSpaceController : ControllerBase
    {
        private readonly ISharedSpaceService _spaceService;

        public SharedSpaceController(ISharedSpaceService spaceService)
        {
            _spaceService = spaceService;
        }

        // =========================================================================
        // HÀM HELPER: Lấy ID chuẩn theo cấu trúc Token của SacoStay Auth 
        // =========================================================================
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                // Ném ngoại lệ để tầng try-catch ở các Action bắt được và trả về Unauthorized (401)
                throw new UnauthorizedAccessException("Không xác định được người dùng hoặc token không hợp lệ.");
            }

            return userId;
        }
        // Endpoint: POST /api/shared-space/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateSpace([FromBody] CreateSpaceDTO dto)
        {
            // CreateSpaceDTO chỉ cần chứa 1 trường duy nhất là TargetUserId (ID của đứa vừa match)
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var currentUserId = GetCurrentUserId(); // Lấy ID của thằng đang gọi từ JWT

                var result = await _spaceService.CreateSharedSpaceAsync(currentUserId, dto.TargetUserId);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message, spaceId = result.SpaceId });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống nội bộ.", error = ex.Message });
            }
        }
        // =========================================================================
        // TASK 2: GET /api/shared-space/current
        // =========================================================================
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentSpace()
        {
            try
            {
                var userId = GetCurrentUserId();
                var data = await _spaceService.GetCurrentSpaceAsync(userId);

                if (data == null)
                    return NotFound(new { message = "Bạn hiện không ở trong không gian tìm trọ chung nào đang hoạt động." });

                return Ok(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống nội bộ.", error = ex.Message });
            }
        }

        // =========================================================================
        // TASK 3: POST /api/shared-space/{spaceId}/shortlist
        // =========================================================================
        [HttpPost("{spaceId}/shortlist")]
        public async Task<IActionResult> AddToShortlist(Guid spaceId, [FromBody] AddToShortlistDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var result = await _spaceService.AddToShortlistAsync(userId, spaceId, dto);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống nội bộ.", error = ex.Message });
            }
        }

        // =========================================================================
        // TASK 4: POST /api/shared-space/shortlist/{shortlistId}/vote
        // =========================================================================
        [HttpPost("shortlist/{shortlistId}/vote")]
        public async Task<IActionResult> VoteRoom(Guid shortlistId, [FromBody] VoteRoomDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var result = await _spaceService.VoteRoomAsync(userId, shortlistId, dto);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Message });

                return Ok(new { message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống nội bộ.", error = ex.Message });
            }
        }

        // =========================================================================
        // TASK 5: PUT /api/shared-space/{spaceId}/finalize
        // =========================================================================
        //[HttpPut("{spaceId}/finalize")]
        //public async Task<IActionResult> FinalizeSpace(Guid spaceId, [FromBody] FinalizeSpaceDTO dto)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    try
        //    {
        //        var userId = GetCurrentUserId();
        //        var result = await _spaceService.FinalizeSpaceAsync(userId, spaceId, dto);

        //        if (!result.IsSuccess)
        //            return BadRequest(new { message = result.Message });

        //        return Ok(new { message = result.Message });
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        return Unauthorized(new { message = ex.Message });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Lỗi hệ thống nội bộ.", error = ex.Message });
        //    }
        //}
        // =========================================================================
        // 1. Gửi đề xuất chốt phòng (Bên A gọi)
        // PUT: /api/shared-space/{spaceId}/propose-finalize
        // =========================================================================
        [HttpPut("{spaceId}/propose-finalize")]
        public async Task<IActionResult> ProposeFinalize(Guid spaceId, [FromBody] FinalizeSpaceDTO dto)
        {
            try
            {
                var result = await _spaceService.ProposeFinalizeAsync(GetCurrentUserId(), spaceId, dto);
                return result.IsSuccess ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // =========================================================================
        // 2. Chấp nhận chốt phòng (Bên B gọi)
        // PUT: /api/shared-space/{spaceId}/accept-finalize
        // =========================================================================
        [HttpPut("{spaceId}/accept-finalize")]
        public async Task<IActionResult> AcceptFinalize(Guid spaceId)
        {
            try
            {
                var result = await _spaceService.AcceptFinalizeAsync(GetCurrentUserId(), spaceId);
                return result.IsSuccess ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // =========================================================================
        // 3. Từ chối đề xuất chốt phòng (Bên B hoặc Bên A gọi)
        // PUT: /api/shared-space/{spaceId}/reject-finalize
        // =========================================================================
        [HttpPut("{spaceId}/reject-finalize")]
        public async Task<IActionResult> RejectFinalize(Guid spaceId)
        {
            try
            {
                var result = await _spaceService.RejectFinalizeAsync(GetCurrentUserId(), spaceId);
                return result.IsSuccess ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}