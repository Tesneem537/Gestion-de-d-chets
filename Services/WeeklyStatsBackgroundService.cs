using WasteManagement3.Data;

namespace WasteManagement3.Services
{
    public class WeeklyStatsBackgroundService : BackgroundService
    {
        private readonly ILogger<WeeklyStatsBackgroundService> _logger;
        private readonly IServiceProvider _services;
        private Timer? _timer;

        public WeeklyStatsBackgroundService(
            ILogger<WeeklyStatsBackgroundService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Calculate time until next Sunday at 23:00 (11 PM)
            var now = DateTime.UtcNow;
            var nextSunday = now.AddDays(7 - (int)now.DayOfWeek).Date.AddHours(23);
            var delay = nextSunday - now;

            _timer = new Timer(async _ =>
            {
                await GenerateWeeklyStatsAsync();

                // Reset timer for next week
                _timer?.Change(TimeSpan.FromDays(7), Timeout.InfiniteTimeSpan);
            }, null, delay, Timeout.InfiniteTimeSpan);

            return Task.CompletedTask;
        }

        private async Task GenerateWeeklyStatsAsync()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

            try
            {
                _logger.LogInformation("Starting automatic weekly stats generation");

                // Create HTTP client to call our own endpoint
                var client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://yourapi.com"); // Update with your base URL

                var response = await client.PostAsync("/api/WeeklyStats/autofill-weeklystats", null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Weekly stats generated successfully: {Content}", content);
                }
                else
                {
                    _logger.LogError("Failed to generate weekly stats: {StatusCode} - {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in automatic weekly stats generation");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
