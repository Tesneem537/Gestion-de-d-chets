using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WasteManagement3.DTOs;
using WasteManagement3.Models;
using Microsoft.EntityFrameworkCore;
using WasteManagement3.Data;
using System;
using System.Linq;
using System.Globalization;

namespace WasteManagement3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeeklyStatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public WeeklyStatsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("weekly-stats")]
        public async Task<IActionResult> GetWeeklyStats()
        {
            var currentWeek = GetWeekOfYear(DateTime.Now);

            var query = _context.WeeklyStats
                .Where(x => x.WeekNumber == currentWeek)
                .AsNoTracking(); // Important for read-only operations

            if (!await query.AnyAsync())
            {
                return NotFound("No data available for this week");
            }

            var results = await query.ToListAsync();
            try
            {
                // Calculate statistics
                var weeklyData = await _context.WeeklyStats
                    .Where(x => x.WeekNumber == currentWeek)
                    .ToListAsync();

                if (!weeklyData.Any())
                {
                    return NotFound("No data available for this week");
                }

                // Total waste this week
                var totalWaste = weeklyData.Sum(x => x.TotalQuantity);

                // Number of collections (unique days)
                var collectionCount = weeklyData.Select(x => x.DayOfWeek).Distinct().Count();

                // Best collection day (day with most waste collected)
                var bestDay = weeklyData
                    .GroupBy(x => x.DayOfWeek)
                    .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalQuantity) })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault()?.Day ?? "No data";

                // Hotel rankings
                var hotelRankings = weeklyData
                    .GroupBy(x => new { x.HotelId, x.HotelName })
                    .Select(g => new
                    {
                        g.Key.HotelName,
                        TotalWaste = g.Sum(x => x.TotalQuantity)
                    })
                    .OrderByDescending(x => x.TotalWaste)
                    .Select((x, index) => new
                    {
                        x.HotelName,
                        x.TotalWaste,
                        Rank = index + 1
                    })
                    .Take(5) // Top 5 hotels
                    .ToList();

                // Waste by type
                var wasteByType = weeklyData
                    .GroupBy(x => x.WasteType)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalQuantity));

                return Ok(new
                {
                    totalWaste,
                    collectionCount,
                    bestDay,
                    hotelRankings,
                    wasteByType
                });
            }
            catch (Exception ex)
            {
                // Log the error and return a failure response
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("run-ml")]
        public async Task<IActionResult> RunMLPrediction([FromBody] List<WeeklyStatsDto> data)
        {
            if (data == null || data.Count == 0)
            {
                return BadRequest("No data provided.");
            }

            // Convert to the format expected by Python script
            var inputData = data.Select(d => new
            {
                d.HotelId,
                d.HotelName,
                d.WeekNumber,
                d.DayOfWeek,
                d.WasteType,
                d.TotalQuantity
            }).ToList();

            try
            {
                var result = await RunPythonMLAsync(inputData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<MLResultDto> RunPythonMLAsync(object data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var scriptPath = Path.Combine("PythonScripts", "ml_predictor.py");
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), scriptPath);
                if (!System.IO.File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Python script not found at: {fullPath}");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = GetPythonPath(),
                    Arguments = $"\"{fullPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                try
                {
                    process.Start();
                    await process.StandardInput.WriteAsync(json);
                    process.StandardInput.Close();

                    string result = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!process.WaitForExit(5000)) // 5 second timeout
                    {
                        process.Kill();
                        throw new TimeoutException("Python script execution timed out");
                    }

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Python script failed with exit code {process.ExitCode}: {error}");
                    }

                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    var mlResult = JsonConvert.DeserializeObject<MLResultDto>(result, settings);
                    if (mlResult == null)
                    {
                        throw new Exception("Failed to deserialize ML results");
                    }

                    mlResult.HotelClusters ??= new List<HotelClusterDto>();
                    return mlResult;
                }
                catch (Exception ex)
                {
                    process.Kill();
                    throw new Exception("Failed to execute Python script", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to run ML prediction", ex);
            }
        }

        private string GetPythonPath()
        {
            return _configuration["PythonConfig:PythonExecutable"] ?? "python";
        }

        private static int GetWeekOfYear(DateTime date)
        {
            var ci = CultureInfo.CurrentCulture;
            var cal = ci.Calendar;
            var calWeekRule = ci.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = ci.DateTimeFormat.FirstDayOfWeek;
            return cal.GetWeekOfYear(date, calWeekRule, firstDayOfWeek);
        }

        [HttpPost("autofill-weeklystats")]
        public async Task<IActionResult> AutoFillWeeklyStats()
        {
            var weekNum = DateTime.Now.GetWeekOfYear();

            var allData = _context.WasteCollection
              .Include(w => w.Hotel)
             .Include(w => w.Collector)
             .ToList(); // 👈 ici on force l’exécution côté client

            var filtered = allData
                .Where(w => w.EntryTime.GetWeekOfYear() == weekNum)
                .ToList();


            var stats = allData
                .GroupBy(x => new
                {
                    x.HotelID,
                    x.HotelName,
                    DayOfWeek = x.EntryTime.DayOfWeek.ToString(),
                    x.WasteType
                })
                .Select(g => new WeeklyStats
                {
                    HotelId = g.Key.HotelID,
                    HotelName = g.Key.HotelName,
                    WeekNumber = weekNum,
                    DayOfWeek = g.Key.DayOfWeek,
                    WasteType = g.Key.WasteType.ToString(),
                    TotalQuantity = g.Sum(x => x.Quantity)
                }).ToList();

            await _context.WeeklyStats.AddRangeAsync(stats);
            await _context.SaveChangesAsync();

            return Ok("Auto-filled WeeklyStats 😎");
        }



    }
}