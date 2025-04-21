using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WasteManagement3.DTOs
{

    public static class DateTimeExtensions
    {
        public static int GetWeekOfYear(this DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            var calendar = culture.Calendar;
            var weekRule = culture.DateTimeFormat.CalendarWeekRule;
            var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;


            try
            {
                return culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            }
            catch
            {
                // Fallback to ISO 8601 week number
                return ISOWeek.GetWeekOfYear(date);
            }
        }
    }
    public class HotelClusterDto
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; }
        public double TotalQuantity { get; set; }
        public int Cluster { get; set; }
    }

    public class MLResultDto
    {
        public double PredictedNextWeekQuantity { get; set; }
        public List<HotelClusterDto> HotelClusters { get; set; }
        public string Message { get; set; }
    }

    public class WeeklyStatsDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "HotelId must be positive")]
        public int HotelId { get; set; }

        [Required(ErrorMessage = "HotelName is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "HotelName must be between 2 and 100 characters")]
        public string HotelName { get; set; }

        [Range(1, 52, ErrorMessage = "WeekNumber must be between 1 and 52")]
        public int WeekNumber { get; set; }

        [Required(ErrorMessage = "DayOfWeek is required")]
        public string DayOfWeek { get; set; }

        [Required(ErrorMessage = "WasteType is required")]
        public string WasteType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "TotalQuantity must be positive")]
        public double TotalQuantity { get; set; }
    }

}

