using API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    /// <summary>
    /// Controller đăng nhập.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: https://localhost:7037/api/Login
        /// <summary>
        /// Đăng nhập vào hệ thống.
        /// </summary>
        /// <param name="loginUser">Thông tin đăng nhập bao gồm email và mật khẩu.</param>
        /// <returns>Trả về JWT token nếu đăng nhập thành công.</returns>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] Login loginUser)
        {
            string hashedPassword = HashPassword(loginUser.password);

            var user = (from u in _context.adm_user
                        where u.USER_NAME == loginUser.user_name
                        && u.USER_PWD == hashedPassword
                        && u.STATUS == 1
                        select new
                        {
                            u.USER_ID,
                            u.FULL_NAME,
                            u.USER_NAME,
                            u.OFFICER_ID,
                            u.USER_LEVEL,
                            u.CSYTID,
                            u.STATUS,
                            u.NOTE,
                            u.ROLE
                        }).FirstOrDefault();

            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("sdfsdfdsf34fsdfs@1234fsdfsdfsdg54sdg45dsfgsg5");
            //var roles = "";
            var claims = new List<System.Security.Claims.Claim>
            {
                new Claim("USER_ID", user.USER_ID.ToString()),
                new Claim("FULL_NAME", user.FULL_NAME ?? ""),
                new Claim("USER_NAME", user.USER_NAME ?? ""),
                new Claim("OFFICER_ID", user.OFFICER_ID.ToString()),
                new Claim("USER_LEVEL", user.USER_LEVEL.ToString()),
                new Claim("CSYTID", user.CSYTID.ToString()),
                new Claim("STATUS", user.STATUS.ToString()),
                new Claim("NOTE", user.NOTE ?? ""),
                //new Claim("ROLE", user.ROLE ?? "")

            };
            string[] roles = user.ROLE.Split(",");
            foreach (var role in roles) {
                claims.Add(new Claim("ROLE", role));
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(3),
                Issuer = "HNM",
                Audience = "Audience@HNM",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                message = "Login complete!",
                token = tokenString,
                user
            });
        }

        // POST: api/Login/ChangePassword
        /// <summary>
        /// Đổi mật khẩu.
        /// </summary>
        /// <param name="request">Thông tin đổi mật khẩu bao gồm email, mật khẩu cũ và mật khảu mới.</param>
        /// <returns>Trả về JWT token nếu đăng nhập thành công.</returns>
        [Authorize]
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = _context.adm_user.FirstOrDefault(u => u.USER_NAME == request.USER_NAME);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            string hashedOldPassword = HashPassword(request.oldPassword);
            if (user.USER_PWD != hashedOldPassword)
            {
                return NotFound(new { message = "Mật khẩu cũ không chính xác." });
            }

            user.USER_PWD = HashPassword(request.newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        // POST: api/Login/ResetPassword
        /// <summary>
        /// Đặt lại mật khẩu.
        /// </summary>
        /// <param name="request">Thông tin đặt lại mật khẩu bao gồm mã nhân viên và mật khẩu mới.</param>
        /// <returns>Trả về thông báo thành công nếu đặt lại mật khẩu thành công.</returns>
        [Authorize(Roles = "admin")]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.adm_user.FirstOrDefaultAsync(u => u.USER_NAME == request.USER_NAME);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }
            user.USER_PWD = HashPassword(request.newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công!" });
        }
    }
    public class ResetPasswordRequest
    {
        public string USER_NAME { get; set; }
        public string newPassword { get; set; }
    }
    public class Login
    {
        public string user_name { get; set; }
        public string password { get; set; }
    }
    public class ChangePasswordRequest
    {
        public string USER_NAME { get; set; }
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
    }
}
