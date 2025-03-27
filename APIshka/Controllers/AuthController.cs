using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using APIshka.DataBaseContext;
using APIshka.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using APIshka.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;

namespace APIshka.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Login == request.Username);
            if (user == null || user.PasswordHash != request.Password)
                return Unauthorized("Invalid credentials");

            var token = GenerateJwtToken(user.Id);
            user.Token = token;
            user.TokenExpiry = DateTime.UtcNow.AddDays(7);
            _context.SaveChanges();

            return Ok(new { token, user.Coins });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Login == request.Login))
                return BadRequest("Username already exists");

            var user = new User
            {
                Login = request.Login,
                PasswordHash = request.Password,
                Coins = 100
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user.Id);
            user.Token = token;
            user.TokenExpiry = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new { token, user.Coins });
        }

        private string GenerateJwtToken(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", userId.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token); // Используем WriteToken вместо ToString()
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }


    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Вспомогательный метод для валидации токена
        private int? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                }, out _);

                return int.Parse(principal.FindFirst("id").Value);
            }
            catch
            {
                return null;
            }
        }

        #region Монеты
        [HttpPost("get-coins")]
        public IActionResult GetCoins([FromBody] TokenRequest request)
        {
            var userId = ValidateToken(request.Token);
            if (userId == null) return Unauthorized();

            var coins = _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Coins)
                .FirstOrDefault();

            return Ok(new { coins });
        }

        [HttpPost("add-coins")]
        public IActionResult AddCoins([FromBody] AddCoinsRequest request)
        {
            var userId = ValidateToken(request.Token);
            if (userId == null)
                return Unauthorized("Invalid token");

           var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            if (request.Amount <= 0)
                return BadRequest("Amount must be positive");

            user.Coins += request.Amount;

            try
            {
                _context.SaveChanges();
                return Ok(new
                {
                    Success = true,
                    NewBalance = user.Coins
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating balance");
            }
        }
        #endregion

        #region Скины
        [HttpPost("get-skins")]
        public IActionResult GetSkins([FromBody] TokenRequest request)
        {
            var userId = ValidateToken(request.Token);
            if (userId == null) return Unauthorized();

            var skins = _context.Skins
                .Where(s => s.UserId == userId)
                .Select(s => new { s.Id, s.Name, s.Price })
                .ToList();

            return Ok(skins);
        }

        [HttpPost("get-available-skins")]
        public IActionResult GetAvailableSkins([FromBody] TokenRequest request)
        {
            var userId = ValidateToken(request.Token);
            if (userId == null) return Unauthorized();

            var skins = _context.Skins
                .Where(s => s.UserId == null)
                .Select(s => new { s.Id, s.Name, s.Price })
                .ToList();

            return Ok(skins);
        }

        [HttpPost("buy-skin")]
        public IActionResult BuySkin([FromBody] BuySkinRequest request)
        {
            var userId = ValidateToken(request.Token);
            if (userId == null) return Unauthorized();

            var user = _context.Users.Include(u => u.Skins).First(u => u.Id == userId);
            var skin = _context.Skins.Find(request.SkinId);

            if (skin == null) return NotFound("Skin not found");
            if (skin.UserId != null) return BadRequest("Skin already purchased");
            if (user.Coins < skin.Price) return BadRequest("Not enough coins");

            user.Coins -= skin.Price;
            skin.UserId = (int)userId;
            user.Skins.Add(skin);

            _context.SaveChanges();
            return Ok(new { user.Coins });
        }
        #endregion
    }

    // DTO классы
    public class TokenRequest
    {
        public string Token { get; set; }
    }

    public class AddCoinsRequest
    {
        public string Token { get; set; }
        public int Amount { get; set; }
    }

    public class BuySkinRequest
    {
        public string Token { get; set; }
        public int SkinId { get; set; }
    }

}