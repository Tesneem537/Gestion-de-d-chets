using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteManagement3.Controllers;
using WasteManagement3.Data;
using WasteManagement3.Models;
using WasteManagement3.DTOs;
using WasteManagement3.Services; 

[AllowAnonymous]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok("Welcome Admin!");
    }
    [HttpPost("addcollector")]

    public async Task<IActionResult> Add([FromBody] CollectorDto CollectorDto)
    {
        if (CollectorDto == null)
            return BadRequest(new { message = "Invalid Collector data" });

        // Check if the user already exists
        var existingUser = await _context.Collector.SingleOrDefaultAsync(u => u.CollectorName == CollectorDto.CollectorName);
        if (existingUser != null)
            return Conflict(new { message = "Collector already exists" });


        // Create new user
        var newCollector = new Collector
        {
            CollectorName = CollectorDto.CollectorName,

        };

        // Save to database
        _context.Collector.Add(newCollector);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Collector registered successfully" });

    }

    [HttpPost("addhotel")]

    public async Task<IActionResult> AddHotel([FromBody] HotelDto HotelDto)
    {
        if (HotelDto == null)
            return BadRequest(new { message = "Invalid Collector data" });

        // Check if the user already exists
        var existingUser = await _context.Hotel.SingleOrDefaultAsync(u => u.HotelName == HotelDto.HotelName);
        if (existingUser != null)
            return Conflict(new { message = "Hotel already exists" });


        // Create new user
        var newHotel = new Hotel
        {
            HotelName = HotelDto.HotelName,

        };

        // Save to database
        _context.Hotel.Add(newHotel);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Hotel registered successfully" });

    }


    [HttpPost("addtruck")]

    public async Task<IActionResult> AddTrucks([FromBody] TruckDto TruckDto)
    {
        if (TruckDto == null)
            return BadRequest(new { message = "Invalid Truck data" });

        // Check if the user already exists
        var existingUser = await _context.Truck.SingleOrDefaultAsync(u => u.TruckName == TruckDto.TruckName);
        if (existingUser != null)
            return Conflict(new { message = "Truck already exists" });


        // Create new user
        var newTruck = new Truck
        {
            TruckName = TruckDto.TruckName,

        };

        // Save to database
        _context.Truck.Add(newTruck);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Truck registered successfully" });

    }
    

    [HttpPost("addWasteType")]

    public async Task<IActionResult> AddWasteType([FromBody] WasteDto WasteDto)
    {
        if (WasteDto == null)
            return BadRequest(new { message = "Invalid Waste data" });

        // Check if the user already exists
        var existingUser = await _context.WasteType.SingleOrDefaultAsync(u => u.WasteTypeName == WasteDto.WasteTypeName);
        if (existingUser != null)
            return Conflict(new { message = "WasteType already exists" });


        // Create new user
        var newWaste = new WasteType
        {
            WasteTypeName = WasteDto.WasteTypeName,

        };

        // Save to database
        _context.WasteType.Add(newWaste);
        await _context.SaveChangesAsync();

        return Ok(new { message = "WasteType registered successfully" });

    }
    [HttpGet("all-names")]
    public IActionResult GetAllCollectorNames()
    {
        var names = _context.Collector
            .Select(c => c.CollectorName)
            .ToList();
        return Ok(names);
    }




    [HttpGet("all-Hotels")]
    public IActionResult GetAllHotelsNames()
    {
        var names = _context.Hotel
            .Select(c => c.HotelName)
            .ToList();
        return Ok(names);
    }


    [HttpGet("all-waste")]
    public IActionResult GetAllWasteTypeNames()
    {
        var names = _context.WasteType
            .Select(c => c.WasteTypeName)
            .ToList();
        return Ok(names);
    }


    [HttpGet("getStatistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = new
        {
            hotelCount = await _context.Hotel.CountAsync(),
            collectorCount = await _context.Collector.CountAsync(),
            truckCount = await _context.Truck.CountAsync(),
            wasteTypeCount = await _context.WasteType.CountAsync()
        };

        return Ok(stats);
    }

    [HttpPost("logout")]
    
    public IActionResult Logout()
    {
        // Optionally store token in blacklist if you're using one
        return Ok(new { message = "Logged out successfully" });
    }
    [HttpPost("changePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
    {
        // Validate that new password and confirmation match
        if (model.NewPassword != model.ConfirmPassword)
        {
            return BadRequest("New password and confirmation do not match");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null)
            return NotFound("User not found");

        if (!PasswordHelper.VerifyPassword(model.OldPassword, user.PasswordHash))
            return BadRequest("Old password is incorrect");

        // You might want to add password strength validation here
        if (model.NewPassword == model.OldPassword)
        {
            return BadRequest("New password must be different from old password");
        }

        user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully 💪" });
    }



    [HttpPost("generate-weekly-stats")]
    public async Task<IActionResult> GenerateWeeklyStats()
    {
        var currentWeek = DateTime.Now.GetWeekOfYear();
        var currentYear = DateTime.Now.Year;

        // Load all waste collections from DB (include relations if needed)
        var allCollections = await _context.WasteCollection
            .Include(w => w.Hotel)
            .Include(w => w.Collector)
            .ToListAsync();

        // Filter by current week (client-side)
        var thisWeekCollections = allCollections
            .Where(w => w.EntryTime.GetWeekOfYear() == currentWeek && w.EntryTime.Year == currentYear)
            .ToList();

        // Group by Collector and WasteType (you can change it to Hotel if you prefer)
        var groupedStats = thisWeekCollections
            .GroupBy(w => new { w.CollectorID, w.WasteType })
            .Select(g => new WeeklyStats
            {
                CollectorID = g.Key.CollectorID,
                WasteType = g.Key.WasteType.ToString(), // ⚠️ cast enum to string if needed
                WeekNumber = currentWeek,
                Year = currentYear,
                TotalQuantity = g.Sum(x => x.Quantity),
                CreatedAt = DateTime.Now
            })
            .ToList();


        // Optional: delete existing stats for this week (to avoid duplicates)
        var existingStats = _context.WeeklyStats
            .Where(w => w.WeekNumber == currentWeek && w.Year == currentYear)
            .ToList();

        _context.WeeklyStats.RemoveRange(existingStats);

        // Save new stats
        _context.WeeklyStats.AddRange(groupedStats);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Weekly stats generated ✅", statsCount = groupedStats.Count });
    }
   





}


