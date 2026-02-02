using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SacoStayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<Account> _userManager;
        private readonly IConfiguration _configuration;
        public AuthController(RoleManager<IdentityRole<Guid>> roleManager, 
                                UserManager<Account> userManager,
                                IConfiguration configuration)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _configuration = configuration;
        }
        //hàm tao jwt token
        private async Task<string> GenerateJwtToken(Account user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("phone", user.PhoneNumber ?? ""),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            };
            //gen jwt thêm phần key:value của role nè, tí decode ra mới có role để phân quyền
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            Account user = null;
            if (dto.EmailPhoneorUsername.Contains("@"))
                user = await _userManager.FindByEmailAsync(dto.EmailPhoneorUsername);
            else if (dto.EmailPhoneorUsername.All(char.IsDigit))
            {
                // Login bằng số điện thoại
                user = _userManager.Users
                    .FirstOrDefault(u => u.PhoneNumber == dto.EmailPhoneorUsername);
            }else { 
                user = await _userManager.FindByNameAsync(dto.EmailPhoneorUsername);
            }
            if (user == null)
                return Unauthorized("Invalid username/email");

            // Kiểm tra password 
            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);

            // Nếu password sai
            if (!validPassword)
                return Unauthorized("Invalid password");

            // Nếu mọi thứ hợp lệ, login thành công
            var token = await GenerateJwtToken(user);
            return Ok(new { token });
        }
        //test auth

        [Authorize]
        [HttpGet("profile")]

        public async Task<IActionResult> GetProfile()
        {
            // Lấy userId từ JWT (claim sub)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                Roles = roles
            });
        }
    }
}
