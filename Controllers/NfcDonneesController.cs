using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WasteManagement3.Data;
using WasteManagement3.DTOs;
using WasteManagement3.Models;
using Microsoft.Extensions.Logging;
using WasteManagement3.Services;

namespace WasteManagement3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NfcDonneesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationService _locationService;
        private readonly ILogger<NfcDonneesController> _logger;

        public NfcDonneesController(
            ApplicationDbContext context,
            ILocationService locationService,
            ILogger<NfcDonneesController> logger)
        {
            _context = context;
            _locationService = locationService;
            _logger = logger;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan([FromBody] NfcScanDto scan)
        {
            try
            {
                if (string.IsNullOrEmpty(scan.TagId))
                {
                    _logger.LogWarning("Scan attempted without TagId");
                    return BadRequest(new { error = "TagId is required" });
                }

                var location = !string.IsNullOrEmpty(scan.Location)
                    ? scan.Location
                    : await _locationService.GetCurrentLocationAsync();

                if (string.IsNullOrEmpty(location))
                {
                    _logger.LogWarning("Location could not be determined for TagId: {TagId}", scan.TagId);
                    return BadRequest(new { error = "Location could not be determined" });
                }

                var openRecord = await _context.NFCdonnees
                    .Where(x => x.TagId == scan.TagId && x.ExitTime == null)
                    .OrderByDescending(x => x.EntryTime)
                    .FirstOrDefaultAsync();

                if (openRecord == null)
                {
                    var newEntry = new NFCdonnees
                    {
                        TagId = scan.TagId,
                        EntryTime = scan.ScanTime ?? DateTime.UtcNow,
                        EntryLocation = location
                    };

                    _context.NFCdonnees.Add(newEntry);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New entry recorded for TagId: {TagId}", scan.TagId);

                    return Ok(new ScanResponseDto
                    {
                        Success = true,
                        Message = "Entry recorded successfully",
                        ScanType = "entry",
                        TagId = newEntry.TagId,
                        EntryTime = newEntry.EntryTime,
                        EntryLocation = newEntry.EntryLocation
                    });
                }
                else
                {
                    var scanTime = scan.ScanTime ?? DateTime.UtcNow;

                    if (scanTime - openRecord.EntryTime < TimeSpan.FromSeconds(5))
                    {
                        _logger.LogInformation("Duplicate scan ignored for TagId: {TagId}", scan.TagId);
                        return Ok(new ScanResponseDto
                        {
                            Success = false,
                            Message = "Duplicate entry scan ignored",
                            ScanType = "entry",
                            TagId = openRecord.TagId,
                            EntryTime = openRecord.EntryTime,
                            EntryLocation = openRecord.EntryLocation
                        });
                    }

                    openRecord.ExitTime = scanTime;
                    openRecord.ExitLocation = location;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Exit recorded for TagId: {TagId}", scan.TagId);

                    return Ok(new ScanResponseDto
                    {
                        Success = true,
                        Message = "Exit recorded successfully",
                        ScanType = "exit",
                        TagId = openRecord.TagId,
                        EntryTime = openRecord.EntryTime,
                        EntryLocation = openRecord.EntryLocation,
                        ExitTime = openRecord.ExitTime,
                        ExitLocation = openRecord.ExitLocation,
                        DurationMinutes = (openRecord.ExitTime - openRecord.EntryTime)?.TotalMinutes
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scan for TagId: {TagId}", scan?.TagId);
                return StatusCode(500, new
                {
                    error = "An error occurred while processing the scan",
                    details = ex.Message
                });
            }
        }
    }
}