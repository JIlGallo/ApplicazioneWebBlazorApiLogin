// AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlazorAuthApp.API.Models;
using Microsoft.EntityFrameworkCore;
using BlazorAuthApp.API.Data;
using BCrypt.Net;

namespace BlazorAuthApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        public AuthController(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }


        [HttpPost("register")] // Definisci una nuova route per la registrazione
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto user)
        {
            // Verifica se lo username è già presente nel database
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                return BadRequest("Username già esistente.");
            }

            // Eseguire l'hashing della password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);

            // Crea un nuovo utente con l'hash della password
            var newUser = new User
            {
                Username = user.Username,
                PasswordHash = passwordHash
            };

            // Salva il nuovo utente nel database
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            // Restituisci una risposta di successo
            return Ok();
        }


        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] UserLoginDto user)
        {
            // Trova l'utente nel database
            var dbUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == user.Username);

            if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.PasswordHash))
            {
                return Unauthorized(); // Utente non trovato o password errata
            }

            // Genera il token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, dbUser.Username)
        }),
                Expires = DateTime.UtcNow.AddMinutes(60), // Imposta la scadenza del token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { Token = tokenHandler.WriteToken(token) });
        }
    }
    public class UserLoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class UserRegistrationDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

