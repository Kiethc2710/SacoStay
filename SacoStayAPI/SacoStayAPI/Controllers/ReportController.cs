using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromForm] CreateReportRequest request)
        {
            try
            {
                // Trong thực tế, ReporterId thường được lấy từ JWT Token (User.Identity)
                // request.ReporterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var success = await _reportService.SubmitReportAsync(request);

                if (success)
                {
                    return Ok(new { Message = "Gửi report thành công. Quản trị viên sẽ xem xét." });
                    Console.WriteLine($"SỐ ẢNH NHẬN ĐƯỢC: {request.Images?.Count ?? 0}");
                }

                return BadRequest(new { Message = "Không thể lưu report." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log lỗi ở đây
                return StatusCode(500, new { Message = "Đã xảy ra lỗi hệ thống." });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetListReport() // Nhớ thêm dấu () nhé
        {
            try
            {
                var reports = await _reportService.GetListReportsAsync();

                if (reports == null || !reports.Any())
                {
                    return Ok(new { Message = "Chưa có báo cáo nào.", Data = new List<object>() });
                }

                return Ok(new { Message = "Lấy danh sách thành công", Data = reports });
            }
            catch (Exception ex)
            {
                // Log lỗi ex.Message ở đây nếu hệ thống có thư viện log
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi lấy danh sách báo cáo." });
            }
        }
    }
}
