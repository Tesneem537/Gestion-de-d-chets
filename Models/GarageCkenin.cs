using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WasteManagement3.Models
{
    public class GarageCheckin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckinID { get; set; }  // Ensure this is only defined once

        [ForeignKey("Collector")]
        public int CollectorID { get; set; }
        public Users Collector { get; set; }

        [ForeignKey("Truck")]
        public int TruckID { get; set; }
        public Truck Truck { get; set; }

        public DateTime CheckinTime { get; set; }
    }
}
