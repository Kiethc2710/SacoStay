using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SacoStayAPI.Hubs
{
    /// <summary>
    /// Map JWT claim "sub" → SignalR User identifier (dùng cho Clients.User).
    /// </summary>
    public class SignalRUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection) =>
            connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? connection.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }
}
