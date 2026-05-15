using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;

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
    }
}
