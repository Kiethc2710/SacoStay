using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LifestyleController : ControllerBase
    {
        private readonly LifestyleService _lifestyleService;

        public LifestyleController(LifestyleService lifestyleService)
        {
            _lifestyleService = lifestyleService;
        }

        [HttpPost("question")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDTO dto)
        {
            // Kiểm tra dữ liệu hợp lệ cơ bản
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest("Nội dung câu hỏi không được để trống.");
            }

            if (dto.Options == null || !dto.Options.Any())
            {
                return BadRequest("Cần có ít nhất một lựa chọn cho câu hỏi này.");
            }

            try
            {
                await _lifestyleService.CreateQuestionWithOptionsAsync(dto);
                return Ok(new { message = "Tạo câu hỏi và các lựa chọn thành công!" });
            }
            catch (Exception ex)
            {
                // Trong thực tế nên log lỗi này lại
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
        [HttpGet("questions")]
        public async Task<IActionResult> GetAllQuestions()
        {
            try
            {
                var questions = await _lifestyleService.GetAllQuestionsWithOptionsAsync();
                return Ok(questions);
            }
            catch (Exception ex)
            {
                // Trong thực tế nên log lỗi này lại
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("my-answers")]
        public async Task<IActionResult> GetMyAnswers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không hợp lệ.");

            var answers = await _lifestyleService.GetUserAnswersAsync(userId);
            return Ok(answers);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("answers/{userId}")]
        public async Task<IActionResult> GetUserAnswers(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("Token không hợp lệ.");
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("Thiếu userId.");

            var answers = await _lifestyleService.GetUserAnswersAsync(userId);
            return Ok(answers);
        }

        [Authorize(AuthenticationSchemes = "Bearer")] 
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitAnswers([FromBody] UserSubmitLifestyleDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Token không hợp lệ.");

            if (dto.SelectedOptionIds == null || !dto.SelectedOptionIds.Any())
                return BadRequest("Vui lòng chọn ít nhất 1 câu trả lời.");

            await _lifestyleService.SubmitUserAnswersAsync(userId, dto);
            return Ok(new { message = $"Lưu hồ sơ lối sống {userId} thành công! " });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("match/{targetUserId}")]
        public async Task<IActionResult> GetMatchingScore(string targetUserId)
        {
            // 1. Lấy ID của user đang đăng nhập từ Bearer Token
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("Token không hợp lệ hoặc không chứa thông tin User.");
            }

            // 2. Chặn trường hợp user "tự kỷ" - tự truyền ID của chính mình vào để so sánh
            if (currentUserId == targetUserId)
            {
                return BadRequest("Không thể tự tính điểm tương hợp với chính mình.");
            }

            try
            {
                // 3. Gọi Service để tính toán
                var result = await _lifestyleService.CalculateMatchingScoreAsync(currentUserId, targetUserId);

                // 4. Trả kết quả thành công (HTTP 200) về cho Frontend
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ thống
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("swipe-deck")]
        public async Task<IActionResult> GetSwipeDeck([FromQuery] int limit = 10, [FromQuery] bool includeSwiped = false)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("Token không hợp lệ.");

            try
            {
                // =================================================================
                // BƯỚC BẢO MẬT: KIỂM TRA LIMIT CÓ HỢP LỆ VỚI GÓI ĐÃ MUA KHÔNG
                // =================================================================

                // Giả sử bạn có 1 hàm trong Service hoặc DB để check gói của User
                // var userPlan = await _userService.GetUserPlanAsync(currentUserId);

                int maxAllowedLimit = 20; // Mặc định ai cũng là gói Free (10 thẻ)

                /* Mở comment đoạn này khi bạn có bảng lưu Gói User
                if (userPlan == "VIP") 
                {
                    maxAllowedLimit = 50; 
                }
                else if (userPlan == "SVIP") 
                {
                    maxAllowedLimit = 100; 
                }
                */

                // Nếu Frontend truyền limit lớn hơn số thẻ tối đa của gói nó mua
                // -> Ép cái limit đó về đúng số thẻ tối đa của gói đó
                if (limit > maxAllowedLimit)
                {
                    limit = maxAllowedLimit;
                }

                // =================================================================

                // Trả cái limit (đã được kiểm duyệt sạch sẽ) xuống cho Service
                var deck = await _lifestyleService.GetSwipeDeckAsync(currentUserId, limit, includeSwiped);
                return Ok(deck);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
        // API 2: LƯU HÀNH ĐỘNG QUẸT (Trái/Phải)
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("swipe")]
        public async Task<IActionResult> SwipeUser([FromQuery] string targetUserId, [FromQuery] bool isLike)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized("Token không hợp lệ.");
            if (string.IsNullOrEmpty(targetUserId)) return BadRequest("Thiếu ID người dùng.");

            try
            {
                await _lifestyleService.SaveSwipeActionAsync(currentUserId, targetUserId, isLike);
                return Ok(new { message = isLike ? "Đã quẹt PHẢI (Like)" : "Đã quẹt TRÁI (Pass)" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("my-likes")]
        public async Task<IActionResult> GetMyLikes()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("Token không hợp lệ.");

            var likes = await _lifestyleService.GetMyLikesAsync(currentUserId);
            return Ok(likes);
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpDelete("my-likes/{targetUserId}")]
        public async Task<IActionResult> RemoveLike(string targetUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("Token không hợp lệ.");

            if (string.IsNullOrWhiteSpace(targetUserId))
                return BadRequest("Thiếu ID người dùng.");

            var removed = await _lifestyleService.RemoveLikeAsync(currentUserId, targetUserId);
            if (!removed)
                return NotFound(new { message = "Không tìm thấy lượt thích cần xoá." });

            return Ok(new { message = "Đã xoá khỏi danh sách thích." });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("swipe-quota")]
        public async Task<IActionResult> GetSwipeQuota()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("Token không hợp lệ.");

            var quota = await _lifestyleService.GetSwipeQuotaAsync(currentUserId);
            return Ok(quota);
        }

        /// <summary>
        /// API 1: Chỉ cập nhật nội dung câu hỏi
        /// URL: PUT /api/lifestylequestions/content
        /// </summary>
        [HttpPut("question")]
        public async Task<IActionResult> UpdateQuestionOnly([FromBody] UpdateQuestionDTO dto)
        {
            // 1. Validate dữ liệu đầu vào
            if (dto == null)
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });

            if (dto.Id == null || dto.Id <= 0)
                return BadRequest(new { message = "Vui lòng cung cấp Id câu hỏi hợp lệ." });

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Nội dung câu hỏi không được để trống." });

            try
            {
                // 2. Gọi Service
                var updatedQuestion = await _lifestyleService.UpdateQuestionOnlyAsync(dto);
                return Ok(new
                {
                    message = "Cập nhật câu hỏi thành công!",
                    data = updatedQuestion
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                // Có thể ghi log lỗi ở đây
                return StatusCode(500, new { message = "Lỗi máy chủ: " + ex.Message });
            }
        }

        /// <summary>
        /// API 2: Cập nhật và thêm mới danh sách Câu trả lời (Options)
        /// URL: PUT /api/lifestylequestions/{id}/options
        /// </summary>
        [HttpPut("options")]
        public async Task<IActionResult> UpdateQuestionOptions(int questionId, [FromBody] List<UpdateOptionDTO> incomingOptions)
        {
            // 1. Validate dữ liệu đầu vào
            if (questionId <= 0)
                return BadRequest(new { message = "Id câu hỏi không hợp lệ." });

            if (incomingOptions == null || incomingOptions.Count == 0)
                return BadRequest(new { message = "Danh sách câu trả lời không được để trống." });

            try
            {
                // 2. Gọi Service
                await _lifestyleService.UpdateQuestionOptionsAsync( questionId, incomingOptions);
                return Ok(new { message = "Cập nhật danh sách câu trả lời thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (System.Exception ex)
            {
                // Có thể ghi log lỗi ở đây
                return StatusCode(500, new { message = "Lỗi máy chủ: " + ex.Message });
            }
        }

    } 
}
