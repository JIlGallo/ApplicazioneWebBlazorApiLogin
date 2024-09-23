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
using Microsoft.AspNetCore.Authorization;

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
        [HttpGet("{userId}/username")]
        public async Task<IActionResult> GetUsername(int userId)
        {
            // Cerca l'utente nel database per ID
            var user = await _context.Users.FindAsync(userId); // Assumiamo che il tuo modello utente si chiami "Users"

            if (user == null)
            {
                return NotFound(); // Restituisci 404 Not Found se l'utente non viene trovato
            }

            // Restituisci il nome utente
            return Ok(user.Username);
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
            new Claim(ClaimTypes.Name, dbUser.Username),
            new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString())
        }),
                Expires = DateTime.UtcNow.AddMinutes(60), // Imposta la scadenza del token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { Token = tokenHandler.WriteToken(token) });
        }
        [HttpPut("{userId}/username")]
        [Authorize]
        public async Task<IActionResult> UpdateUsername(int userId, [FromBody] UpdateUsernameDto request)
        {
            // Verifica se l'ID utente nella richiesta corrisponde a quello autenticato
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId != currentUserId)
            {
                return Unauthorized("Non autorizzato a modificare questo utente.");
            }

            // Cerca l'utente nel database
            var dbUser = await _context.Users.FindAsync(userId);
            if (dbUser == null)
            {
                return NotFound("Utente non trovato.");
            }

            // Verifica se il nuovo nome utente è già in uso
            if (await _context.Users.AnyAsync(u => u.Username == request.NewUsername))
            {
                return BadRequest("Nome utente già esistente.");
            }

            // Aggiorna il nome utente
            dbUser.Username = request.NewUsername;
            await _context.SaveChangesAsync(); // Salva le modifiche nel database

            // Genera un nuovo token JWT con il nome utente aggiornato
            var newToken = GenerateJwtToken(dbUser);

            return Ok(new { Token = newToken }); // Restituisci il nuovo token al client
        }
        // In AuthController.cs


        [HttpPut("{userId}/password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword(int userId, [FromBody] UpdatePasswordDto request)
        {
            // Verifica se l'ID utente nella richiesta corrisponde a quello autenticato
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId != currentUserId)
            {
                return Unauthorized("Non autorizzato a modificare questo utente.");
            }

            // Cerca l'utente nel database
            var dbUser = await _context.Users.FindAsync(userId);
            if (dbUser == null)
            {
                return NotFound("Utente non trovato.");
            }

            // Verifica la vecchia password
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, dbUser.PasswordHash))
            {
                return BadRequest("Vecchia password errata.");
            }

            // Aggiorna la password
            dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync(); // Salva le modifiche nel database

            // Genera un nuovo token JWT
            var newToken = GenerateJwtToken(dbUser);

            return Ok(new { Token = newToken }); // Restituisci il nuovo token al client
        }
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(60), // Imposta la scadenza del token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok();
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
    public class UpdateUsernameDto
    {
        public string NewUsername { get; set; }
    }

    public class UpdatePasswordDto
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}

