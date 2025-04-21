using System.ComponentModel.DataAnnotations;

namespace WasteManagement3.Models
{
    public class WasteType
    {

       
        public int WasteTypeID { get; set; }
        public string WasteTypeName { get; set; }

        public ICollection<WasteCollection> WasteCollection { get; set; }
    }
}

