// Models/WeeklyStats.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WasteManagement3.Models
{
    public class WeeklyStats
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required]
        public string HotelName { get; set; }

        [Required]
        public int WeekNumber { get; set; }

        [Required]
        public string DayOfWeek { get; set; }

        [Required]
        public string WasteType { get; set; }

        [Required]
        public double TotalQuantity { get; set; }
        [Required]
        public int CollectorID { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
    }
}