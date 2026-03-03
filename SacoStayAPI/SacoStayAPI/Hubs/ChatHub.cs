using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SacoStayAPI.Data;
using SacoStayAPI.Model.Entities;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDBContext _context;

    public ChatHub(ApplicationDBContext context) => _context = context;

    public async Task SendPrivateMessage(string receiverId, string message)
    {
        // Lấy ID người gửi từ Token JWT (đã cấu hình trong Program.cs)
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(message)) return;

        // 1. Lưu vào Database (Để sau này lấy Dữ liệu cũ)
        var chatMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(senderId),
            ReceiverId = Guid.Parse(receiverId),
            Message = message,
            SentAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMsg);
        await _context.SaveChangesAsync();

        // 2. Gửi Realtime (Dữ liệu mới)
        // Gửi cho người nhận
        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        // Gửi ngược lại cho chính mình để đồng bộ tin nhắn trên các tab khác nhau
        await Clients.Caller.SendAsync("ReceiveMessage", senderId, message);
    }
}