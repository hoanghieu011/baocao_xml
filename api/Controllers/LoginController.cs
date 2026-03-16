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

            var user = (from u in _context.user
                        join nv in _context.nhan_vien on u.ma_nv equals nv.ma_nv
                        join vt in _context.vi_tri on nv.ma_vi_tri equals vt.ma_vi_tri into vtGroup
                        from vt in vtGroup.DefaultIfEmpty()
                        join bp in _context.bo_phan on nv.bo_phan_id equals bp.id into bpGroup
                        from bp in bpGroup.DefaultIfEmpty()
                        where (nv.ma_nv == loginUser.ma_nv && u.password == hashedPassword &&
                                nv.xoa != 1)
                        select new
                        {
                            u.id,
                            u.id_nv,
                            nv.ma_nv,
                            nv.full_name,
                            nv.gioi_tinh,
                            nv.ma_vi_tri,
                            ten_vi_tri = vt != null ? vt.ten_vi_tri : null,
                            ten_bo_phan = bp != null ? bp.ten_bo_phan : null,
                            nv.cong_viec,
                            nv.email,
                            role = u.role
                        }).FirstOrDefault();

            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác!" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("sdfsdfdsf34fsdfs@1234fsdfsdfsdg54sdg45dsfgsg5");
            var roles = user.role.Split(',');
            var claims = new List<Claim>
            {
                new Claim("id", user.id.ToString()),
                new Claim("id_nv", user.id_nv.ToString()),
                new Claim("ma_nv", user.ma_nv),
                new Claim("full_name", user.full_name),
                new Claim("gioi_tinh", user.gioi_tinh ?? string.Empty),
                new Claim("ma_vi_tri", user.ma_vi_tri ?? string.Empty),
                new Claim("vi_tri", user.ten_vi_tri ?? string.Empty),
                new Claim("ten_bo_phan", user.ten_bo_phan ?? string.Empty),
                new Claim("cong_viec", user.cong_viec ?? string.Empty),
                new Claim("email", user.email ?? string.Empty),
                new Claim("roles", user.role ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(3),
                Issuer = "HNM",
                Audience = "SINFONIA",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
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
            var user = _context.user.FirstOrDefault(u => u.ma_nv == request.ma_nv);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            string hashedOldPassword = HashPassword(request.oldPassword);
            if (user.password != hashedOldPassword)
            {
                return NotFound(new { message = "Mật khẩu cũ không chính xác." });
            }

            user.password = HashPassword(request.newPassword);
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
            var user = await _context.user.FirstOrDefaultAsync(u => u.ma_nv == request.ma_nv);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }
            user.password = HashPassword(request.newPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công!" });
        }
    }
    public class ResetPasswordRequest
    {
        public string ma_nv { get; set; }
        public string newPassword { get; set; }
    }
    public class Login
    {
        public string ma_nv { get; set; }
        public string password { get; set; }
    }
    public class ChangePasswordRequest
    {
        public string ma_nv { get; set; }
        public string oldPassword { get; set; }
        public string newPassword { get; set; }
    }
}
