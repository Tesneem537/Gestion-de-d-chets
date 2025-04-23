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
using Microsoft.EntityFrameworkCore; // For EF operations

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
                .AsNoTracking();

            if (!await query.AnyAsync())
            {
                return NotFound("No data available for this week");
            }

            var results = await query.ToListAsync();
            try
            {
                var weeklyData = await _context.WeeklyStats
                    .Where(x => x.WeekNumber == currentWeek)
                    .ToListAsync();

                if (!weeklyData.Any())
                {
                    return NotFound("No data available for this week");
                }

                var totalWaste = weeklyData.Sum(x => x.TotalQuantity);
                var collectionCount = weeklyData.Select(x => x.DayOfWeek).Distinct().Count();
                var bestDay = weeklyData
                    .GroupBy(x => x.DayOfWeek)
                    .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalQuantity) })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault()?.Day ?? "No data";

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
                    .Take(5)
                    .ToList();

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
                return StatusCode(500, $"Internal server error: Failed to run ML prediction - {ex.Message}");
            }
        }

        private async Task<MLResultDto> RunPythonMLAsync(object data)
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

                if (!process.WaitForExit(5000))
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

        private string GetPythonPath()
        {
            var path = _configuration["PythonConfig:PythonExecutable"] ?? "python";
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException($"Python executable not found at: {path}");
            }
            return path;
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var currentDate = DateTime.Now;
                var weekNum = ISOWeek.GetWeekOfYear(currentDate);
                var year = currentDate.Year;

                // Calculate ISO week date range (Monday to Sunday)
                var firstDayOfWeek = ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday);
                var lastDayOfWeek = ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Sunday).AddDays(1).AddTicks(-1);

                // Get data for the current week range with null checks
                var allData = await _context.WasteCollection
                    .Include(w => w.Hotel)
                    .Where(w => w.EntryTime >= firstDayOfWeek && w.EntryTime <= lastDayOfWeek)
                    .Select(w => new
                    {
                        w.HotelID,
                        HotelName = w.Hotel != null ? w.Hotel.HotelName : "Unknown Hotel",
                        w.EntryTime,
                        WasteType = w.WasteType != null ? w.WasteType.ToString() : "Unknown",
                        w.Quantity,
                        w.CollectorID
                    })
                    .ToListAsync();

                if (!allData.Any())
                {
                    return NotFound($"No waste collection data found for week {weekNum} of {year}");
                }

                // Group data and create weekly stats with proper null handling
                var stats = allData
                    .GroupBy(w => new
                    {
                        w.HotelID,
                        w.HotelName,
                        DayOfWeek = w.EntryTime.DayOfWeek.ToString(),
                        w.WasteType
                    })
                    .Select(g => new WeeklyStats
                    {
                        HotelId = g.Key.HotelID,
                        HotelName = g.Key.HotelName,
                        WeekNumber = weekNum,
                        DayOfWeek = g.Key.DayOfWeek,
                        WasteType = g.Key.WasteType,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        CollectorID = g.First().CollectorID,
                        Year = year,
                        CreatedAt = DateTime.Now
                    })
                    .ToList();

                // Remove existing stats for this week
                var existingStats = await _context.WeeklyStats
                    .Where(s => s.WeekNumber == weekNum && s.Year == year)
                    .ToListAsync();

                if (existingStats.Any())
                {
                    _context.WeeklyStats.RemoveRange(existingStats);
                    await _context.SaveChangesAsync();
                }

                // Add new stats
                await _context.WeeklyStats.AddRangeAsync(stats);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Weekly stats successfully auto-filled",
                    WeekNumber = weekNum,
                    Year = year,
                    RecordsAdded = stats.Count,
                    TotalWaste = stats.Sum(s => s.TotalQuantity),
                    HotelsProcessed = stats.Select(s => s.HotelName).Distinct().Count()
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    Message = "Error auto-filling weekly stats",
                    Error = ex.Message,
                    DetailedError = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
        [HttpGet("low-frequency-visits")]
        public IActionResult GetLowFrequencyVisits()
        {
            // Calculate the start and end of the current week
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            var lowFrequency = _context.WasteCollection
                .Where(w => w.EntryTime >= startOfWeek && w.EntryTime < endOfWeek)
                .GroupBy(w => new { w.CollectorID, w.HotelID })
                .Where(g => g.Count() < 2) // Threshold for "too few visits"
                .Select(g => new
                {
                    CollectorID = g.Key.CollectorID,
                    HotelID = g.Key.HotelID,
                    VisitsThisWeek = g.Count()
                })
                .ToList();

            return Ok(lowFrequency);
        }
    }
    }