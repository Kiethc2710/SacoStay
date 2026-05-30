using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using SacoStayAPI.Data;
using SacoStayAPI.Model.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly ApplicationDBContext _context;

        public ChatHub(ApplicationDBContext context) => _context = context;

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(senderId) || string.IsNullOrWhiteSpace(message)) return;

            var chatMsg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Parse(senderId),
                ReceiverId = Guid.Parse(receiverId),
                Message = message.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message.Trim());
            await Clients.Caller.SendAsync("ReceiveMessage", senderId, message.Trim());
        }
    }
}
