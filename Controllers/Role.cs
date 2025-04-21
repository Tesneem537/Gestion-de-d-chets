using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WasteManagement3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Role : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-data")]
        public IActionResult GetAdminData()
        {
            return Ok("Admin data accessed successfully!");
        }

        [Authorize(Roles = "Collector")]
        [HttpGet("collector-data")]
        public IActionResult GetCollectorData()
        {
            return Ok("Collector data accessed successfully!");
        }

        [Authorize(Roles = "Hotel")]
        [HttpGet("hotel-data")]
        public IActionResult GetHotelData()
        {
            return Ok("Hotel data accessed successfully!");
        }
    }
}
