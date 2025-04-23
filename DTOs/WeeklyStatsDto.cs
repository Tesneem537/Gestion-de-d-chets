using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WasteManagement3.DTOs
{
    public static class DateTimeExtensions
    {
        public static int GetWeekOfYear(this DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                return culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            }
            catch
            {
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
        [Range(1, int.MaxValue)]
        public int HotelId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string HotelName { get; set; }

        [Range(1, 52)]
        public int WeekNumber { get; set; }

        [Required]
        public string DayOfWeek { get; set; }

        [Required]
        public string WasteType { get; set; }

        [Range(0, double.MaxValue)]
        public double TotalQuantity { get; set; }
    }
}