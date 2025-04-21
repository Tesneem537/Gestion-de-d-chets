using Microsoft.AspNetCore.Mvc;
using WasteManagement3.Data;
using WasteManagement3.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WasteManagement3.Controllers
{
    [ApiController]
    [Route("api/hotel")]
    public class HotelController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HotelController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddHotel([FromBody] Hotel hotel)
        {
            if (await _context.Hotel.AnyAsync(h => h.HotelName == hotel.HotelName))
                return BadRequest("🚫 Hotel already exists.");

            _context.Hotel.Add(hotel);
            await _context.SaveChangesAsync();
            return Ok("✅ Hotel ajouté avec succès !");
        }
        [HttpGet("getHotel")]
        public IActionResult GetWasteCollectionsByHotelName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Hotel name is required.");

            var collections = _context.WasteCollection
                .Include(w => w.Collector)
                .Include(w => w.Hotel)
                .Include(w => w.Truck)
                .Include(w => w.WasteType)
                .Where(w => w.Hotel.HotelName == name)
                .Select(w => new
                {
                    w.WasteCollectionID,
                    HotelName = w.Hotel.HotelName,
                    CollectorName = w.Collector != null ? w.Collector.CollectorName : null,
                    TruckName = w.Truck != null ? w.Truck.TruckName : null,
                    WasteType = w.WasteType != null ? w.WasteType.WasteTypeName : null,
                    w.Quantity,
                    w.Comment,
                    EntryTime = w.EntryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Photo = w.Photo != null ? Convert.ToBase64String(w.Photo) : null
                })

                .ToList();

            if (!collections.Any())
                return NotFound($"No waste collections found for Hotel name: {name}");

            return Ok(collections);

        }


    }


}
