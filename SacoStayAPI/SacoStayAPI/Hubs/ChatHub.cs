using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using SacoStayAPI.Data;
using SacoStayAPI.Model.Entities;
using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly ApplicationDBContext _context;
        private readonly INotificationDispatcher _notificationDispatcher;

        public ChatHub(ApplicationDBContext context, INotificationDispatcher notificationDispatcher)
        {
            _context = context;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            var senderIdRaw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(senderIdRaw) || string.IsNullOrWhiteSpace(message)) return;
            if (!Guid.TryParse(senderIdRaw, out var senderGuid) || !Guid.TryParse(receiverId, out var receiverGuid))
                throw new HubException("Người nhận hoặc phiên đăng nhập không hợp lệ.");

            var trimmed = message.Trim();
            var senderKey = senderGuid.ToString();
            var receiverKey = receiverGuid.ToString();

            var chatMsg = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = senderGuid,
                ReceiverId = receiverGuid,
                Message = trimmed,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            await Clients.User(receiverKey).SendAsync("ReceiveMessage", senderKey, trimmed);
            await Clients.Caller.SendAsync("ReceiveMessage", senderKey, trimmed);

            var sender = await _context.Accounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == senderGuid);
            var senderName = BuildDisplayName(sender) ?? "Người dùng";
            var title = "Tin nhắn mới";
            var messagePreview = trimmed.Length > 80 ? trimmed[..80] + "..." : trimmed;
            await _notificationDispatcher.NotifyAsync(
                receiverGuid,
                title,
                $"{senderName}: {messagePreview}",
                "chat",
                $"/chat?with={senderKey}"
            );
        }

        private static string? BuildDisplayName(Account? account)
        {
            if (account == null) return null;
            var full = $"{account.FirstName} {account.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(full)) return full;
            if (!string.IsNullOrWhiteSpace(account.UserName)) return account.UserName;
            return null;
        }
    }
}
