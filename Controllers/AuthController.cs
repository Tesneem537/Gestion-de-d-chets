using Microsoft.AspNetCore.Mvc;
using WasteManagement3.Data;
using WasteManagement3.Models;
using WasteManagement3.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Generators;
namespace WasteManagement3.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AuthController(ApplicationDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // User Registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
                return BadRequest(new { message = "Invalid user data" });

            // Check if the user already exists
            var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.Email == userDto.Email);
            if (existingUser != null)
                return Conflict(new { message = "User already exists" });

            // Hash the user's password using BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            // Create new user
            var newUser = new Users
            {
                Email = userDto.Email,
                PasswordHash = hashedPassword,
                UserName = userDto.Name,
                Role = userDto.Role
            };

            // Save to database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        // User Login (Generate JWT Token)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null || string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
                return BadRequest(new { message = "Invalid credentials" });

            // Check if the user exists
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            // Verify the password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            // Generate JWT token
            var token = _authService.GenerateJwtToken(user);

            return Ok(new { token , role = user.Role });
        }
    }

    // DTO for Registering a User
    public class RegisterDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } 
    }

    // DTO for Logging In
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }


}
