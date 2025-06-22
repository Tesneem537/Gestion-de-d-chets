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

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto userDto)
        {
            try
            {
                // Validate input
                if (userDto == null || string.IsNullOrEmpty(userDto.Email) || string.IsNullOrEmpty(userDto.Password))
                    return BadRequest(new { message = "Invalid user data" });

                // Check if user exists (case-insensitive)
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == userDto.Email.ToLower());

                if (existingUser != null)
                    return Conflict(new { message = "User already exists" });

                // Hash password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

                // Create transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create and save user
                    var newUser = new Users
                    {
                        Email = userDto.Email,
                        PasswordHash = hashedPassword,
                        UserName = userDto.Name,
                        Role = userDto.Role
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // SIMPLY ADD NAME TO RESPECTIVE TABLE
                    if (userDto.Role == "Collector")
                    {
                        _context.Collector.Add(new Collector
                        {
                            CollectorName = userDto.Name // Only setting the name
                        });
                    }
                    else if (userDto.Role == "Hotel")
                    {
                        _context.Hotel.Add(new Hotel
                        {
                            HotelName = userDto.Name // Only setting the name
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "User registered successfully" });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
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

            return Ok(new { token, role = user.Role });
        }
    } }

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



