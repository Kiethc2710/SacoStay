using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Service;

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

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment(decimal amount)
        {
            var url = await _service.CreatePayment(amount);
            return Ok(url);
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            await _service.HandleReturnAsync(Request.Query);
            return Ok("Payment processed");
        }
    }
}
