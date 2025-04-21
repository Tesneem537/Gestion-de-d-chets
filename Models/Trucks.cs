using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WasteManagement3.Models
{
    public class Truck
    {
        [Key]  // Mark TruckID as the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Identity ensures automatic increment
        public int TruckID { get; set; }

        public string TruckName { get; set; }
       
        public ICollection<WasteCollection> WasteCollection { get; set; }
    }
}
