namespace WasteManagement3.Models
{
    public class NFCdonnees
    {
        public int Id { get; set; }
        public string TagId { get; set; }
        public DateTime EntryTime { get; set; }
        public string EntryLocation { get; set; }
        public DateTime? ExitTime { get; set; } // Nullable
        public string? ExitLocation { get; set; } // Nullable
    }
}
