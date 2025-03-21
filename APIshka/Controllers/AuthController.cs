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

namespace APIshka.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RegisterController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Login == request.Login))
                return Conflict("User already exists");

            var user = new User
            {
                Login = request.Login,
                PasswordHash = HashPassword(request.Password),
                Coins = 0
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return Ok(user);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
    [ApiController]
    [Route("api/[controller]")]
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
            var user = _context.Users.FirstOrDefault(u => u.Login == request.Username && u.PasswordHash == HashPassword(request.Password));
            if (user == null)
                return Unauthorized();

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CoinsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CoinsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("balance")]
        public IActionResult GetBalance([FromQuery] string token) // Токен передается в параметре запроса
        {
            // Проверяем валидность токена и извлекаем userId
            var userId = ValidateTokenAndGetUserId(token);
            if (userId == null)
                return Unauthorized("Invalid token");

            // Находим пользователя в базе данных
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            return Ok(new { Balance = user.Coins });
        }

        private int? ValidateTokenAndGetUserId(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                // Валидируем токен
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                }, out var validatedToken);

                // Извлекаем userId из токена
                var userIdClaim = principal.FindFirst("id")?.Value;
                if (userIdClaim == null)
                    return null;

                return int.Parse(userIdClaim);
            }
            catch
            {
                return null; // Токен невалиден
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class RewardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public RewardController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("add")]
        public IActionResult AddCoins([FromBody] AddCoinsRequest request)
        {
            // Проверяем валидность токена и извлекаем userId
            var userId = ValidateTokenAndGetUserId(request.Token);
            if (userId == null)
                return Unauthorized("Invalid token");

            // Находим пользователя в базе данных
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            // Добавляем монеты пользователю
            user.Coins += request.Amount;
            _context.SaveChanges();

            return Ok(user);
        }

        private int? ValidateTokenAndGetUserId(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                // Валидируем токен
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                }, out var validatedToken);

                // Извлекаем userId из токена
                var userIdClaim = principal.FindFirst("id")?.Value;
                if (userIdClaim == null)
                    return null;

                return int.Parse(userIdClaim);
            }
            catch
            {
                return null; // Токен невалиден
            }
        }
    }

    public class AddCoinsRequest
    {
        public string Token { get; set; } 
        public int Amount { get; set; }  
    }

    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAll")]
    public class SkinsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SkinsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("available")]
        public IActionResult GetAvailableSkins([FromQuery] string token) // Токен передается в параметре запроса
        {
            // Проверяем валидность токена
            var userId = ValidateTokenAndGetUserId(token);
            if (userId == null)
                return Unauthorized("Invalid token");

            // Получаем список скинов, которые еще не куплены (UserId == null)
            var skins = _context.Skins.Where(s => s.UserId == null).ToList();
            return Ok(skins);
        }

        [HttpPost("purchase")]
        public IActionResult PurchaseSkin([FromBody] PurchaseRequest request)
        {
            // Проверяем валидность токена и извлекаем userId
            var userId = ValidateTokenAndGetUserId(request.Token);
            if (userId == null)
                return Unauthorized("Invalid token");

            // Находим пользователя и скин в базе данных
            var user = _context.Users.Include(u => u.Skins).FirstOrDefault(u => u.Id == userId);
            var skin = _context.Skins.FirstOrDefault(s => s.Id == request.SkinId);

            if (user == null || skin == null)
                return NotFound("User or skin not found");

            // Проверяем, достаточно ли у пользователя монет для покупки
            if (user.Coins < skin.Price)
                return BadRequest("Not enough coins");

            // Выполняем покупку
            user.Coins -= skin.Price; // Списание монет
            skin.UserId = user.Id;   // Привязка скина к пользователю
            user.Skins.Add(skin);    // Добавление скина в коллекцию пользователя

            // Создаем запись о транзакции
            var transaction = new Transaction
            {
                UserId = user.Id,
                SkinId = skin.Id,
                Date = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);

            // Сохраняем изменения в базе данных
            _context.SaveChanges();

            return Ok(skin);
        }

        private int? ValidateTokenAndGetUserId(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                // Валидируем токен
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                }, out var validatedToken);

                // Извлекаем userId из токена
                var userIdClaim = principal.FindFirst("id")?.Value;
                if (userIdClaim == null)
                    return null;

                return int.Parse(userIdClaim);
            }
            catch
            {
                return null; // Токен невалиден
            }
        }
    }
    public class PurchaseRequest
    {
        public string Token { get; set; } // Токен передается в теле запроса
        public int SkinId { get; set; }   // Идентификатор скина
    }
   

}