using System.ComponentModel.DataAnnotations;
using WasteManagement3.Models;

public class Collector
{
    [Key]
    public int CollectorID { get; set; }
    public string CollectorName { get; set; }

    public ICollection<WasteCollection> WasteCollection { get; set; }
}
