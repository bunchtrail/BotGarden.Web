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
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
            {
                return BadRequest("Необходимо указать email и пароль.");
            }

            var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.userEmail == userDto.Email);
            if (existingUser != null)
            {
                return Conflict("Пользователь с таким email уже существует.");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var newUser = new Users
            {
                userEmail = userDto.Email,
                userHashedPass = hashedPassword,
                userRole = "User"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var (accessToken, refreshTokenPlain) = await GenerateTokens(newUser);

            // Возвращаем плейн refresh-токен клиенту, он должен хранить его в защищенном месте (httpOnly cookie)
            return Ok(new
            {
                Message = "Пользователь успешно зарегистрирован",
                AccessToken = accessToken,
                RefreshToken = refreshTokenPlain
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
            {
                return BadRequest("Необходимо указать email и пароль.");
            }

            var user = await _context.Users.SingleOrDefaultAsync(x => x.userEmail == userDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.userHashedPass))
            {
                return Unauthorized("Неверный email или пароль.");
            }

            var (accessToken, refreshTokenPlain) = await GenerateTokens(user);

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenPlain
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
                return NotFound("Пользователь не найден.");
            }

            return Ok(new
            {
                Email = user.userEmail,
                Role = user.userRole
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] TokenModel tokenModel)
        {
            if (tokenModel == null || string.IsNullOrEmpty(tokenModel.RefreshToken) || string.IsNullOrEmpty(tokenModel.Token))
            {
                return BadRequest("Некорректный запрос.");
            }

            var principal = GetPrincipalFromExpiredToken(tokenModel.Token);
            if (principal == null)
            {
                return Unauthorized("Недействительный access-токен.");
            }

            var email = principal.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Недействительный токен.");
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.userEmail == email);
            if (user == null || string.IsNullOrEmpty(user.RefreshTokenHash))
            {
                return Unauthorized("Пользователь не найден или не авторизован.");
            }

            // Проверяем совпадение хэша refresh-токена
            if (!VerifyRefreshToken(tokenModel.RefreshToken, user.RefreshTokenHash) ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Недействительный или истекший refresh-токен.");
            }

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshTokenPlain = GenerateRefreshToken();
            user.RefreshTokenHash = HashRefreshToken(newRefreshTokenPlain);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenPlain
            });
        }

        private async Task<(string AccessToken, string RefreshTokenPlain)> GenerateTokens(Users user)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshTokenPlain = GenerateRefreshToken();
            var refreshTokenHash = HashRefreshToken(refreshTokenPlain);

            user.RefreshTokenHash = refreshTokenHash;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return (accessToken, refreshTokenPlain);
        }

        private string GenerateJwtToken(Users user)
        {
            var claims = new Claim[]
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
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(1), // Access-токен на 1 час
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }

        private string HashRefreshToken(string refreshTokenPlain)
        {
            // Можно использовать HMACSHA256
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshTokenPlain));
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyRefreshToken(string refreshTokenPlain, string storedHash)
        {
            var hashedInput = HashRefreshToken(refreshTokenPlain);
            return storedHash == hashedInput;
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
                ValidateLifetime = false // Разрешаем истекший токен для извлечения клеймов
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                // Проверяем, что токен действительно JWT
                if (securityToken is JwtSecurityToken jwtToken &&
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return principal;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }

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
