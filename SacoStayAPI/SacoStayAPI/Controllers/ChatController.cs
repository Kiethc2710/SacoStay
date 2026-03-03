using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Data;
using System.Security.Claims;

namespace SacoStayAPI.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        public ChatController(ApplicationDBContext context) => _context = context;

        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(Guid otherUserId)
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                    .Select(m => new {
                        m.SenderId,
                        m.Message,
                        m.SentAt
                    })
                .ToListAsync();

            return Ok(messages);
        }
    }
}
