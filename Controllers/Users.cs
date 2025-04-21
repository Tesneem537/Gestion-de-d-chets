using Microsoft.AspNetCore.Mvc;
using WasteManagement3.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using WasteManagement3.Data;
namespace WasteManagement3.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class User: ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public User(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all users
        [HttpGet("getUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync(); // Fetch users from the database
            return Ok(users);
        }
    }
}