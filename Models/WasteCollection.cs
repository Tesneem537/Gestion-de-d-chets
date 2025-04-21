using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WasteManagement3.Models
{
    public class WasteCollection
    {
        [Key]
        public int WasteCollectionID { get; set; }

      
        public int CollectorID { get; set; }

       
        public Collector Collector { get; set; }


        public int HotelID { get; set; }

       
        public Hotel Hotel { get; set; }

      
        public string TruckName { get; set; }
        public int TruckID { get; set; }

        public Truck Truck { get; set; }

       
        public int WasteTypeID { get; set; }

        public WasteType WasteType { get; set; }

     
        public double Quantity { get; set; }
        public string? Comment { get; set; }
        public DateTime EntryTime { get; set; }
        public byte[]? Photo { get; set; }
        public string CollectorName { get; internal set; }
        public string HotelName { get; internal set; }
        public string WasteTypeName { get; internal set; }
    }
}
