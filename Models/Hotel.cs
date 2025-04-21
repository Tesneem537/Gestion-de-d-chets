using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WasteManagement3.Models
{
    public class Hotel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HotelID { get; set; }
        [Required]
        public string HotelName { get; set; }
        [Required]
        
        public ICollection<WasteCollection> WasteCollection { get; set; }
    }
}
