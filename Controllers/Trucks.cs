using Microsoft.AspNetCore.Mvc;
using WasteManagement3.Data;
using WasteManagement3.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace WasteManagement3.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/truck")]
    public class TruckController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TruckController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddTruck([FromBody] Truck truck)
        {
            if (await _context.Truck.AnyAsync(t => t.TruckName == truck.TruckName))
                return BadRequest("🚫 Truck already exists.");

            _context.Truck.Add(truck);
            await _context.SaveChangesAsync();
            return Ok("✅ Truck ajouté avec succès !");
        }
    }
}
