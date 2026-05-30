using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Data;
using System.IdentityModel.Tokens.Jwt;
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
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var currentUserId = Guid.Parse(userIdStr);

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        m.SenderId,
                        m.Message,
                        m.SentAt
                    })
                .ToListAsync();

            return Ok(messages);
        }

        /// <summary>Danh sách người đã từng nhắn tin (để hiển thị hội thoại, không phụ thuộc localStorage).</summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            if (!Guid.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var messages = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var seen = new HashSet<Guid>();
            var conversations = new List<object>();

            foreach (var m in messages)
            {
                var otherId = m.SenderId == currentUserId ? m.ReceiverId : m.SenderId;
                if (!seen.Add(otherId)) continue;
                conversations.Add(new
                {
                    otherUserId = otherId.ToString(),
                    lastMessage = m.Message,
                    lastSentAt = m.SentAt
                });
            }

            return Ok(conversations);
        }
    }
}
