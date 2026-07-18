using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Service; // Thêm để nhận diện IPhotoService
using SacoStayAPI.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IPhotoService _photoService; // Khai báo PhotoService

        public AuthController(RoleManager<IdentityRole<Guid>> roleManager,
                                UserManager<Account> userManager,
                                IConfiguration configuration,
                                IMemoryCache cache,
                                EmailService emailService,
                                IPhotoService photoService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _configuration = configuration;
            _cache = cache;
            _emailService = emailService;
            _photoService = photoService;
        }

        /// <summary>FE gửi tenant/landlord — DB seed dùng tenants.</summary>
        private static string NormalizeRegisterRole(string? role)
        {
            var r = (role ?? string.Empty).Trim().ToLowerInvariant();
            if (r == "tenant") return "tenants";
            if (r == "landlord" || r == "tenants" || r == "admin") return r;
            return "tenants";
        }

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
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
                claims.Add(new Claim(ClaimTypes.Role, role));
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

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        //{
        //    Account user = null;
        //    if (dto.EmailPhoneorUsername.Contains("@"))
        //        user = await _userManager.FindByEmailAsync(dto.EmailPhoneorUsername);
        //    else if (dto.EmailPhoneorUsername.All(char.IsDigit))
        //    {
        //        user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == dto.EmailPhoneorUsername);
        //    }
        //    else
        //    {
        //        user = await _userManager.FindByNameAsync(dto.EmailPhoneorUsername);
        //    }
        //    if (user == null) return Unauthorized("Invalid username/email");

        //    var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
        //    if (!validPassword) return Unauthorized("Invalid password");

        //    if (!await _userManager.IsEmailConfirmedAsync(user))
        //        return Unauthorized("Email chưa được xác nhận");

        //    var token = await GenerateJwtToken(user);
        //    return Ok(new { token });
        //}
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            Account user = null;
            if (dto.EmailPhoneorUsername.Contains("@"))
                user = await _userManager.FindByEmailAsync(dto.EmailPhoneorUsername);
            else if (dto.EmailPhoneorUsername.All(char.IsDigit))
            {
                user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == dto.EmailPhoneorUsername);
            }
            else
            {
                user = await _userManager.FindByNameAsync(dto.EmailPhoneorUsername);
            }

            // 1. Kiểm tra tài khoản tồn tại không
            if (user == null) return Unauthorized("Invalid username/email");

            // 2. KIỂM TRA MẬT KHẨU TRƯỚC (Như bạn nói, phải đúng pass đã mới xử tiếp)
            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword) return Unauthorized("Invalid password");

            // 3. MẬT KHẨU ĐÚNG RỒI -> BÂY GIỜ MỚI CHECK XEM CÓ BỊ BAN KHÔNG
            if (await _userManager.IsLockedOutAsync(user))
            {
                return BadRequest(new { Message = "Tài khoản của bạn đã bị khóa vĩnh viễn do vi phạm nội quy." });
            }

            // 4. Kiểm tra các bước phụ khác
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized("Email chưa được xác nhận");

            // 5. Cấp token vào nhà
            var token = await GenerateJwtToken(user);
            return Ok(new { token });
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.FirstName,
                user.LastName,
                user.Gender,
                user.Job,
                user.LivingArea,
                user.DateOfBirth,
                user.Bio,
                user.IsVerified,
                ProfileImage = user.ProfileImages,
                Roles = roles
            });
        }

        /// <summary>Hồ sơ công khai — discovery / chat (không trả email, chỉ trả phone khi là landlord).</summary>
        [AllowAnonymous]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPublicProfile(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var isLandlord = roles.Contains("landlord", StringComparer.OrdinalIgnoreCase);

            var response = new
            {
                user.Id,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.Gender,
                user.Job,
                user.LivingArea,
                user.DateOfBirth,
                user.Bio,
                ProfileImage = user.ProfileImages,
                Roles = roles
            };

            if (isLandlord)
            {
                return Ok(new
                {
                    response.Id,
                    response.UserName,
                    response.FirstName,
                    response.LastName,
                    response.Gender,
                    response.Job,
                    response.LivingArea,
                    response.DateOfBirth,
                    response.Bio,
                    response.ProfileImage,
                    response.Roles,
                    PhoneNumber = user.PhoneNumber
                });
            }

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
            }

            var existingUser = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUser != null) return BadRequest(new { message = "Username đã tồn tại" });

            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingEmail != null) return BadRequest(new { message = "Email đã tồn tại" });

            var existingPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber);
            if (existingPhone != null) return BadRequest(new { message = "Số điện thoại đã tồn tại" });

            var roleName = NormalizeRegisterRole(dto.Role);

            // Landlord bắt buộc nhập PhoneNumber
            if (roleName.Equals("landlord", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                return BadRequest(new { message = "Chủ trọ bắt buộc phải nhập số điện thoại." });
            }

            var user = new Account
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }

                await _userManager.AddToRoleAsync(user, roleName);

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var otp = new Random().Next(100000, 999999).ToString();

                _cache.Set($"email_confirm_{user.Email}", new { Otp = otp, Token = token }, TimeSpan.FromMinutes(5));

                await _emailService.SendEmailAsync(user.Email!, "Xác nhận email SacoStay", $"Mã OTP của bạn là: <b>{otp}</b>. Hết hạn sau 5 phút.");

                return Ok(new { message = "Đăng ký thành công. Vui lòng nhập OTP gửi về email." });
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Email)) return BadRequest("Email không hợp lệ");
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Email không tồn tại");
            if (await _userManager.IsEmailConfirmedAsync(user)) return BadRequest("Email đã được xác nhận");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var otp = new Random().Next(100000, 999999).ToString();

            _cache.Set($"email_confirm_{user.Email}", new { Otp = otp, Token = token }, TimeSpan.FromMinutes(5));

            await _emailService.SendEmailAsync(user.Email!, "Gửi lại mã OTP xác nhận email", $"Mã OTP của bạn là: <b>{otp}</b>. Hết hạn sau 5 phút.");
            return Ok("Đã gửi lại OTP mới");
        }

        [HttpPost("verify-email-otp")]
        public async Task<IActionResult> VerifyEmailOtp(string email, string otp)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return BadRequest("Email không tồn tại");

            if (!_cache.TryGetValue($"email_confirm_{email}", out dynamic data)) return BadRequest("OTP đã hết hạn");
            if (data.Otp != otp) return BadRequest("OTP không đúng");

            var result = await _userManager.ConfirmEmailAsync(user, data.Token);
            if (!result.Succeeded) return BadRequest("Xác nhận thất bại");

            _cache.Remove($"email_confirm_{email}");
            return Ok("Xác nhận email thành công");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ResendOtpDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Ok("Nếu email tồn tại, chúng tôi đã gửi mã OTP.");
            if (!await _userManager.IsEmailConfirmedAsync(user)) return BadRequest("Email chưa được xác nhận.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var otp = new Random().Next(100000, 999999).ToString();

            _cache.Set($"reset_pwd_{dto.Email}", new OtpCacheModel { Otp = otp, Token = token }, TimeSpan.FromMinutes(5));

            await _emailService.SendEmailAsync(dto.Email, "Reset mật khẩu", $"Mã OTP của bạn là: <b>{otp}</b>");
            return Ok("Nếu email tồn tại, chúng tôi đã gửi mã OTP.");
        }

        [HttpPost("verify-reset-otp")]
        public IActionResult VerifyResetOtp([FromBody] VerifyOtpDTO dto)
        {
            if (!_cache.TryGetValue($"reset_pwd_{dto.Email}", out OtpCacheModel data)) return BadRequest("OTP đã hết hạn");
            if (data.Otp != dto.Otp) return BadRequest("OTP không đúng");

            _cache.Set($"reset_verified_{dto.Email}", true, TimeSpan.FromMinutes(5));
            return Ok("OTP hợp lệ");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Email không hợp lệ");

            if (!_cache.TryGetValue($"reset_verified_{dto.Email}", out bool verified) || !verified) return BadRequest("Bạn chưa xác thực OTP");
            if (!_cache.TryGetValue($"reset_pwd_{dto.Email}", out OtpCacheModel data)) return BadRequest("Phiên reset không hợp lệ");

            var result = await _userManager.ResetPasswordAsync(user, data.Token, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            _cache.Remove($"reset_pwd_{dto.Email}");
            _cache.Remove($"reset_verified_{dto.Email}");
            return Ok("Reset mật khẩu thành công");
        }
         
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPost("upload-profile-images")]
        public async Task<IActionResult> UploadProfileImages([FromForm] List<IFormFile> files)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Người dùng không tồn tại");

            if (files == null || files.Count == 0) return BadRequest("Không có file nào được chọn");

            // Khởi tạo mảng nếu bị null
            if (user.ProfileImages == null) user.ProfileImages = new List<string>();

            if (user.ProfileImages.Count + files.Count > 5)
            {
                return BadRequest($"Bạn chỉ có thể thêm tối đa 5 ảnh. Hiện tại bạn đã có {user.ProfileImages.Count} ảnh.");
            }

            // Duyệt qua từng file và upload lên S3
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var imageUrl = await _photoService.UploadPhotoAsync(file, "users/profiles");
                    user.ProfileImages.Add(imageUrl);
                }
            }

            // Cập nhật thông tin vào DB
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { message = "Tải ảnh lên thành công", profileImages = user.ProfileImages });
            }

            return BadRequest(result.Errors);
        }
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpDelete("delete-profile-image")]
        public async Task<IActionResult> DeleteProfileImage([FromQuery] string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return BadRequest("Đường dẫn ảnh không hợp lệ");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Người dùng không tồn tại");

            if (user.ProfileImages == null || !user.ProfileImages.Contains(imageUrl))
            {
                return BadRequest("Ảnh không tồn tại trong hồ sơ của bạn");
            }

            // 1. Xóa ảnh vật lý trên S3
            var deleteSuccess = await _photoService.DeletePhotoAsync(imageUrl);

            // 2. Xóa URL khỏi danh sách quản lý của User trong Database
            user.ProfileImages.Remove(imageUrl);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new { message = "Xóa ảnh thành công", profileImages = user.ProfileImages });
            }

            return BadRequest(result.Errors);
        }
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UserProfileDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Người dùng không tồn tại");

            if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
            {
                try
                {
                    var avatarUrl = await _photoService.UploadPhotoAsync(dto.AvatarFile, "users/avatars");

                    if (user.ProfileImages == null) user.ProfileImages = new List<string>();

                    user.ProfileImages.Clear(); // Xóa avatar cũ, chỉ giữ lại 1 ảnh duy nhất làm diện mạo mới
                    user.ProfileImages.Add(avatarUrl);
                }
                catch (Exception)
                {
                    return StatusCode(500, "Lỗi hệ thống khi tải ảnh đại diện lên đám mây S3.");
                }
            }
            if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrEmpty(dto.Job)) user.Job = dto.Job;
            if (!string.IsNullOrEmpty(dto.LivingArea)) user.LivingArea = dto.LivingArea;
            if (!string.IsNullOrEmpty(dto.Bio)) user.Bio = dto.Bio;

            if (dto.Gender.HasValue)
            {
                user.Gender = dto.Gender.Value;
            }

            if (dto.DateOfBirth.HasValue)
            {
                user.DateOfBirth = dto.DateOfBirth.Value;
            }
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = "Cập nhật hồ sơ thành công!",
                    profileImages = user.ProfileImages,
                    dateOfBirth = user.DateOfBirth.ToString("yyyy-MM-dd") 
                });
            }

            return BadRequest(result.Errors);
        }
    }
}