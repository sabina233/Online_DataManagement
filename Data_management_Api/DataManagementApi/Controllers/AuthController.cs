using Microsoft.AspNetCore.Mvc;
using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DataManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest("用户名和密码不能为空");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            
            if (user == null)
            {
                return Unauthorized(new { message = "用户不存在" });
            }

            // 注意：生产环境应使用哈希加密。目前保持与现有逻辑一致，使用明文。
            if (user.Password != request.Password)
            {
                return Unauthorized(new { message = "密码错误" });
            }

            // 生成 JWT Token
            var token = GenerateJwtToken(user);

            return Ok(new { 
                user = user,
                token = token 
            });
        }
    
        /// <summary>
        /// 修改密码
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                return NotFound(new { message = "未找到用户" });
            }

            // 验证当前密码
            if (user.Password != request.OldPassword)
            {
                return BadRequest(new { message = "当前密码不正确" });
            }

            user.Password = request.NewPassword;
            await _context.SaveChangesAsync();

            return Ok(new { message = "密码更新成功" });
        }

        /// <summary>
        /// 生成 JWT Token 辅助方法
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? ""));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "user")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(144), // 144小时过期
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>
    /// 登录请求模型
    /// </summary>
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    /// <summary>
    /// 修改密码请求模型
    /// </summary>
    public class ChangePasswordRequest
    {
        public required string Username { get; set; }
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
     }
 }
