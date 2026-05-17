using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Service;
using System;
using System.Threading.Tasks;

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _service;

        public PaymentController(IPaymentService service)
        {
            _service = service;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("buy-package")]
        public async Task<IActionResult> BuyPackage([FromQuery] Guid roomPostId, [FromQuery] string packageName)
        {
            try
            {
                // Gọi dịch vụ tính tiền theo gói và sinh link VNPay theo thuật toán cũ
                var url = await _service.CreatePackagePaymentUrlAsync(roomPostId, packageName);
                return Ok(new { paymentUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            await _service.HandleReturnAsync(Request.Query);

            // Điều hướng về giao diện Frontend sau khi thanh toán xong
            return Redirect("http://localhost:4200/owner/my-posts?payment=completed");
        }
    }
}