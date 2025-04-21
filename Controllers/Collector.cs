using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using WasteManagement3.Data;
using WasteManagement3.Models;
using WasteManagement3.DTOs;
using WasteManagement3.Controllers;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Build.Framework;
using ILogger = Microsoft.Build.Framework.ILogger;

namespace WasteManagement3.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/collector")]
    public class Collecteur : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public Collecteur(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("Enregistrer")]
        public async Task<ActionResult<WasteCollectionDto>> PostWasteCollection([FromForm] WasteCollectionDto wasteCollectionDto)
        {
            try
            {

                var collector = await _context.Collector.FirstOrDefaultAsync(c => c.CollectorName == wasteCollectionDto.CollectorName);

                if (collector == null)
                {
                    return BadRequest("collector not found (collector, hotel, truck, or waste type).");
                }
                var hotel = await _context.Hotel.FirstOrDefaultAsync(h => h.HotelName == wasteCollectionDto.HotelName);

                if (hotel == null)
                {
                    return BadRequest("hotel not found (collector, hotel, truck, or waste type).");
                }
                var truck = await _context.Truck.FirstOrDefaultAsync(t => t.TruckName == wasteCollectionDto.TruckName);

                if (truck == null)
                {
                    return BadRequest("truck not found (collector, hotel, truck, or waste type).");
                }
                var wasteType = await _context.WasteType.FirstOrDefaultAsync(w => w.WasteTypeName == wasteCollectionDto.WasteTypeName);

                if (wasteType == null)
                {
                    return BadRequest("wasteType not found (collector, hotel, truck, or waste type).");
                }



                byte[]? imageData = null;

                if (wasteCollectionDto.Photo != null)
                {


                    if (wasteCollectionDto.Photo.Length > 10 * 1024 * 1024)
                        return BadRequest("❌ L'image doit être inférieure à 5 Mo.");

                    using (var ms = new MemoryStream())
                    {
                        await wasteCollectionDto.Photo.CopyToAsync(ms);
                        imageData = ms.ToArray();
                    }
                }


                var wasteCollection = new WasteCollection
                {
                    CollectorID = collector.CollectorID,
                    HotelID = hotel.HotelID,
                    TruckName = truck.TruckName,
                    TruckID = truck.TruckID,
                    WasteTypeID = wasteType.WasteTypeID,
                    WasteTypeName = wasteType.WasteTypeName,
                    Quantity = wasteCollectionDto.Quantity,
                    Comment = wasteCollectionDto.Comment,
                    EntryTime = wasteCollectionDto.EntryTime, 
                    Photo = imageData,
                    CollectorName = wasteCollectionDto.CollectorName,
                    HotelName = wasteCollectionDto.HotelName,

                };

                _context.WasteCollection.Add(wasteCollection);
                await _context.SaveChangesAsync();


                return CreatedAtAction(nameof(PostWasteCollection), new { id = wasteCollection.WasteCollectionID }, wasteCollectionDto);


            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"❌ Internal server error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpGet("getCollector")]
        public IActionResult GetWasteCollectionsByCollectorName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Collector name is required.");

            var collections = _context.WasteCollection
                .Include(w => w.Collector)
                .Include(w => w.Hotel)
                .Include(w => w.Truck)
                .Include(w => w.WasteType)
                .Where(w => w.Collector.CollectorName == name)
                .Select(w => new
                {
                    w.WasteCollectionID,
                    CollectorName = w.Collector.CollectorName,
                    HotelName = w.Hotel != null ? w.Hotel.HotelName : null,
                    TruckName = w.Truck != null ? w.Truck.TruckName : null,
                    WasteType = w.WasteType != null ? w.WasteType.WasteTypeName : null,
                    w.Quantity,
                    w.Comment,
                    EntryTime = w.EntryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Photo = w.Photo != null ? Convert.ToBase64String(w.Photo) : null
                })
                
                .ToList();

            if (!collections.Any())
                return NotFound($"No waste collections found for collector name: {name}");

            return Ok(collections);
        }





    }

}

