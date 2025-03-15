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

namespace APIshka.Controllers
{
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

        public CoinsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("balance")]
        public IActionResult GetBalance(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound();

            return Ok(user.Coins);
        }
    }
    [ApiController]
    [Route("api/[controller]")]
    public class RewardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RewardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public IActionResult AddCoins([FromBody] AddCoinsRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user == null)
                return NotFound();

            user.Coins += request.Amount;
            _context.SaveChanges();
            return Ok(user);
        }
    }
    public class AddCoinsRequest
    {
        public int UserId { get; set; }
        public int Amount { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SkinsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SkinsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("available")]
        public IActionResult GetAvailableSkins()
        {
            var skins = _context.Skins.Where(s => s.UserId == null).ToList();
            return Ok(skins);
        }

        [HttpPost("purchase")]
        public IActionResult PurchaseSkin([FromBody] PurchaseRequest request)
        {
            var user = _context.Users.Include(u => u.Skins).FirstOrDefault(u => u.Id == request.UserId);
            var skin = _context.Skins.FirstOrDefault(s => s.Id == request.SkinId);

            if (user == null || skin == null)
                return NotFound();

            if (user.Coins < skin.Price)
                return BadRequest("Not enough coins");

            user.Coins -= skin.Price;
            skin.UserId = user.Id;
            user.Skins.Add(skin);

            var transaction = new Transaction
            {
                UserId = user.Id,
                SkinId = skin.Id,
                Date = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);

            _context.SaveChanges();
            return Ok(skin);
        }
    }
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

}