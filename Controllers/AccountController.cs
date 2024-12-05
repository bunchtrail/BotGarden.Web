using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BotGarden.Infrastructure.Contexts;
using BotGardens.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;

namespace BotGardens.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly BotanicGardenContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(BotanicGardenContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            // Валидация входных данных
            if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
            {
                return BadRequest("Необходимо указать email и пароль.");
            }

            // Проверка существующего пользователя
            var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.userEmail == userDto.Email);
            if (existingUser != null)
            {
                return Conflict("Пользователь с таким email уже существует.");
            }

            var user = new Users
            {
                userEmail = userDto.Email,
                userHashedPass = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                userRole = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Генерация токенов
            var (accessToken, refreshToken) = await GenerateTokens(user);

            return Ok(new
            {
                Message = "Пользователь успешно зарегистрирован",
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserDto userDto)
        {
            // Валидация входных данных
            if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
            {
                return BadRequest("Необходимо указать email и пароль.");
            }

            var user = await _context.Users.SingleOrDefaultAsync(x => x.userEmail == userDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.userHashedPass))
            {
                return Unauthorized("Неверный email или пароль.");
            }

            // Генерация токенов
            var (accessToken, refreshToken) = await GenerateTokens(user);

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUser()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.userEmail == email);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { user.userEmail, user.userRole });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel tokenModel)
        {
            if (tokenModel == null || string.IsNullOrEmpty(tokenModel.RefreshToken))
            {
                return BadRequest("Некорректный запрос.");
            }

            var principal = GetPrincipalFromExpiredToken(tokenModel.Token);
            if (principal == null)
            {
                return Unauthorized("Недействительный access-токен.");
            }


            var email = principal.Identity.Name;

            var user = await _context.Users.SingleOrDefaultAsync(u => u.userEmail == email);
            if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Недействительный или истекший refresh-токен.");
            }

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        private async Task<(string AccessToken, string RefreshToken)> GenerateTokens(Users user)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Сохранение refresh-токена в базе данных
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (accessToken, refreshToken);
        }

        private string GenerateJwtToken(Users user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.userEmail),
                new Claim(ClaimTypes.Role, user.userRole)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(10),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false // Позволяем валидировать истекшие токены
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

    // Объекты передачи данных (DTO)
    public class UserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class TokenModel
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
