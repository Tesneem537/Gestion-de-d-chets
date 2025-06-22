namespace WasteManagement3.DTOs
{
    public class ScanResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ScanType { get; set; } // "entry" or "exit"
        public string TagId { get; set; }
        public DateTime EntryTime { get; set; }
        public string EntryLocation { get; set; }
        public DateTime? ExitTime { get; set; }
        public string? ExitLocation { get; set; }
        public double? DurationMinutes { get; set; }
    }
}