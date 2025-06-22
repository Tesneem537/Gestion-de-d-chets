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
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            var now = DateTime.UtcNow;
            var currentWeek = ISOWeek.GetWeekOfYear(now);
            var currentYear = now.Year;

            try
            {
                var weeklyData = await _context.WeeklyStats
                    .Where(x => x.WeekNumber == currentWeek && x.Year == currentYear)
                    .ToListAsync();

                if (!weeklyData.Any())
                {
                    var availableWeeks = await _context.WeeklyStats
                        .GroupBy(x => new { x.Year, x.WeekNumber })
                        .Select(g => new { g.Key.Year, g.Key.WeekNumber })
                        .OrderByDescending(x => x.Year)
                        .ThenByDescending(x => x.WeekNumber)
                        .ToListAsync();

                    return NotFound(new
                    {
                        message = $"No data available for ISO week {currentWeek} in year {currentYear}.",
                        calculatedWeek = currentWeek,
                        currentDate = now.ToString("yyyy-MM-dd"),
                        availableWeeks
                    });
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

                var dailyData = weeklyData
                    .GroupBy(x => x.DayOfWeek.ToLower())
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            Waste = g.Sum(x => x.TotalQuantity),
                            Collections = g.Count()
                        });

                var dailyWaste = dailyData.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Waste
                );

                var dailyCollections = dailyData.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Collections
                );

                return Ok(new
                {
                    totalWaste,
                    collectionCount,
                    bestDay,
                    hotelRankings,
                    wasteByType,
                    dailyWaste,
                    dailyCollections,
                    currentWeek,
                    currentYear
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }



        [HttpPost("autofill-weeklystats")]
        public async Task<IActionResult> AutoFillWeeklyStats([FromQuery] bool forceRefresh = false)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime? lastProcessedDate = forceRefresh
                    ? null
                    : await _context.WeeklyStats
                        .OrderByDescending(ws => ws.CreatedAt)
                        .Select(ws => (DateTime?)ws.CreatedAt)
                        .FirstOrDefaultAsync();

                var collectionsQuery = _context.WasteCollection
                    .Include(w => w.Hotel)
                    .AsQueryable();

                if (!forceRefresh && lastProcessedDate.HasValue)
                {
                    collectionsQuery = collectionsQuery.Where(w => w.EntryTime > lastProcessedDate.Value);
                }

                var newCollections = await collectionsQuery
                    .Select(w => new
                    {
                        w.HotelID,
                        HotelName = w.Hotel != null ? w.Hotel.HotelName : "Unknown Hotel",
                        w.EntryTime,
                        w.WasteType,
                        w.Quantity,
                        w.CollectorID,
                        WeekNumber = ISOWeek.GetWeekOfYear(w.EntryTime),
                        Year = ISOWeek.GetYear(w.EntryTime),
                        DayOfWeek = w.EntryTime.DayOfWeek.ToString()
                    })
                    .ToListAsync();

                if (!newCollections.Any())
                {
                    return Ok(new
                    {
                        Message = "No new waste collection data found to process",
                        LastProcessedDate = lastProcessedDate?.ToString("yyyy-MM-ddTHH:mm:ss"),
                        ForceRefresh = forceRefresh,
                        Suggestion = forceRefresh
                            ? "No data exists in WasteCollection table"
                            : "Try with ?forceRefresh=true to process all data"
                    });
                }

                var statsAdded = 0;
                var statsUpdated = 0;
                var processedWeekYears = new HashSet<(int Week, int Year)>();

                foreach (var collection in newCollections)
                {
                    var existingStat = await _context.WeeklyStats
                        .FirstOrDefaultAsync(ws =>
                            ws.HotelId == collection.HotelID &&
                            ws.WeekNumber == collection.WeekNumber &&
                            ws.Year == collection.Year &&
                            ws.DayOfWeek == collection.DayOfWeek &&
                            ws.WasteType == collection.WasteType.ToString());

                    if (existingStat != null)
                    {
                        existingStat.TotalQuantity += collection.Quantity;
                        existingStat.CollectorID = collection.CollectorID;
                        existingStat.CreatedAt = DateTime.UtcNow;
                        statsUpdated++;
                    }
                    else
                    {
                        _context.WeeklyStats.Add(new WeeklyStats
                        {
                            HotelId = collection.HotelID,
                            HotelName = collection.HotelName,
                            WeekNumber = collection.WeekNumber,
                            Year = collection.Year,
                            DayOfWeek = collection.DayOfWeek,
                            WasteType = collection.WasteType.ToString(),
                            TotalQuantity = collection.Quantity,
                            CollectorID = collection.CollectorID,
                            CreatedAt = DateTime.UtcNow
                        });
                        statsAdded++;
                    }

                    processedWeekYears.Add((collection.WeekNumber, collection.Year));
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Message = "Weekly stats updated successfully",
                    Statistics = new
                    {
                        NewCollectionsProcessed = newCollections.Count,
                        StatsAdded = statsAdded,
                        StatsUpdated = statsUpdated,
                        WeeksProcessed = processedWeekYears.Count,
                        EarliestDateProcessed = newCollections.Min(c => c.EntryTime),
                        LatestDateProcessed = newCollections.Max(c => c.EntryTime)
                    },
                    ProcessingDetails = new
                    {
                        ForceRefresh = forceRefresh,
                        LastProcessedDateBefore = lastProcessedDate,
                        NewLastProcessedDate = DateTime.UtcNow
                    },
                    DataSummary = new
                    {
                        Hotels = newCollections.Select(c => c.HotelName).Distinct().Count(),
                        WasteTypes = newCollections.Select(c => c.WasteType.ToString()).Distinct().Count(),
                        DaysWithData = newCollections.Select(c => c.DayOfWeek).Distinct().Count()
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    Message = "Failed to update weekly stats",
                    ErrorDetails = new
                    {
                        ex.Message,
                        ex.StackTrace
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        [HttpPost("run-ml")]
        public async Task<IActionResult> RunMLPrediction([FromQuery] int weekNumber, [FromQuery] int year)
        {
            try
            {
                // Get all data for the given week and year
                var data = await _context.WeeklyStats
                    .Where(ws => ws.WeekNumber == weekNumber && ws.Year == year)
                    .Select(d => new WeeklyStatsDto
                    {
                        HotelId = d.HotelId,
                        HotelName = d.HotelName,
                        WeekNumber = d.WeekNumber,
                        DayOfWeek = d.DayOfWeek,
                        WasteType = d.WasteType,
                        TotalQuantity = d.TotalQuantity
                    }).ToListAsync();

                if (data == null || data.Count == 0)
                {
                    return BadRequest("No data provided.");
                }

                // Run Python ML Script
                var result = await RunPythonMLAsync(data);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: Failed to run ML prediction - {ex.Message}");
            }
        }

        private async Task<MLResultDto> RunPythonMLAsync(List<WeeklyStatsDto> data)
        {
            var json = JsonConvert.SerializeObject(data);
            var scriptPath = Path.Combine("PythonScripts", "ml_predictor.py");
            var fullPath = Path.GetFullPath(scriptPath);

            if (!System.IO.File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Python script not found at: {fullPath}");
            }

            var pythonPath = GetPythonPath();
            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{fullPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            try
            {
                process.Start();

                await using (var writer = process.StandardInput)
                {
                    await writer.WriteAsync(json);
                }

                string result = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(15000)) // Increased timeout to 15 seconds
                {
                    process.Kill();
                    throw new TimeoutException("Python script execution timed out");
                }

                // Only treat as error if exit code is non-zero AND error contains actual error (not warnings)
                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error) &&
                    !error.Contains("UserWarning") && !error.Contains("Warning"))
                {
                    throw new Exception($"Python script failed: {error}");
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
                if (!process.HasExited)
                {
                    process.Kill();
                }
                throw new Exception($"Failed to execute Python script: {ex.Message}");
            }
        }

        private string GetPythonPath()
        {
            // Try common Python paths if config is not set
            var path = _configuration["PythonConfig:PythonExecutable"] ?? "python3";

            // Check if python exists in PATH
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    return path;
                }
            }
            catch { }

            // Try alternative paths
            var possiblePaths = new[]
            {
        "python",
        "python3",
        "/usr/bin/python3",
        "/usr/local/bin/python3",
        "C:\\Python39\\python.exe",
        "C:\\Python38\\python.exe"
    };

            foreach (var possiblePath in possiblePaths)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = possiblePath,
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        return possiblePath;
                    }
                }
                catch { }
            }

            throw new FileNotFoundException("Python executable not found. Please install Python or configure the path in appsettings.json");
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
        [HttpGet("historical-data")]
        public async Task<IActionResult> GetHistoricalData([FromQuery] int? weeks)
        {
            try
            {
                var weeksBack = weeks ?? 4; // Default to 4 weeks if not specified
                var currentDate = DateTime.Now;
                var currentWeek = ISOWeek.GetWeekOfYear(currentDate);
                var currentYear = currentDate.Year;

                // Calculate the earliest week we want to include
                var earliestDate = currentDate.AddDays(-7 * weeksBack);
                var earliestWeek = ISOWeek.GetWeekOfYear(earliestDate);
                var earliestYear = earliestDate.Year;

                // Get all weekly stats within the date range
                var historicalStats = await _context.WeeklyStats
                    .Where(ws =>
                        (ws.Year > earliestYear ||
                         (ws.Year == earliestYear && ws.WeekNumber >= earliestWeek)) &&
                        (ws.Year < currentYear ||
                         (ws.Year == currentYear && ws.WeekNumber <= currentWeek)))
                    .GroupBy(ws => new { ws.WeekNumber, ws.Year })
                    .OrderByDescending(g => g.Key.Year)
                    .ThenByDescending(g => g.Key.WeekNumber)
                    .Select(g => new
                    {
                        WeekStartDate = ISOWeek.ToDateTime(g.Key.Year, g.Key.WeekNumber, DayOfWeek.Monday),
                        Stats = g.ToList()
                    })
                    .ToListAsync();

                if (!historicalStats.Any())
                {
                    return NotFound("No historical data available for the specified period");
                }

                var result = new List<object>();

                foreach (var weekGroup in historicalStats)
                {
                    var weeklyData = weekGroup.Stats;

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
                            HotelId = g.Key.HotelId,
                            HotelName = g.Key.HotelName,
                            TotalWaste = g.Sum(x => x.TotalQuantity)
                        })
                        .OrderByDescending(x => x.TotalWaste)
                        .Select((x, index) => new
                        {
                            x.HotelId,
                            x.HotelName,
                            x.TotalWaste,
                            Rank = index + 1
                        })
                        .Take(5)
                        .ToList();

                    var wasteByType = weeklyData
                        .GroupBy(x => x.WasteType)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalQuantity));

                    // Get daily waste and collections
                    var dailyData = weeklyData
                        .GroupBy(x => x.DayOfWeek)
                        .ToDictionary(
                            g => g.Key.ToLower(),
                            g => new
                            {
                                Waste = g.Sum(x => x.TotalQuantity),
                                Collections = g.Count()
                            }
                        );

                    var dailyWaste = dailyData.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Waste
                    );

                    var dailyCollections = dailyData.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Collections
                    );

                    result.Add(new
                    {
                        WeekStartDate = weekGroup.WeekStartDate.ToString("yyyy-MM-dd"),
                        TotalWaste = totalWaste,
                        CollectionCount = collectionCount,
                        BestDay = bestDay,
                        HotelRankings = hotelRankings,
                        WasteByType = wasteByType,
                        DailyWaste = dailyWaste,
                        DailyCollections = dailyCollections
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("check-week-calculation")]
        public IActionResult CheckWeekCalculation()
        {
            var currentDate = DateTime.UtcNow;
            var weekNum = ISOWeek.GetWeekOfYear(currentDate);
            var year = currentDate.Year;

            return Ok(new
            {
                CurrentUTC = DateTime.UtcNow,
                WeekNumber = weekNum,
                Year = year,
                WeekStart = ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Monday),
                WeekEnd = ISOWeek.ToDateTime(year, weekNum, DayOfWeek.Sunday)
            });
        }
        [HttpGet("daily-waste")]
        public async Task<IActionResult> GetDailyWasteTrend()
        {
            try
            {
                // Get the current week's data
                var currentWeek = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
                var currentYear = DateTime.UtcNow.Year;

                var dailyWaste = await _context.WeeklyStats
                    .Where(ws => ws.WeekNumber == currentWeek && ws.Year == currentYear)
                    .GroupBy(ws => ws.DayOfWeek.ToLower()) // Ensure lowercase (e.g., "monday" → "mon")
                    .Select(g => new
                    {
                        Day = g.Key.Substring(0, 3), // Shorten to 3 letters (e.g., "mon")
                        TotalWaste = g.Sum(ws => ws.TotalQuantity)
                    })
                    .ToDictionaryAsync(x => x.Day, x => x.TotalWaste);

                // Fill missing days with 0
                var daysOfWeek = new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };
                var filledData = daysOfWeek.ToDictionary(
                    day => day,
                    day => dailyWaste.TryGetValue(day, out var total) ? total : 0.0
                );

                return Ok(new { dailyWaste = filledData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to fetch daily waste trend",
                    error = ex.Message
                });
            }
        }
    }
    }