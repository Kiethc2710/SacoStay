using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Services;
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
        private readonly EmailService _emailService;
        private readonly IMemoryCache _cache;


        public AuthController(RoleManager<IdentityRole<Guid>> roleManager, 
                                UserManager<Account> userManager,
                                IConfiguration configuration,
                                IMemoryCache cache,
                                EmailService emailService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;
            _emailService = emailService;
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

        [Authorize(AuthenticationSchemes = "Bearer")]
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
        //Register API
        // Tạo tài khoản với các thông tin cần thiết như username, sdt, ngày tháng năm sinh, pwd
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            // 1. Validate dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
            }

            // 2. Kiểm tra username đã tồn tại chưa
            var existingUser = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Username đã tồn tại" });
            }

            // 3. Kiểm tra email đã tồn tại chưa
            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingEmail != null)
            {
                return BadRequest(new { message = "Email đã tồn tại" });
            }
            // 4. Kiểm tra số điện thoại đã tồn tại chưa (nếu có)
            var existingPhone = _userManager.Users
                    .FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber);
            if (existingPhone != null)
            {
                return BadRequest(new { message = "Số điện thoại đã tồn tại" });
            }
             // 5. Tạo user mới (để password hash do UserManager xử lý)
                var user = new Account
            {
                UserName = dto.UserName,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            { 
                // 5. Gán role Customer mặc định
                if (!await _roleManager.RoleExistsAsync("tenants"))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>("tenants"));
                }
                await _userManager.AddToRoleAsync(user, "tenants");
                // 1. Generate token của Identity
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // 2. Tạo OTP 6 số
                var otp = new Random().Next(100000, 999999).ToString();

                // 3. Lưu mapping OTP -> token vào cache (5 phút)
                _cache.Set(
                    $"email_confirm_{user.Email}",
                    new { Otp = otp, Token = token },
                    TimeSpan.FromMinutes(5)
                );

                // 4. Gửi OTP qua email
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Xác nhận email SacoStay",
                    $"Mã OTP của bạn là: <b>{otp}</b>. Hết hạn sau 5 phút."
                );

                return Ok(new
                {
                    message = "Đăng ký thành công. Vui lòng nhập OTP gửi về email."
                });

                return Ok(new { message = "Registration successful! Please check your email for confirmation." });

            }

            // Trả lỗi nếu tạo user thất bại
            return BadRequest(result.Errors);
        }
        //Xca nhận email bằng OTP
        [HttpPost("verify-email-otp")]
        public async Task<IActionResult> VerifyEmailOtp(string email, string otp)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return BadRequest("Email không tồn tại");

            if (!_cache.TryGetValue($"email_confirm_{email}", out dynamic data))
                return BadRequest("OTP đã hết hạn");

            if (data.Otp != otp)
                return BadRequest("OTP không đúng");

            var result = await _userManager.ConfirmEmailAsync(user, data.Token);

            if (!result.Succeeded)
                return BadRequest("Xác nhận thất bại");

            _cache.Remove($"email_confirm_{email}");

            return Ok("Xác nhận email thành công");
        }

    }
}
