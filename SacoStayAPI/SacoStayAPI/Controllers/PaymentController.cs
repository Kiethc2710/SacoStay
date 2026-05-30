using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Service;
using System;
using System.IO;
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
        [HttpPost("buy-landlord-package")]
        public async Task<IActionResult> BuyLandlordPackage([FromBody] BuyLandlordPackageDTO dto)
        {
            if (dto.RoomPostId == Guid.Empty)
                return BadRequest(new { message = "Thiếu RoomPostId." });

            if (string.IsNullOrWhiteSpace(dto.PackageName))
                return BadRequest(new { message = "Thiếu tên gói." });

            try
            {
                var url = await _service.CreatePackagePaymentUrlAsync(dto.RoomPostId, dto.PackageName);
                return Ok(new { paymentUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("buy-tenant-package")]
        public async Task<IActionResult> BuyTenantPackage([FromBody] BuyTenantPackageDTO dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            if (string.IsNullOrWhiteSpace(dto.PackageName))
                return BadRequest(new { message = "Thiếu tên gói." });

            try
            {
                var url = await _service.CreateTenantPackagePaymentUrlAsync(parsedUserId, dto.PackageName);
                return Ok(new { paymentUrl = url });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("payos-return")]
        public async Task<IActionResult> PayOSReturn()
        {
            await _service.HandleReturnAsync(Request.Query);
            var redirectUrl = await _service.BuildFrontendReturnUrlAsync(Request.Query);
            return Redirect(redirectUrl);
        }

        [HttpPost("payos-webhook")]
        public async Task<IActionResult> PayOSWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            await _service.HandleWebhookAsync(payload);
            return Ok(new { message = "OK" });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("history")]
        public async Task<IActionResult> GetTransactionHistory()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                return Unauthorized(new { message = "Token không hợp lệ." });

            var history = await _service.GetTransactionHistoryAsync(parsedUserId);
            return Ok(history);
        }
    }
}
